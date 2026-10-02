using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Actions exposed by the compact mobile HUD. Values used by the old layout are kept
/// for serialized-scene compatibility, but the clean HUD only creates Jump, Interact
/// and Flashlight buttons.
/// </summary>
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
/// Binds one fixed joystick and a small set of action buttons to the local player.
/// There are deliberately no layout preferences, edit mode, sliders or settings UI.
/// </summary>
public class PwTouchControls : MonoBehaviour
{
    public Joystick moveJoystick;
    public List<PwTouchButton> buttons = new List<PwTouchButton>();
    public CharController_Motor motor;
    public bool hideWhenNoPlayer = true;
    [Range(0.25f, 1f)] public float opacity = 0.88f;

    private CanvasGroup canvasGroup;
    private PlayerInteraction interaction;
    private FlashlightController flashlight;
    private float nextSearchTime;

    public CharController_Motor Motor
    {
        get { return motor; }
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        WireButtons();
    }

    private void OnEnable()
    {
        nextSearchTime = 0f;
        BindLocalPlayer(true);
    }

    private void Update()
    {
        BindLocalPlayer(false);

        if (canvasGroup == null)
        {
            return;
        }

        bool available = motor != null || !hideWhenNoPlayer;
        float targetAlpha = available ? opacity : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime * 5f);
        canvasGroup.blocksRaycasts = available;
        canvasGroup.interactable = available;
    }

    private void WireButtons()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].owner = this;
            }
        }
    }

    private void BindLocalPlayer(bool immediate)
    {
        if (motor != null && motor.IsLocal && motor.isActiveAndEnabled)
        {
            if (moveJoystick != null && motor.moveJoystick != moveJoystick)
            {
                motor.moveJoystick = moveJoystick;
            }
            return;
        }

        if (!immediate && Time.unscaledTime < nextSearchTime)
        {
            return;
        }

        nextSearchTime = Time.unscaledTime + 0.5f;
        motor = null;
        interaction = null;
        flashlight = null;

        CharController_Motor[] motors = FindObjectsByType<CharController_Motor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
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
            return;
        }

        if (moveJoystick != null)
        {
            motor.moveJoystick = moveJoystick;
        }

        interaction = motor.GetComponentInChildren<PlayerInteraction>(true);
        flashlight = motor.GetComponentInChildren<FlashlightController>(true);
        if (flashlight == null)
        {
            flashlight = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
        }
        WireButtons();
    }

    public void Press(PwTouchAction action)
    {
        BindLocalPlayer(true);

        switch (action)
        {
            case PwTouchAction.Jump:
                if (motor != null)
                {
                    motor.Jump();
                }
                break;

            case PwTouchAction.Interact:
                if (interaction != null)
                {
                    interaction.OnInteractButtonPressed();
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
        // The clean HUD has no hold/toggle layout controls. Kept as an explicit hook
        // so action buttons always have symmetrical pointer-down/up handling.
    }
}

/// <summary>A simple pressable HUD action with visual touch feedback.</summary>
public class PwTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PwTouchControls owner;
    public PwTouchAction action = PwTouchAction.Jump;
    public Image background;
    public Image icon;
    public Color idleColor = new Color(1f, 1f, 1f, 0.86f);
    public Color pressedColor = new Color(1f, 0.68f, 0.52f, 1f);
    public float pressScale = 0.92f;
    public float returnSpeed = 16f;
    public UnityEvent onPressed = new UnityEvent();
    public UnityEvent onReleased = new UnityEvent();

    private Vector3 baseScale = Vector3.one;
    private bool pressed;

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<PwTouchControls>();
        }
        if (background == null)
        {
            background = GetComponent<Image>();
        }
        baseScale = transform.localScale;
        ApplyColor();
    }

    private void OnEnable()
    {
        pressed = false;
        baseScale = transform.localScale;
        transform.localScale = baseScale;
        ApplyColor();
    }

    private void Update()
    {
        float scale = pressed ? pressScale : 1f;
        Vector3 target = new Vector3(baseScale.x * scale, baseScale.y * scale, 1f);
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            target,
            Time.unscaledDeltaTime * returnSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        ApplyColor();
        if (owner != null)
        {
            owner.Press(action);
        }
        onPressed.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        ApplyColor();
        if (owner != null)
        {
            owner.Release(action);
        }
        onReleased.Invoke();
    }

    private void OnDisable()
    {
        pressed = false;
        transform.localScale = baseScale;
        ApplyColor();
    }

    private void ApplyColor()
    {
        Image target = icon != null ? icon : background;
        if (target != null)
        {
            target.color = pressed ? pressedColor : idleColor;
        }
    }
}
