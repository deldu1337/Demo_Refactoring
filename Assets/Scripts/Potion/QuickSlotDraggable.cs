using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
/// <summary>
/// 포션 슬롯 아이콘을 드래그하여 이동하거나 반환하는 기능을 제공합니다.
/// </summary>
public class QuickSlotDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PotionSlotUI slot;          // 연결된 슬롯 정보를 보관합니다.
    public Canvas canvas;              // 드래그 시 사용할 캔버스를 참조합니다.
    private RectTransform rt;
    private Transform originalParent;
    private CanvasGroup cg;

    /// <summary>
    /// 드래그에 필요한 컴포넌트를 준비합니다.
    /// </summary>
    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = gameObject.GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// 드래그를 시작할 때 호출됩니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty) return;
        originalParent = transform.parent;

        // 드래그 중에는 수량 표기를 잠시 숨기겠습니다.
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.gameObject.SetActive(false);
        }

        cg.blocksRaycasts = false;
        transform.SetParent(canvas.transform, true);
    }

    /// <summary>
    /// 드래그 중 위치를 따라 이동합니다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty) return;
        rt.position = eventData.position;
    }

    /// <summary>
    /// 드래그를 종료하면서 슬롯 이동 또는 반환을 처리합니다.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        var qb = PotionQuickBar.Instance;

        // 드래그가 끝나면 수량 표기를 다시 보이게 하겠습니다.
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.gameObject.SetActive(true);
        }

        // 1) 다른 포션 슬롯으로 이동했는지 확인합니다.
        if (qb && qb.TryGetSlotIndexAtScreenPosition(eventData.position, out int targetIndex))
        {
            if (slot != null && targetIndex != slot.index)
                qb.Move(slot.index, targetIndex);

            SnapBack();
            return;
        }

        // 2) 대상 슬롯이 없다면 인벤토리로 되돌립니다.
        if (slot != null)
            qb?.ReturnToInventory(slot.index);

        SnapBack();
    }

    /// <summary>
    /// 드래그 대상의 위치와 부모를 원래대로 돌려놓습니다.
    /// </summary>
    private void SnapBack()
    {
        transform.SetParent(originalParent, false);
        rt.anchoredPosition = Vector2.zero;

        // 원래 슬롯 안에서 수량과 텍스트가 위에 보이도록 정렬합니다.
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.SetAsLastSibling();

            var textTr = originalParent.Find("Text (Legacy)");
            if (textTr) textTr.SetAsLastSibling();
        }
    }
}
