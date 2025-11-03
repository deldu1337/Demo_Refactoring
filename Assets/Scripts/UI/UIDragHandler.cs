using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform targetPanel; // 이동시킬 전체 패널
    private Vector2 offset;

    private Canvas canvas;

    void Awake()
    {
        if (targetPanel == null)
            targetPanel = transform.parent as RectTransform; // 기본: 부모 패널

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetPanel,
            eventData.position,
            eventData.pressEventCamera,
            out offset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetPanel.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            targetPanel.localPosition = localPoint - offset;
        }
    }
}
