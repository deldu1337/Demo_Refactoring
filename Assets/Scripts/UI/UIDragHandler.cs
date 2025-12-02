using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform targetPanel;
    private Vector2 offset;

    private Canvas canvas;

    /// <summary>
    /// 드래그 대상 패널과 캔버스를 초기화합니다.
    /// </summary>
    void Awake()
    {
        if (targetPanel == null)
            targetPanel = transform.parent as RectTransform;

        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// 드래그 시작 시 포인터와 패널 사이의 오프셋을 계산합니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetPanel,
            eventData.position,
            eventData.pressEventCamera,
            out offset);
    }

    /// <summary>
    /// 드래그 중 패널 위치를 포인터 위치에 맞게 이동합니다.
    /// </summary>
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
