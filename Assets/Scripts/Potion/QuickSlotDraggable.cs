using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class QuickSlotDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PotionSlotUI slot;          // 이 아이콘이 속한 슬롯 참조
    public Canvas canvas;              // 드래그용
    private RectTransform rt;
    private Transform originalParent;
    private CanvasGroup cg;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = gameObject.GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty) return;
        originalParent = transform.parent;

        // 원래 부모 안의 Qty 비활성화 (있을 경우)
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.gameObject.SetActive(false);
        }

        cg.blocksRaycasts = false;
        transform.SetParent(canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty) return;
        rt.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        var qb = PotionQuickBar.Instance;

        // 드래그 끝나면 원래 부모의 Qty 다시 활성화 (있을 경우)
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.gameObject.SetActive(true);
        }

        // 1) 포션 슬롯 위로 드롭 → Move
        if (qb && qb.TryGetSlotIndexAtScreenPosition(eventData.position, out int targetIndex))
        {
            if (slot != null && targetIndex != slot.index)
                qb.Move(slot.index, targetIndex);

            SnapBack();
            return;
        }

        // 2) 그 외(어디든) → 인벤토리로 반환
        if (slot != null)
            qb?.ReturnToInventory(slot.index);   // ← 핵심 한 줄

        SnapBack();
    }

    private void SnapBack()
    {
        transform.SetParent(originalParent, false);
        rt.anchoredPosition = Vector2.zero;

        // 부모(Potion1) 안에서 Text가 항상 맨 위로 오게 보장
        if (originalParent != null)
        {
            var qtyTr = originalParent.Find("Qty");
            if (qtyTr) qtyTr.SetAsLastSibling();

            var textTr = originalParent.Find("Text (Legacy)");
            if (textTr) textTr.SetAsLastSibling();
        }
    }
}
