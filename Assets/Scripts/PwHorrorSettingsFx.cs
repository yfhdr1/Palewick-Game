using UnityEngine;
using UnityEngine.UI;
public class PwHorrorSettingsFx : MonoBehaviour
{
    private const float ShadowTime = 0.85f;
    private const float ScareTime = 0.16f;
    private const float PulseTime = 0.5f;
    private const float SliderThrottle = 0.8f;
    private const float ScareCooldown = 25f;
    private static PwHorrorSettingsFx instance;
    private static float lastSlider;
    private static float lastScare;
    private RectTransform overlay;
    private Image leftShadow;
    private Image rightShadow;
    private Image scareFace;
    private Image scareFlash;
    private Image vignette;
    private Texture2D shadowTex;
    private Texture2D faceTex;
    private Texture2D vignetteTex;
    private Sprite shadowSprite;
    private Sprite faceSprite;
    private Sprite vignetteSprite;
    private float shadowTimer = -1f;
    private bool shadowLeft;
    private float scareTimer = -1f;
    private float pulseTimer = -1f;
    private float idleTimer;
    private float nextIdle = 14f;
    public static void Attach(RectTransform host)
    {
        if (host == null) return;
        PwHorrorSettingsFx fx = host.GetComponent<PwHorrorSettingsFx>();
        if (fx == null) fx = host.gameObject.AddComponent<PwHorrorSettingsFx>();
        instance = fx;
    }
    public static void MenuOpened()
    {
        if (instance == null) return;
        instance.idleTimer = 0f;
        if (Random.value < 0.3f) instance.PlayShadow();
    }
    public static void TabChanged()
    {
        if (instance == null || !instance.isActiveAndEnabled) return;
        float roll = Random.value;
        if (roll < 0.08f && Time.unscaledTime - lastScare > ScareCooldown) instance.PlayScare();
        else if (roll < 0.45f) instance.PlayShadow();
        instance.PlayPulse();
    }
    public static void SliderMoved()
    {
        if (instance == null || !instance.isActiveAndEnabled) return;
        if (Time.unscaledTime - lastSlider < SliderThrottle) return;
        lastSlider = Time.unscaledTime;
        instance.PlayPulse();
        if (Random.value < 0.12f) instance.PlayShadow();
    }
    private void Awake()
    {
        instance = this;
        BuildOverlay();
    }
    private void OnEnable()
    {
        idleTimer = 0f;
        nextIdle = Random.Range(10f, 20f);
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (shadowSprite != null) Destroy(shadowSprite);
        if (faceSprite != null) Destroy(faceSprite);
        if (vignetteSprite != null) Destroy(vignetteSprite);
        if (shadowTex != null) Destroy(shadowTex);
        if (faceTex != null) Destroy(faceTex);
        if (vignetteTex != null) Destroy(vignetteTex);
    }
    private void BuildOverlay()
    {
        shadowTex = MakeShadowTexture(96, 224);
        faceTex = MakeFaceTexture(256);
        vignetteTex = MakeVignetteTexture(128);
        shadowSprite = Sprite.Create(shadowTex, new Rect(0f, 0f, shadowTex.width, shadowTex.height), new Vector2(0.5f, 0.5f), 100f);
        faceSprite = Sprite.Create(faceTex, new Rect(0f, 0f, faceTex.width, faceTex.height), new Vector2(0.5f, 0.5f), 100f);
        vignetteSprite = Sprite.Create(vignetteTex, new Rect(0f, 0f, vignetteTex.width, vignetteTex.height), new Vector2(0.5f, 0.5f), 100f);
        GameObject go = new GameObject("HorrorFxOverlay", typeof(RectTransform));
        go.layer = gameObject.layer;
        overlay = go.GetComponent<RectTransform>();
        overlay.SetParent(transform, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
        vignette = NewImage("Vignette", vignetteSprite, new Color(0.25f, 0f, 0.02f, 0f));
        Stretch(vignette.rectTransform);
        leftShadow = NewImage("ShadowL", shadowSprite, new Color(0f, 0f, 0f, 0f));
        SetupShadow(leftShadow.rectTransform, true);
        rightShadow = NewImage("ShadowR", shadowSprite, new Color(0f, 0f, 0f, 0f));
        SetupShadow(rightShadow.rectTransform, false);
        scareFlash = NewImage("ScareFlash", null, new Color(0.08f, 0f, 0.01f, 0f));
        Stretch(scareFlash.rectTransform);
        scareFace = NewImage("ScareFace", faceSprite, new Color(1f, 1f, 1f, 0f));
        RectTransform fr = scareFace.rectTransform;
        fr.anchorMin = new Vector2(0.5f, 0.5f);
        fr.anchorMax = new Vector2(0.5f, 0.5f);
        fr.sizeDelta = new Vector2(620f, 620f);
    }
    private Image NewImage(string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(overlay, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    private void SetupShadow(RectTransform rt, bool left)
    {
        rt.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rt.anchorMax = new Vector2(left ? 0f : 1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(300f, 760f);
        rt.anchoredPosition = new Vector2(left ? 90f : -90f, 20f);
        if (!left) rt.localScale = new Vector3(-1f, 1f, 1f);
    }
    private void PlayShadow()
    {
        shadowLeft = Random.value < 0.5f;
        shadowTimer = 0f;
    }
    private void PlayScare()
    {
        lastScare = Time.unscaledTime;
        scareTimer = 0f;
    }
    private void PlayPulse()
    {
        pulseTimer = 0f;
    }
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        idleTimer += dt;
        if (idleTimer >= nextIdle)
        {
            idleTimer = 0f;
            nextIdle = Random.Range(12f, 26f);
            PlayShadow();
        }
        UpdateShadow(dt);
        UpdateScare(dt);
        UpdatePulse(dt);
    }
    private void UpdateShadow(float dt)
    {
        if (shadowTimer < 0f) return;
        shadowTimer += dt;
        float k = shadowTimer / ShadowTime;
        Image img = shadowLeft ? leftShadow : rightShadow;
        Image other = shadowLeft ? rightShadow : leftShadow;
        if (other != null) other.color = new Color(0f, 0f, 0f, 0f);
        if (img == null) return;
        if (k >= 1f)
        {
            img.color = new Color(0f, 0f, 0f, 0f);
            shadowTimer = -1f;
            return;
        }
        float alpha = Mathf.Sin(k * Mathf.PI);
        alpha *= 0.55f + Mathf.PerlinNoise(Time.unscaledTime * 14f, 3.7f) * 0.25f;
        img.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        RectTransform rt = img.rectTransform;
        float rise = Mathf.Lerp(-80f, 60f, EaseOut(k));
        rt.anchoredPosition = new Vector2((shadowLeft ? 90f : -90f) + Mathf.Sin(k * 9f) * 8f, rise);
    }
    private void UpdateScare(float dt)
    {
        if (scareTimer < 0f) return;
        scareTimer += dt;
        float k = scareTimer / ScareTime;
        if (k >= 1f)
        {
            scareFace.color = new Color(1f, 1f, 1f, 0f);
            scareFlash.color = new Color(0.08f, 0f, 0.01f, 0f);
            scareTimer = -1f;
            return;
        }
        float a = k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
        scareFace.color = new Color(1f, 1f, 1f, a * 0.92f);
        scareFlash.color = new Color(0.08f, 0f, 0.01f, a * 0.85f);
        float s = Mathf.Lerp(1.25f, 1f, k);
        scareFace.rectTransform.localScale = new Vector3(s, s, 1f);
        scareFace.rectTransform.anchoredPosition = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
    }
    private void UpdatePulse(float dt)
    {
        if (pulseTimer < 0f) return;
        pulseTimer += dt;
        float k = pulseTimer / PulseTime;
        if (k >= 1f)
        {
            vignette.color = new Color(0.25f, 0f, 0.02f, 0f);
            pulseTimer = -1f;
            return;
        }
        float a = Mathf.Sin(k * Mathf.PI) * 0.4f;
        vignette.color = new Color(0.25f, 0f, 0.02f, a);
    }
    private static float EaseOut(float k)
    {
        return 1f - (1f - k) * (1f - k);
    }
    public static Texture2D MakeShadowTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;
                float a = 0f;
                float headY = 0.84f;
                float head = 1f - Mathf.Clamp01((Dist(u, v, 0.5f, headY, 1f, 0.42f) - 0.09f) / 0.05f);
                float bodyWidth = Mathf.Lerp(0.3f, 0.1f, Mathf.Pow(Mathf.Clamp01((0.78f - v) / 0.78f), 0.6f));
                bodyWidth = Mathf.Lerp(0.16f, bodyWidth, Mathf.Clamp01((0.78f - v) * 8f));
                float body = v < 0.8f ? 1f - Mathf.Clamp01((Mathf.Abs(u - 0.5f) - bodyWidth) / 0.07f) : 0f;
                float rag = Mathf.PerlinNoise(u * 9f, v * 9f) * 0.35f;
                a = Mathf.Max(head, body) - rag * Mathf.Clamp01(1.2f - v);
                a *= Mathf.Clamp01(v * 6f);
                px[y * w + x] = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    private static Texture2D MakeFaceTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float face = 1f - Mathf.Clamp01((Dist(u, v, 0.5f, 0.52f, 1f, 1.25f) - 0.34f) / 0.06f);
                float noise = Mathf.PerlinNoise(u * 14f, v * 14f);
                float eyeL = 1f - Mathf.Clamp01((Dist(u, v, 0.36f, 0.62f, 1f, 1.5f) - 0.075f) / 0.035f);
                float eyeR = 1f - Mathf.Clamp01((Dist(u, v, 0.64f, 0.62f, 1f, 1.5f) - 0.075f) / 0.035f);
                float mouth = 1f - Mathf.Clamp01((Dist(u, v, 0.5f, 0.3f, 1.4f, 0.8f) - 0.09f) / 0.04f);
                float g = 0.52f + noise * 0.16f;
                Color c = new Color(g * 0.82f, g * 0.86f, g * 0.8f, face * 0.96f);
                float hole = Mathf.Max(Mathf.Max(eyeL, eyeR), mouth);
                if (hole > 0f)
                {
                    Color dark = new Color(0.04f, 0.005f, 0.008f, 1f);
                    c = Color.Lerp(c, dark, hole);
                    c.a = Mathf.Max(c.a, hole * face);
                }
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    private static Texture2D MakeVignetteTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float d = Dist(u, v, 0.5f, 0.5f, 1f, 1f);
                float a = Mathf.Clamp01((d - 0.3f) / 0.4f);
                px[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
    private static float Dist(float u, float v, float cx, float cy, float sx, float sy)
    {
        float dx = (u - cx) / Mathf.Max(0.0001f, sx);
        float dy = (v - cy) / Mathf.Max(0.0001f, sy);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
