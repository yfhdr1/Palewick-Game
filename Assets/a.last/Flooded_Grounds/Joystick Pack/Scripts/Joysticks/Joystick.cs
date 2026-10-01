using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float Horizontal => input.x;
    public float Vertical => input.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);

    public float HandleRange
    {
        get => handleRange;
        set => handleRange = Mathf.Abs(value);
    }

    public float DeadZone
    {
        get => deadZone;
        set => deadZone = Mathf.Abs(value);
    }

    public AxisOptions AxisOptions { get => axisOptions; set => axisOptions = value; }
    public bool SnapX { get => snapX; set => snapX = value; }
    public bool SnapY { get => snapY; set => snapY = value; }

    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0f;
    [SerializeField] private AxisOptions axisOptions = AxisOptions.Both;
    [SerializeField] private bool snapX = false;
    [SerializeField] private bool snapY = false;

    [SerializeField] protected RectTransform background = null;
    [SerializeField] protected RectTransform handle = null;

    private Canvas canvas;
    private Camera cam;
    private Vector2 input = Vector2.zero;

    protected virtual void Start()
    {
        HandleRange = handleRange;
        DeadZone = deadZone;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;

        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0) handle = transform.GetChild(0) as RectTransform;

        Vector2 center = new Vector2(0.5f, 0.5f);
        if (background != null)
        {
            background.pivot = center;
        }
        if (handle != null)
        {
            handle.pivot = center;
            handle.anchorMin = center;
            handle.anchorMax = center;
            handle.anchoredPosition = Vector2.zero;
        }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, cam, out position);

        Vector2 bgSize = background.rect.size;
        if (bgSize.x <= 0 || bgSize.y <= 0) bgSize = background.sizeDelta;
        if (bgSize.x <= 0 || bgSize.y <= 0) bgSize = new Vector2(250f, 250f);

        Vector2 radius = bgSize * 0.5f;
        input = position / radius;
        FormatInput();

        if (input.magnitude > deadZone)
        {
            if (input.magnitude > 1f) input = input.normalized;
        }
        else
        {
            input = Vector2.zero;
        }

        handle.anchoredPosition = input * radius * handleRange;
    }

    private void FormatInput()
    {
        if (axisOptions == AxisOptions.Horizontal)
            input = new Vector2(input.x, 0f);
        else if (axisOptions == AxisOptions.Vertical)
            input = new Vector2(0f, input.y);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}

public enum AxisOptions { Both, Horizontal, Vertical }