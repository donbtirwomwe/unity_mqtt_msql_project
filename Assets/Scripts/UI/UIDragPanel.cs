using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform target;

    private RectTransform _targetRect;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private Vector2 _dragOffset;

    void Awake()
    {
        _targetRect = target != null ? target : GetComponent<RectTransform>();
        if (_targetRect != null)
            _parentRect = _targetRect.parent as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_targetRect == null || _parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            _dragOffset = _targetRect.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRect == null || _parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            _targetRect.anchoredPosition = localPoint + _dragOffset;
        }
    }
}
