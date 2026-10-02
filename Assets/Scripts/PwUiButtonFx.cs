using UnityEngine;
using UnityEngine.EventSystems;
public sealed class PwUiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.04f;
    public float pressScale = 0.94f;
    public float speed = 16f;
    private RectTransform rect;
    private Vector3 baseScale;
    private Vector3 targetScale;
    private void Awake()
    {
        rect = transform as RectTransform;
        baseScale = rect != null ? rect.localScale : Vector3.one;
        targetScale = baseScale;
    }
    private void Update()
    {
        if (rect == null) return;
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = baseScale * pressScale;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;
    }
    private void OnDisable()
    {
        if (rect != null) rect.localScale = baseScale;
    }
}