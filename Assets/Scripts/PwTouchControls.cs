using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public enum PwTouchAction
{
    None = 0,
    Jump = 1,
    Sprint = 2,
    AutoRun = 3,
    Interact = 4,
    SwitchView = 5,
    Flashlight = 6,
    Custom = 7
}

/// <summary>
/// Runtime brain of the new on-screen controls. It owns the move joystick and the
/// action buttons, keeps them bound to the local player, applies the player's layout
/// preferences (size, opacity, left/right handed) and can run in an edit mode where the
/// buttons can be dragged around and saved (used by the Controls panel in Scene_Lobby).
/// </summary>
public class PwTouchControls : MonoBehaviour
{
    public const string PrefScale = "pw_touch_scale";
    public const string PrefAlpha = "pw_touch_alpha";
    public const string PrefMirror = "pw_touch_mirror";
    public const string PrefHoldSprint = "pw_touch_holdsprint";
    public const string PrefPosPrefix = "pw_touch_pos_";

    public RectTransform layoutRoot;
    public RectTransform joystickRect;
    public RectTransform buttonsRect;
    public Joystick moveJoystick;
    public List<PwTouchButton> buttons = new List<PwTouchButton>();
    public CharController_Motor motor;
    public bool editMode;
    public bool holdToSprint = true;
    [Range(0.6f, 1.6f)] public float buttonScale = 1f;
    [Range(0.25f, 1f)] public float opacity = 0.85f;
    public bool mirrored;
    public bool hideWhenNoPlayer = true;

    private CanvasGroup group;
    private PlayerInteraction interaction;
    private CameraViewSwitcher viewSwitcher;
    private FlashlightController flashlight;
    private bool sprintHeld;
    private bool mirrorApplied;
    private float nextSearchTime;
    private readonly Dictionary<PwTouchButton, Vector2> defaultPositions = new Dictionary<PwTouchButton, Vector2>();

    public CharController_Motor Motor
    {
        get { return motor; }
    }

    public bool SprintHeld
    {
        get { return sprintHeld; }
    }

    private void Awake()
    {
        if (layoutRoot == null)
        {
            layoutRoot = transform as RectTransform;
        }
        group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        CacheDefaults();
        LoadPrefs();
        ApplyLayout();
    }

    private void CacheDefaults()
    {
        defaultPositions.Clear();
        for (int i = 0; i < buttons.Count; i++)
        {
            PwTouchButton button = buttons[i];
            if (button == null)
            {
                continue;
            }
            button.owner = this;
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                defaultPositions[button] = rect.anchoredPosition;
            }
        }
    }

    public void LoadPrefs()
    {
        buttonScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefScale, buttonScale), 0.6f, 1.6f);
        opacity = Mathf.Clamp(PlayerPrefs.GetFloat(PrefAlpha, opacity), 0.25f, 1f);
        mirrored = PlayerPrefs.GetInt(PrefMirror, mirrored ? 1 : 0) == 1;
        holdToSprint = PlayerPrefs.GetInt(PrefHoldSprint, holdToSprint ? 1 : 0) == 1;
    }

    public void SavePrefs()
    {
        PlayerPrefs.SetFloat(PrefScale, buttonScale);
        PlayerPrefs.SetFloat(PrefAlpha, opacity);
        PlayerPrefs.SetInt(PrefMirror, mirrored ? 1 : 0);
        PlayerPrefs.SetInt(PrefHoldSprint, holdToSprint ? 1 : 0);
        for (int i = 0; i < buttons.Count; i++)
        {
            PwTouchButton button = buttons[i];
            RectTransform rect = button == null ? null : button.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }
            Vector2 p = rect.anchoredPosition;
            PlayerPrefs.SetString(PrefPosPrefix + button.name, p.x.ToString("F1", CultureInfo.InvariantCulture) + "," + p.y.ToString("F1", CultureInfo.InvariantCulture));
        }
        PlayerPrefs.Save();
    }

    public void ApplyLayout()
    {
        if (group != null)
        {
            group.alpha = editMode ? 1f : opacity;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        float sign = mirrored ? -1f : 1f;
        for (int i = 0; i < buttons.Count; i++)
        {
            PwTouchButton button = buttons[i];
            RectTransform rect = button == null ? null : button.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }
            Vector2 target;
            if (!TryLoadPosition(button.name, out target))
            {
                Vector2 fallback;
                target = defaultPositions.TryGetValue(button, out fallback) ? fallback : rect.anchoredPosition;
                target.x *= sign;
            }
            rect.anchoredPosition = target;
            rect.localScale = new Vector3(buttonScale, buttonScale, 1f);
            button.BakeBaseScale();
        }
        if (mirrorApplied != mirrored)
        {
            Mirror(joystickRect);
            Mirror(buttonsRect);
            mirrorApplied = mirrored;
        }
    }

    private bool TryLoadPosition(string key, out Vector2 value)
    {
        value = Vector2.zero;
        string raw = PlayerPrefs.GetString(PrefPosPrefix + key, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }
        string[] parts = raw.Split(',');
        float x;
        float y;
        if (parts.Length != 2 ||
            !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
        {
            return false;
        }
        value = new Vector2(x, y);
        return true;
    }

    private static void Mirror(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }
        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        Vector2 pivot = rect.pivot;
        min.x = 1f - min.x;
        max.x = 1f - max.x;
        pivot.x = 1f - pivot.x;
        rect.anchorMin = new Vector2(Mathf.Min(min.x, max.x), min.y);
        rect.anchorMax = new Vector2(Mathf.Max(min.x, max.x), max.y);
        rect.pivot = pivot;
        Vector2 pos = rect.anchoredPosition;
        pos.x = -pos.x;
        rect.anchoredPosition = pos;
    }

    public void SetScale(float value)
    {
        buttonScale = Mathf.Clamp(value, 0.6f, 1.6f);
        ApplyLayout();
    }

    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp(value, 0.25f, 1f);
        ApplyLayout();
    }

    public void SetMirrored(bool value)
    {
        if (mirrored == value)
        {
            return;
        }
        mirrored = value;
        ClearSavedPositions();
        ApplyLayout();
    }

    public void SetHoldToSprint(bool value)
    {
        holdToSprint = value;
    }

    public void SetEditMode(bool value)
    {
        editMode = value;
        ApplyLayout();
    }

    public void ResetLayout()
    {
        ClearSavedPositions();
        buttonScale = 1f;
        opacity = 0.85f;
        mirrored = false;
        holdToSprint = true;
        foreach (KeyValuePair<PwTouchButton, Vector2> pair in defaultPositions)
        {
            RectTransform rect = pair.Key == null ? null : pair.Key.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = pair.Value;
            }
        }
        SavePrefs();
        ApplyLayout();
    }

    private void ClearSavedPositions()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                PlayerPrefs.DeleteKey(PrefPosPrefix + buttons[i].name);
            }
        }
    }

    private void Update()
    {
        if (editMode)
        {
            return;
        }
        Bind();
        if (hideWhenNoPlayer && group != null)
        {
            float target = motor != null ? opacity : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 4f);
            group.blocksRaycasts = motor != null;
        }
    }

    private void LateUpdate()
    {
        if (editMode || motor == null)
        {
            return;
        }
        if (sprintHeld && holdToSprint)
        {
            motor.isSprinting = true;
        }
    }

    private void Bind()
    {
        if (motor != null && motor.IsLocal && motor.isActiveAndEnabled)
        {
            if (moveJoystick != null && motor.moveJoystick != moveJoystick)
            {
                motor.moveJoystick = moveJoystick;
            }
            return;
        }
        if (Time.unscaledTime < nextSearchTime)
        {
            return;
        }
        nextSearchTime = Time.unscaledTime + 0.5f;
        motor = null;
        CharController_Motor[] motors = FindObjectsByType<CharController_Motor>(FindObjectsInactive.Include);
        for (int i = 0; i < motors.Length; i++)
        {
            if (motors[i] != null && motors[i].isActiveAndEnabled && motors[i].IsLocal)
            {
                motor = motors[i];
                break;
            }
        }
        if (motor == null)
        {
            interaction = null;
            viewSwitcher = null;
            flashlight = null;
            return;
        }
        if (moveJoystick != null)
        {
            motor.moveJoystick = moveJoystick;
        }
        interaction = motor.GetComponentInChildren<PlayerInteraction>(true);
        viewSwitcher = motor.cameraSwitcher != null ? motor.cameraSwitcher : motor.GetComponentInChildren<CameraViewSwitcher>(true);
        flashlight = motor.GetComponentInChildren<FlashlightController>(true);
        if (flashlight == null)
        {
            flashlight = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
        }
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].owner = this;
            }
        }
    }

    public void Press(PwTouchAction action)
    {
        if (editMode)
        {
            return;
        }
        Bind();
        switch (action)
        {
            case PwTouchAction.Jump:
                if (motor != null)
                {
                    motor.Jump();
                }
                break;
            case PwTouchAction.Sprint:
                sprintHeld = true;
                if (motor != null)
                {
                    if (holdToSprint)
                    {
                        motor.isSprinting = true;
                    }
                    else
                    {
                        motor.ToggleAutoRun();
                    }
                }
                break;
            case PwTouchAction.AutoRun:
                if (motor != null)
                {
                    motor.ToggleAutoRun();
                }
                break;
            case PwTouchAction.Interact:
                if (interaction != null)
                {
                    interaction.OnInteractButtonPressed();
                }
                break;
            case PwTouchAction.SwitchView:
                if (viewSwitcher != null)
                {
                    viewSwitcher.ToggleView();
                }
                break;
            case PwTouchAction.Flashlight:
                if (flashlight != null)
                {
                    flashlight.ToggleFlashlight();
                }
                break;
        }
    }

    public void Release(PwTouchAction action)
    {
        if (action == PwTouchAction.Sprint)
        {
            sprintHeld = false;
            if (motor != null && holdToSprint)
            {
                motor.isSprinting = false;
            }
        }
    }

    public bool IsActive(PwTouchAction action)
    {
        if (motor == null)
        {
            return false;
        }
        switch (action)
        {
            case PwTouchAction.Sprint:
                return holdToSprint ? sprintHeld : motor.AutoRun;
            case PwTouchAction.AutoRun:
                return motor.AutoRun;
            case PwTouchAction.SwitchView:
                return viewSwitcher != null && viewSwitcher.IsThirdPerson;
            default:
                return false;
        }
    }
}

/// <summary>
/// A single on-screen control. Press feedback, hold support, active-state glow and
/// (in edit mode) drag-to-move so the player can place the buttons where they want.
/// </summary>
public class PwTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public PwTouchControls owner;
    public PwTouchAction action = PwTouchAction.Jump;
    public Image background;
    public Image icon;
    public Text label;
    public Color idleColor = new Color(1f, 1f, 1f, 0.78f);
    public Color activeColor = new Color(1f, 0.52f, 0.42f, 1f);
    public float pressScale = 0.92f;
    public float returnSpeed = 14f;
    public UnityEvent onPressed = new UnityEvent();
    public UnityEvent onReleased = new UnityEvent();

    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private bool pressed;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (icon == null)
        {
            icon = GetComponentInChildren<Image>(true);
        }
        if (owner == null)
        {
            owner = GetComponentInParent<PwTouchControls>();
        }
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        pressed = false;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        float target = pressed ? pressScale : 1f;
        Vector3 wanted = new Vector3(baseScale.x * target, baseScale.y * target, 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, wanted, Time.unscaledDeltaTime * returnSpeed);
        if (icon == null)
        {
            return;
        }
        bool active = owner != null && owner.IsActive(action);
        if (active)
        {
            float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f;
            icon.color = new Color(activeColor.r, activeColor.g, activeColor.b, activeColor.a * pulse);
        }
        else
        {
            icon.color = idleColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        if (owner != null)
        {
            owner.Press(action);
        }
        if (onPressed != null)
        {
            onPressed.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        if (owner != null)
        {
            owner.Release(action);
        }
        if (onReleased != null)
        {
            onReleased.Invoke();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner == null || !owner.editMode || rect == null)
        {
            return;
        }
        Canvas canvas = GetComponentInParent<Canvas>();
        float scale = canvas == null ? 1f : Mathf.Max(0.01f, canvas.scaleFactor);
        rect.anchoredPosition += eventData.delta / scale;
    }

    private void OnDisable()
    {
        pressed = false;
        transform.localScale = baseScale;
    }

    public void BakeBaseScale()
    {
        baseScale = transform.localScale;
    }
}
