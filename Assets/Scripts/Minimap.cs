using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class Minimap : MonoBehaviour
{
    [Header("Full Map")]
    public float fullMapRange = 220f;
    public float fullMapMinRange = 25f;
    public float fullMapMaxRange = 420f;
    public bool showMonsterMarkers = true;
    private GameObject fullMapPanel;
    private RawImage fullMapImage;
    private RectTransform fullMapMarkers;
    private Button closeMapButton;
    private Button zoomInButton;
    private Button zoomOutButton;
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
    private Sprite generatedDot;
    private CharController_Motor localMotor;
    private GameObject loadingPanel;
    private float nextRender;
    private float nextSearch;
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
        if (dotSprite == null)
        {
            Texture2D tex = MakeDiscTexture(64, 7f);
            generatedDot = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
            dotSprite = generatedDot;
        }
        texture = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
        texture.name = "MinimapTexture";
        texture.Create();
        mapImage.texture = texture;
        BuildCamera();
        BuildFullMap();
        CreateOpenButton();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform lp = canvas.rootCanvas.transform.Find("LoadingPanel");
            loadingPanel = lp != null ? lp.gameObject : null;
        }
    }
    private void OnDestroy()
    {
        if (fullMapPanel != null)
        {
            Destroy(fullMapPanel);
        }
        if (mapCamera != null)
        {
            mapCamera.targetTexture = null;
            Destroy(mapCamera.gameObject);
        }
        if (mapImage != null && mapImage.texture == texture)
        {
            mapImage.texture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
        if (generatedDot != null)
        {
            Destroy(generatedDot.texture);
            Destroy(generatedDot);
        }
    }
    private void BuildCamera()
    {
        GameObject go = new GameObject("MinimapCamera");
        mapCamera = go.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = range;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.04f, 0.02f, 0.02f, 1f);
        mapCamera.cullingMask = ~(1 << 5);
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = height + 120f;
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.useOcclusionCulling = false;
        mapCamera.depth = -50f;
        mapCamera.targetTexture = texture;
        mapCamera.enabled = false;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
    private void CreateOpenButton()
    {
        Button button = gameObject.GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OpenFullMap);
    }
    private void BuildFullMap()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        fullMapPanel = new GameObject("FullMapPanel", typeof(RectTransform), typeof(Image));
        fullMapPanel.layer = 5;
        fullMapPanel.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform panel = fullMapPanel.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        Image dim = fullMapPanel.GetComponent<Image>();
        dim.color = new Color(0.01f, 0.005f, 0.008f, 0.94f);
        dim.raycastTarget = true;
        fullMapImage = CreateMapImage(panel, "FullMap", new Color(0.72f, 0.68f, 0.62f, 1f));
        fullMapImage.texture = texture;
        fullMapImage.rectTransform.anchorMin = new Vector2(0.08f, 0.1f);
        fullMapImage.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
        fullMapImage.rectTransform.offsetMin = Vector2.zero;
        fullMapImage.rectTransform.offsetMax = Vector2.zero;
        fullMapMarkers = new GameObject("Markers", typeof(RectTransform)).GetComponent<RectTransform>();
        fullMapMarkers.SetParent(fullMapImage.transform, false);
        fullMapMarkers.anchorMin = Vector2.zero;
        fullMapMarkers.anchorMax = Vector2.one;
        fullMapMarkers.offsetMin = Vector2.zero;
        fullMapMarkers.offsetMax = Vector2.zero;
        Button close = CreateMapButton(panel, "CloseMap", "X", new Vector2(0.92f, 0.9f), new Vector2(120f, 80f));
        closeMapButton = close;
        close.onClick.AddListener(CloseFullMap);
        zoomInButton = CreateMapButton(panel, "ZoomIn", "+", new Vector2(0.88f, 0.2f), new Vector2(100f, 80f));
        zoomOutButton = CreateMapButton(panel, "ZoomOut", "−", new Vector2(0.88f, 0.1f), new Vector2(100f, 80f));
        zoomInButton.onClick.AddListener(() => SetFullMapZoom(fullMapZoom - 20f));
        zoomOutButton.onClick.AddListener(() => SetFullMapZoom(fullMapZoom + 20f));
        fullMapPanel.SetActive(false);
    }
    private RawImage CreateMapImage(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }
    private Button CreateMapButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.02f, 0.025f, 0.88f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 42;
        text.color = new Color(0.95f, 0.88f, 0.82f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return button;
    }
    private void OpenFullMap()
    {
        if (fullMapPanel == null) return;
        if (!hasWorldBounds) CalculateWorldBounds();
        Search();
        fullMapOpen = true;
        fullMapPanel.SetActive(true);
        fullMapZoom = Mathf.Clamp(Mathf.Max(worldBounds.size.x, worldBounds.size.z) * 0.58f, fullMapMinRange, fullMapMaxRange);
        mapCenter = worldBounds.center;
        RenderFullMap();
    }
    private void CloseFullMap()
    {
        fullMapOpen = false;
        if (fullMapPanel != null) fullMapPanel.SetActive(false);
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
        if (!found) bounds = new Bounds(Vector3.zero, new Vector3(fullMapRange, 20f, fullMapRange));
        worldBounds = bounds;
        hasWorldBounds = true;
    }
    private void RenderFullMap()
    {
        if (!fullMapOpen || mapCamera == null || fullMapImage == null) return;
        mapCamera.orthographicSize = fullMapZoom;
        mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y + height + fullMapZoom, mapCenter.z);
        mapCamera.Render();
        UpdateFullMapMarkers();
    }
    private void UpdateFullMapMarkers()
    {
        if (fullMapMarkers == null) return;
        Vector2 size = fullMapMarkers.rect.size;
        for (int i = 0; i < fullMapMarkers.childCount; i++) Destroy(fullMapMarkers.GetChild(i).gameObject);
        if (localMotor != null) AddFullMarker(localMotor.transform.position, TeamColor(localMotor), "Me");
        foreach (KeyValuePair<CharController_Motor, Image> pair in playerDots)
        {
            if (pair.Key != null && pair.Key != localMotor && pair.Key.gameObject.activeInHierarchy) AddFullMarker(pair.Key.transform.position, TeamColor(pair.Key), "Player");
        }
        if (showMonsterMarkers)
        {
            foreach (KeyValuePair<EnemyAI, Image> pair in enemyDots)
            {
                if (pair.Key != null && pair.Key.gameObject.activeInHierarchy) AddFullMarker(pair.Key.transform.position, new Color(0.9f, 0.04f, 0.04f, 1f), "Monster");
            }
        }
    }
    private void AddFullMarker(Vector3 position, Color color, string name)
    {
        if (fullMapMarkers == null) return;
        float x = Mathf.InverseLerp(mapCenter.x - fullMapZoom, mapCenter.x + fullMapZoom, position.x);
        float y = Mathf.InverseLerp(mapCenter.z - fullMapZoom, mapCenter.z + fullMapZoom, position.z);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(fullMapMarkers, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x, y);
        rt.anchorMax = new Vector2(x, y);
        rt.sizeDelta = new Vector2(22f, 22f);
        Image image = go.GetComponent<Image>();
        image.sprite = dotSprite;
        image.color = color;
        image.raycastTarget = false;
    }
    private void LateUpdate()
    {
        if (fullMapOpen)
        {
            HandleFullMapInput();
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
        mapCamera.transform.position = new Vector3(center.x, center.y + height, center.z);
        bool fog = RenderSettings.fog;
        Color ambient = RenderSettings.ambientLight;
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f, 1f);
        try
        {
            mapCamera.Render();
        }
        finally
        {
            RenderSettings.fog = fog;
            RenderSettings.ambientLight = ambient;
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