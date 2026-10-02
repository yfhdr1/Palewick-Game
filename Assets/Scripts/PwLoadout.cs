using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class PwLoadout : MonoBehaviour
{
    public const string LookProp = "pw_look";
    private const float Interval = 1f;
    private static readonly Color[] SkinTones =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(0.48f, 0.13f, 0.11f, 1f),
        new Color(0.56f, 0.58f, 0.64f, 1f),
        new Color(0.32f, 0.36f, 0.22f, 1f)
    };
    private static readonly Color[] LampColors =
    {
        new Color(1f, 0.96f, 0.88f, 1f),
        new Color(0.5f, 1f, 0.58f, 1f),
        new Color(0.62f, 0.78f, 1f, 1f)
    };
    private static readonly HashSet<Light> wardedLights = new HashSet<Light>();
    private static PwLoadout instance;
    private static readonly Dictionary<Transform, int> appliedFlash = new Dictionary<Transform, int>();
    private float next;
    private int pushedLook = -1;
    public static Color SkinToneFor(int index)
    {
        return SkinTones[Mathf.Clamp(index, 0, SkinTones.Length - 1)];
    }
    public static Color LampColorFor(int index)
    {
        return LampColors[Mathf.Clamp(index, 0, LampColors.Length - 1)];
    }
    public static bool IsWarded(Light light)
    {
        return light != null && wardedLights.Contains(light);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PwLoadout");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PwLoadout>();
    }
    private void Awake()
    {
        instance = this;
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    private void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + Interval;
        PushLook();
        ApplyAll();
    }
    private void PushLook()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        int value = PwShop.Look;
        if (pushedLook == value) return;
        pushedLook = value;
        Hashtable props = new Hashtable();
        props[LookProp] = value;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    private void ApplyAll()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerHealth player = players[i];
            if (player == null) continue;
            PhotonView view = player.GetComponent<PhotonView>();
            bool mine = !PhotonNetwork.InRoom || view == null || view.IsMine;
            int look = 0;
            if (mine)
            {
                look = PwShop.Look;
            }
            else if (view != null && view.Owner != null)
            {
                object raw;
                if (view.Owner.CustomProperties.TryGetValue(LookProp, out raw) && raw is int) look = (int)raw;
            }
            int skin = look % 10;
            int lamp = (look / 10) % 10;
            bool upgraded = (look / 100) % 10 != 0;
            bool warded = look >= 1000;
            ApplySkin(player.transform, skin);
            Flashlight(player.transform, lamp, upgraded, warded);
        }
    }
    public static void ApplySkin(Transform root, int index)
    {
        if (root == null) return;
        Color tone = SkinToneFor(index);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !(r is SkinnedMeshRenderer)) continue;
            Material m = r.material;
            if (m == null || !m.HasProperty("_Color")) continue;
            if (m.color != tone) m.color = tone;
        }
    }
    private static void Clean()
    {
        if (appliedFlash.Count < 8) return;
        List<Transform> dead = new List<Transform>();
        foreach (KeyValuePair<Transform, int> pair in appliedFlash)
        {
            if (pair.Key == null) dead.Add(pair.Key);
        }
        for (int i = 0; i < dead.Count; i++)
        {
            appliedFlash.Remove(dead[i]);
        }
        wardedLights.RemoveWhere(l => l == null);
    }
    private static void Flashlight(Transform root, int lamp, bool upgraded, bool warded)
    {
        if (root == null) return;
        Clean();
        int want = (warded ? 1000 : 0) + (upgraded ? 100 : 0) + Mathf.Clamp(lamp, 0, 2);
        int done;
        if (appliedFlash.TryGetValue(root, out done) && done == want) return;
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null || l.type != LightType.Spot) continue;
            l.color = LampColorFor(lamp);
            if (upgraded)
            {
                l.range = 45f;
                l.spotAngle = 95f;
                l.intensity = 6f;
            }
            else
            {
                l.range = 30f;
                l.spotAngle = 80f;
                l.intensity = 4f;
            }
            PwFlashlightFx fx = l.GetComponent<PwFlashlightFx>();
            if (lamp > 0)
            {
                if (fx == null) fx = l.gameObject.AddComponent<PwFlashlightFx>();
                fx.Configure(l, lamp, l.intensity);
            }
            else if (fx != null)
            {
                fx.Restore();
                Destroy(fx);
            }
            if (warded) wardedLights.Add(l);
            else wardedLights.Remove(l);
        }
        appliedFlash[root] = want;
    }
}
public class PwFlashlightFx : MonoBehaviour
{
    private Light target;
    private int mode;
    private float baseIntensity;
    private float seed;
    public void Configure(Light light, int styleMode, float intensity)
    {
        target = light;
        mode = styleMode;
        baseIntensity = intensity;
        if (seed == 0f) seed = Random.Range(0f, 100f);
    }
    public void Restore()
    {
        if (target != null) target.intensity = baseIntensity;
    }
    private void Update()
    {
        if (target == null) return;
        float t = Time.time + seed;
        if (mode == 1)
        {
            float n = Mathf.PerlinNoise(t * 9f, seed);
            float dip = n > 0.86f ? 0.45f : 1f;
            target.intensity = baseIntensity * Mathf.Lerp(0.88f, 1.05f, n) * dip;
        }
        else if (mode == 2)
        {
            target.intensity = baseIntensity * (0.92f + Mathf.Sin(t * 1.6f) * 0.1f);
        }
    }
}
