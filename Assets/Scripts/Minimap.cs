using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
public class Minimap : MonoBehaviour, IPointerClickHandler
{
    [Header("Full Map")]
    public float fullMapRange = 500f;
    public float fullMapMinRange = 25f;
    public float fullMapMaxRange = 1000f;
    public bool showMonsterMarkers = true;
    [SerializeField] private GameObject fullMapPanel;
    [SerializeField] private RawImage fullMapImage;
    [SerializeField] private RectTransform fullMapMarkers;
    [SerializeField] private Button closeMapButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private RawImage fullMapGrid;
    [SerializeField] private Text fullMapScaleText;
    private Texture2D gridTexture;
    private const float GridCellMeters = 50f;
    private Vector3 mapCenter;
    private Bounds worldBounds;
    private bool hasWorldBounds;
    private bool fullMapOpen;
    private bool draggingMap;
    private Vector2 lastMapPointer;
    private float fullMapZoom;
    public RawImage mapImage;
    public RectTransform markers;
    public Image localArrow;
    public Sprite dotSprite;
    public float range = 30f;
    public float height = 60f;
    private const float RenderInterval = 0.1f;
    private const float SearchInterval = 1f;
    private const int TexSize = 256;
    private static readonly Color[] TeamColors =
    {
        new Color(0.93f, 0.78f, 0.1f, 1f),
        new Color(0.9f, 0.45f, 0.12f, 1f),
        new Color(0.2f, 0.5f, 0.9f, 1f),
        new Color(0.35f, 0.7f, 0.2f, 1f)
    };
    private readonly Dictionary<CharController_Motor, Image> playerDots = new Dictionary<CharController_Motor, Image>();
    private readonly Dictionary<EnemyAI, Image> enemyDots = new Dictionary<EnemyAI, Image>();
    private readonly List<CharController_Motor> removeMotors = new List<CharController_Motor>();
    private readonly List<EnemyAI> removeEnemies = new List<EnemyAI>();
    private RenderTexture texture;
    private Camera mapCamera;
    private Light minimapLight;
    private Sprite generatedDot;
    private CharController_Motor localMotor;
    private GameObject loadingPanel;
    private readonly List<HudVisibilityState> hiddenGameplayHud = new List<HudVisibilityState>();
    private float nextRender;
    private float nextSearch;
    private static readonly string[] GameplayControlNames =
    {
        "PwTouchControls",
        "TouchControls",
        "Fixed Joystick",
        "Floating Joystick",
        "Dynamic Joystick",
        "Variable Joystick",
        "MoveJoystick",
        "JumpBtn",
        "JumpButton",
        "SprintBtn",
        "AutoRunBtn",
        "AutoRunButton",
        "InteractBtn",
        "InteractButton",
        "ViewSwitchBtn",
        "FlashlightBtn",
        "CrouchBtn"
    };
    private void Start()
    {
        if (mapImage == null)
        {
            Transform t = transform.Find("Mask/Map");
            mapImage = t != null ? t.GetComponent<RawImage>() : null;
        }
        if (markers == null)
        {
            markers = transform.Find("Mask/Markers") as RectTransform;
        }
        if (localArrow == null && markers != null)
        {
            Transform t = markers.Find("Me");
            localArrow = t != null ? t.GetComponent<Image>() : null;
        }
        if (mapImage == null || markers == null)
        {
            enabled = false;
            return;
        }
        if (fullMapPanel == null || fullMapImage == null || fullMapMarkers == null ||
            closeMapButton == null || zoomInButton == null || zoomOutButton == null)
        {
            enabled = false;
            return;
        }
        mapImage.raycastTarget = true;
        if (dotSprite == null)
        {
            Texture2D tex = MakeDiscTexture(64, 7f);
            generatedDot = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
            dotSprite = generatedDot;
        }
        texture = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
        texture.name = "MinimapTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.Create();
        mapImage.texture = texture;
        fullMapImage.texture = texture;
        fullMapImage.color = Color.white;
        if (fullMapGrid != null && fullMapGrid.texture == null)
        {
            gridTexture = MakeGridTexture(128);
            fullMapGrid.texture = gridTexture;
            fullMapGrid.raycastTarget = false;
        }
        BuildCamera();
        closeMapButton.onClick.AddListener(CloseFullMap);
        zoomInButton.onClick.AddListener(ZoomInFullMap);
        zoomOutButton.onClick.AddListener(ZoomOutFullMap);
        fullMapPanel.SetActive(false);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform lp = canvas.rootCanvas.transform.Find("LoadingPanel");
            loadingPanel = lp != null ? lp.gameObject : null;
        }
    }
    private void OnDisable()
    {
        RestoreGameplayHud();
    }
    private void OnDestroy()
    {
        RestoreGameplayHud();
        if (closeMapButton != null) closeMapButton.onClick.RemoveListener(CloseFullMap);
        if (zoomInButton != null) zoomInButton.onClick.RemoveListener(ZoomInFullMap);
        if (zoomOutButton != null) zoomOutButton.onClick.RemoveListener(ZoomOutFullMap);
        if (mapCamera != null)
        {
            mapCamera.targetTexture = null;
            Destroy(mapCamera.gameObject);
            mapCamera = null;
        }
        if (minimapLight != null)
        {
            Destroy(minimapLight.gameObject);
            minimapLight = null;
        }
        if (mapImage != null && mapImage.texture == texture)
        {
            mapImage.texture = null;
        }
        if (fullMapImage != null && fullMapImage.texture == texture)
        {
            fullMapImage.texture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
            texture = null;
        }
        if (generatedDot != null)
        {
            Destroy(generatedDot.texture);
            Destroy(generatedDot);
        }
        if (gridTexture != null)
        {
            if (fullMapGrid != null && fullMapGrid.texture == gridTexture) fullMapGrid.texture = null;
            Destroy(gridTexture);
            gridTexture = null;
        }
    }
    private void BuildCamera()
    {
        GameObject go = new GameObject("MinimapCamera");
        mapCamera = go.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = range;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.035f, 0.075f, 0.12f, 1f);
        mapCamera.cullingMask = ~0;
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = 1000f;
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.useOcclusionCulling = false;
        mapCamera.depth = -50f;
        mapCamera.targetTexture = texture;
        mapCamera.enabled = false;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject lightObject = new GameObject("MinimapLight");
        minimapLight = lightObject.AddComponent<Light>();
        minimapLight.type = LightType.Directional;
        minimapLight.color = new Color(0.88f, 0.92f, 1f, 1f);
        minimapLight.intensity = 0.85f;
        minimapLight.shadows = LightShadows.None;
        minimapLight.cullingMask = mapCamera.cullingMask;
        minimapLight.enabled = false;
        lightObject.transform.rotation = Quaternion.Euler(65f, -30f, 0f);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        OpenFullMap();
    }
    private void ZoomInFullMap()
    {
        SetFullMapZoom(fullMapZoom - 20f);
    }
    private void ZoomOutFullMap()
    {
        SetFullMapZoom(fullMapZoom + 20f);
    }
    private void OpenFullMap()
    {
        if (fullMapPanel == null) return;
        if (!hasWorldBounds) CalculateWorldBounds();
        Search();
        fullMapOpen = true;
        HideGameplayHud();
        fullMapPanel.SetActive(true);
        float maxExtent = Mathf.Max(worldBounds.extents.x, worldBounds.extents.z);
        float fullCoverage = Mathf.Max(maxExtent * 1.1f, Mathf.Max(worldBounds.size.x, worldBounds.size.z) * 0.55f);
        if (fullCoverage > fullMapMaxRange)
        {
            fullMapMaxRange = fullCoverage * 1.5f;
        }
        fullMapZoom = Mathf.Clamp(fullCoverage, fullMapMinRange, fullMapMaxRange);
        mapCenter = worldBounds.center;
        RenderFullMap();
    }
    private void CloseFullMap()
    {
        fullMapOpen = false;
        if (fullMapPanel != null) fullMapPanel.SetActive(false);
        RestoreGameplayHud();
    }
    private void HideGameplayHud()
    {
        RestoreGameplayHud();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform root = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
        List<GameObject> targets = new List<GameObject>();
        PwTouchControls[] touchControls = root.GetComponentsInChildren<PwTouchControls>(true);
        for (int i = 0; i < touchControls.Length; i++)
        {
            AddHudTarget(targets, touchControls[i] != null ? touchControls[i].gameObject : null);
        }
        Joystick[] joysticks = root.GetComponentsInChildren<Joystick>(true);
        for (int i = 0; i < joysticks.Length; i++)
        {
            AddHudTarget(targets, joysticks[i] != null ? joysticks[i].gameObject : null);
        }
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && IsGameplayControlName(candidate.name))
            {
                AddHudTarget(targets, candidate.gameObject);
            }
        }
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null || target == fullMapPanel || target.transform.IsChildOf(fullMapPanel.transform)) continue;
            hiddenGameplayHud.Add(new HudVisibilityState(target, target.activeSelf));
            target.SetActive(false);
        }
    }
    private void RestoreGameplayHud()
    {
        for (int i = hiddenGameplayHud.Count - 1; i >= 0; i--)
        {
            HudVisibilityState state = hiddenGameplayHud[i];
            if (state.target != null) state.target.SetActive(state.wasActive);
        }
        hiddenGameplayHud.Clear();
    }
    private static void AddHudTarget(List<GameObject> targets, GameObject candidate)
    {
        if (candidate == null) return;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            GameObject existing = targets[i];
            if (existing == null)
            {
                targets.RemoveAt(i);
                continue;
            }
            if (candidate.transform.IsChildOf(existing.transform)) return;
            if (existing.transform.IsChildOf(candidate.transform)) targets.RemoveAt(i);
        }
        targets.Add(candidate);
    }
    private static bool IsGameplayControlName(string objectName)
    {
        for (int i = 0; i < GameplayControlNames.Length; i++)
        {
            if (objectName == GameplayControlNames[i]) return true;
        }
        return false;
    }
    private void SetFullMapZoom(float value)
    {
        fullMapZoom = Mathf.Clamp(value, fullMapMinRange, fullMapMaxRange);
        RenderFullMap();
    }
    private void CalculateWorldBounds()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        bool found = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.gameObject.layer == 5 || r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }

        Terrain[] terrains = Terrain.activeTerrains != null && Terrain.activeTerrains.Length > 0
            ? Terrain.activeTerrains
            : FindObjectsByType<Terrain>(FindObjectsInactive.Exclude);
        if (terrains != null)
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if (t == null || t.terrainData == null) continue;
                Vector3 pos = t.transform.position;
                Vector3 size = t.terrainData.size;
                Bounds tBounds = new Bounds(pos + size * 0.5f, size);
                if (!found)
                {
                    bounds = tBounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(tBounds);
                }
            }
        }

        if (!found) bounds = new Bounds(Vector3.zero, new Vector3(fullMapRange, 20f, fullMapRange));
        worldBounds = bounds;
        hasWorldBounds = true;

        float maxExtent = Mathf.Max(worldBounds.extents.x, worldBounds.extents.z);
        float neededRange = Mathf.Max(maxExtent * 1.1f, Mathf.Max(worldBounds.size.x, worldBounds.size.z) * 0.55f);
        if (neededRange > fullMapMaxRange)
        {
            fullMapMaxRange = neededRange * 1.5f;
        }
    }
    private void RenderFullMap()
    {
        if (!fullMapOpen || mapCamera == null || fullMapImage == null) return;
        mapCamera.orthographicSize = fullMapZoom;
        float highestY = hasWorldBounds ? worldBounds.max.y : mapCenter.y;
        float lowestY = hasWorldBounds ? worldBounds.min.y : (mapCenter.y - 100f);
        float cameraY = highestY + Mathf.Max(50f, height);
        float sceneDepth = (cameraY - lowestY) + 100f;
        mapCamera.farClipPlane = Mathf.Max(1000f, sceneDepth);
        mapCamera.transform.position = new Vector3(mapCenter.x, cameraY, mapCenter.z);
        if (minimapLight != null) minimapLight.cullingMask = mapCamera.cullingMask;
        RenderWithMapLighting();
        UpdateGridOverlay();
        UpdateFullMapMarkers();
    }
    private void UpdateGridOverlay()
    {
        if (fullMapGrid != null)
        {
            float tiles = fullMapZoom * 2f / GridCellMeters;
            float u = (mapCenter.x - fullMapZoom) / GridCellMeters;
            float v = (mapCenter.z - fullMapZoom) / GridCellMeters;
            fullMapGrid.uvRect = new Rect(u, v, tiles, tiles);
        }
        if (fullMapScaleText != null)
        {
            fullMapScaleText.text = Mathf.RoundToInt(fullMapZoom * 2f) + " m";
        }
    }
    private void UpdateFullMapMarkers()
    {
        if (fullMapMarkers == null) return;
        Vector2 size = fullMapMarkers.rect.size;
        for (int i = 0; i < fullMapMarkers.childCount; i++) Destroy(fullMapMarkers.GetChild(i).gameObject);
        if (localMotor != null) AddFullMarker(localMotor.transform.position, TeamColor(localMotor), "Me", localMotor.transform.eulerAngles.y, 30f);
        foreach (KeyValuePair<CharController_Motor, Image> pair in playerDots)
        {
            if (pair.Key != null && pair.Key != localMotor && pair.Key.gameObject.activeInHierarchy) AddFullMarker(pair.Key.transform.position, TeamColor(pair.Key), "Player", 0f, 18f);
        }
        if (showMonsterMarkers)
        {
            foreach (KeyValuePair<EnemyAI, Image> pair in enemyDots)
            {
                if (pair.Key != null && pair.Key.gameObject.activeInHierarchy) AddFullMarker(pair.Key.transform.position, new Color(0.9f, 0.04f, 0.04f, 1f), "Monster", 0f, 20f);
            }
        }
    }
    private void AddFullMarker(Vector3 position, Color color, string name, float yaw, float size)
    {
        if (fullMapMarkers == null) return;
        float x = Mathf.InverseLerp(mapCenter.x - fullMapZoom, mapCenter.x + fullMapZoom, position.x);
        float y = Mathf.InverseLerp(mapCenter.z - fullMapZoom, mapCenter.z + fullMapZoom, position.z);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(fullMapMarkers, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x, y);
        rt.anchorMax = new Vector2(x, y);
        rt.sizeDelta = new Vector2(size, size);
        Image image = go.GetComponent<Image>();
        bool isMe = name == "Me";
        Sprite arrow = localArrow != null ? localArrow.sprite : null;
        image.sprite = isMe && arrow != null ? arrow : dotSprite;
        image.color = color;
        image.raycastTarget = false;
        if (isMe)
        {
            rt.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
        else if (name == "Monster")
        {
            GameObject ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(rt, false);
            RectTransform ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = new Vector2(0.5f, 0.5f);
            ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(size * 1.9f, size * 1.9f);
            Image ring = ringGo.GetComponent<Image>();
            ring.sprite = dotSprite;
            ring.color = new Color(0.9f, 0.04f, 0.04f, 0.28f);
            ring.raycastTarget = false;
        }
    }
    private void LateUpdate()
    {
        if (fullMapOpen)
        {
            HandleFullMapInput();
            if (fullMapOpen && Time.unscaledTime >= nextRender)
            {
                nextRender = Time.unscaledTime + 0.25f;
                RenderFullMap();
            }
            return;
        }
        float now = Time.unscaledTime;
        if (now >= nextSearch)
        {
            nextSearch = now + SearchInterval;
            Search();
        }
        if (localMotor == null || !localMotor.isActiveAndEnabled)
        {
            localMotor = null;
            if (localArrow != null)
            {
                localArrow.enabled = false;
            }
            return;
        }
        Transform me = localMotor.transform;
        Vector3 center = me.position;
        if (localArrow != null)
        {
            localArrow.enabled = true;
            localArrow.color = TeamColor(localMotor);
            localArrow.rectTransform.anchoredPosition = Vector2.zero;
            localArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -me.eulerAngles.y);
            localArrow.rectTransform.SetAsLastSibling();
        }
        foreach (KeyValuePair<CharController_Motor, Image> kv in playerDots)
        {
            if (kv.Key == null || kv.Value == null)
            {
                continue;
            }
            bool show = kv.Key != localMotor && kv.Key.gameObject.activeInHierarchy;
            kv.Value.enabled = show;
            if (show)
            {
                kv.Value.color = TeamColor(kv.Key);
                kv.Value.rectTransform.anchoredPosition = ToMap(kv.Key.transform.position - center);
            }
        }
        float pulse = 1f + Mathf.Sin(now * 6f) * 0.15f;
        foreach (KeyValuePair<EnemyAI, Image> kv in enemyDots)
        {
            if (kv.Key == null || kv.Value == null)
            {
                continue;
            }
            bool show = kv.Key.gameObject.activeInHierarchy;
            kv.Value.enabled = show;
            if (show)
            {
                kv.Value.rectTransform.anchoredPosition = ToMap(kv.Key.transform.position - center);
                kv.Value.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        bool loading = loadingPanel != null && loadingPanel.activeInHierarchy;
        if (!loading && now >= nextRender)
        {
            nextRender = now + RenderInterval;
            RenderMap(center);
        }
    }
    private void HandleFullMapInput()
    {
        if (fullMapImage == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            CloseFullMap();
            return;
        }
        if (Input.mouseScrollDelta.y != 0f) SetFullMapZoom(fullMapZoom - Input.mouseScrollDelta.y * 12f);
        Vector2 pointer = Input.mousePosition;
        bool pressed = Input.GetMouseButtonDown(0);
        bool held = Input.GetMouseButton(0);
        bool released = Input.GetMouseButtonUp(0);
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer = touch.position;
            pressed = touch.phase == TouchPhase.Began;
            held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        if (pressed)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fullMapImage.rectTransform, pointer, null, out lastMapPointer);
            draggingMap = true;
        }
        if (released) draggingMap = false;
        if (draggingMap && held)
        {
            Vector2 current;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fullMapImage.rectTransform, pointer, null, out current);
            Vector2 delta = current - lastMapPointer;
            lastMapPointer = current;
            mapCenter -= new Vector3(delta.x, 0f, delta.y) * fullMapZoom / Mathf.Max(1f, fullMapImage.rectTransform.rect.width);
            RenderFullMap();
        }
    }
    private void RenderMap(Vector3 center)
    {
        mapCamera.orthographicSize = range;
        mapCamera.farClipPlane = Mathf.Max(1000f, height + 120f);
        mapCamera.transform.position = new Vector3(center.x, center.y + height, center.z);
        if (minimapLight != null) minimapLight.cullingMask = mapCamera.cullingMask;
        RenderWithMapLighting();
    }
    private void RenderWithMapLighting()
    {
        if (mapCamera == null) return;

        if (minimapLight != null) minimapLight.cullingMask = mapCamera.cullingMask;
        bool fog = RenderSettings.fog;
        Color ambient = RenderSettings.ambientLight;
        float ambientIntensity = RenderSettings.ambientIntensity;
        bool lightEnabled = minimapLight != null && minimapLight.enabled;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.7f, 1f);
        RenderSettings.ambientIntensity = Mathf.Max(ambientIntensity, 1.25f);
        if (minimapLight != null) minimapLight.enabled = true;
        try
        {
            mapCamera.Render();
        }
        finally
        {
            if (minimapLight != null) minimapLight.enabled = lightEnabled;
            RenderSettings.fog = fog;
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientIntensity = ambientIntensity;
        }
    }
    private Vector2 ToMap(Vector3 offset)
    {
        float half = markers.rect.width * 0.5f;
        Vector2 p = new Vector2(offset.x, offset.z) / range * half;
        float max = half - 12f;
        if (p.magnitude > max)
        {
            p = p.normalized * max;
        }
        return p;
    }
    private void Search()
    {
        CharController_Motor[] motors = FindObjectsByType<CharController_Motor>(FindObjectsInactive.Include);
        for (int i = 0; i < motors.Length; i++)
        {
            CharController_Motor m = motors[i];
            if (m == null)
            {
                continue;
            }
            if (localMotor == null && m.isActiveAndEnabled && m.IsLocal)
            {
                localMotor = m;
            }
            if (!playerDots.ContainsKey(m))
            {
                playerDots.Add(m, NewDot("Player", Color.white, 20f));
            }
        }
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Include);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyAI e = enemies[i];
            if (e == null || enemyDots.ContainsKey(e))
            {
                continue;
            }
            enemyDots.Add(e, NewDot("Monster", new Color(0.9f, 0.05f, 0.05f, 1f), 22f));
        }
        removeMotors.Clear();
        foreach (KeyValuePair<CharController_Motor, Image> kv in playerDots)
        {
            if (kv.Key == null)
            {
                removeMotors.Add(kv.Key);
                if (kv.Value != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }
        }
        for (int i = 0; i < removeMotors.Count; i++)
        {
            playerDots.Remove(removeMotors[i]);
        }
        removeEnemies.Clear();
        foreach (KeyValuePair<EnemyAI, Image> kv in enemyDots)
        {
            if (kv.Key == null)
            {
                removeEnemies.Add(kv.Key);
                if (kv.Value != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }
        }
        for (int i = 0; i < removeEnemies.Count; i++)
        {
            enemyDots.Remove(removeEnemies[i]);
        }
    }
    private Image NewDot(string name, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = markers.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(markers, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.sprite = dotSprite;
        img.color = color;
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }
    private static Color TeamColor(CharController_Motor m)
    {
        int number = 1;
        if (PhotonNetwork.InRoom && m.photonView != null && PhotonNetwork.PlayerList != null)
        {
            List<int> ids = new List<int>();
            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
            {
                ids.Add(players[i].ActorNumber);
            }
            ids.Sort();
            int index = ids.IndexOf(m.photonView.OwnerActorNr);
            number = index < 0 ? 1 : index + 1;
        }
        return TeamColors[Mathf.Clamp(number - 1, 0, TeamColors.Length - 1)];
    }
    public static Texture2D MakeDiscTexture(int size, float border)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[size * size];
        float r = size * 0.5f - 1f;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d + 0.5f);
                float w = border > 0f ? Mathf.Clamp01(r - border - d + 0.5f) : 1f;
                px[y * size + x] = new Color(w, w, w, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    public static Texture2D MakeGridTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color line = new Color(0.75f, 0.78f, 0.72f, 0.22f);
        Color sub = new Color(0.75f, 0.78f, 0.72f, 0.08f);
        Color[] px = new Color[size * size];
        int quarter = size / 4;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = clear;
                if (x == 0 || y == 0) c = line;
                else if (x % quarter == 0 || y % quarter == 0) c = sub;
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    public static Texture2D MakeArrowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[size * size];
        Vector2 tip = new Vector2(size * 0.5f, size * 0.96f);
        Vector2 left = new Vector2(size * 0.1f, size * 0.06f);
        Vector2 notch = new Vector2(size * 0.5f, size * 0.3f);
        Vector2 right = new Vector2(size * 0.9f, size * 0.06f);
        Vector2 c = new Vector2(size * 0.5f, size * 0.45f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool outer = InArrow(p, tip, left, notch, right);
                bool inner = InArrow(c + (p - c) / 0.7f, tip, left, notch, right);
                float v = inner ? 1f : 0f;
                px[y * size + x] = new Color(v, v, v, outer ? 1f : 0f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    private static bool InArrow(Vector2 p, Vector2 tip, Vector2 left, Vector2 notch, Vector2 right)
    {
        return InTriangle(p, tip, left, notch) || InTriangle(p, tip, notch, right);
    }
    private struct HudVisibilityState
    {
        public readonly GameObject target;
        public readonly bool wasActive;
        public HudVisibilityState(GameObject target, bool wasActive)
        {
            this.target = target;
            this.wasActive = wasActive;
        }
    }
    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }
}