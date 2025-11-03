using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryItem item;
    private RectTransform selfRect;

    private ItemOrigin context = ItemOrigin.Inventory;
    public void SetContext(ItemOrigin origin) => context = origin;

    void Awake() => selfRect = transform as RectTransform;

    void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide(this);
    }

    public void SetItem(InventoryItem it) => item = it;

    // ★ 착용 아이템 유효성 검사 (유령 레퍼런스 차단)
    private static bool IsValidEquipped(InventoryItem it)
        => it != null
        && it.data != null
        && !string.IsNullOrEmpty(it.uniqueId)
        && it.id != 0;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null)
        {
            ItemTooltipUI.Instance?.Hide(this);
            return;
        }

        // 장비창: 비교 없이 단일 툴팁
        if (context == ItemOrigin.Equipment)
        {
            ItemTooltipUI.Instance?.ShowNextTo(item, selfRect, this); // 내부에서 compareRoot off
            return;
        }

        // 인벤: 같은 슬롯의 "진짜" 착용 아이템만 비교
        InventoryItem equipped = null;
        var equipPresenter = Object.FindAnyObjectByType<EquipmentPresenter>();
        if (equipPresenter != null)
        {
            var slots = equipPresenter.GetEquipmentSlots();
            if (slots != null)
            {
                var same = slots.FirstOrDefault(s => s.slotType == item.data.type);
                if (same != null && IsValidEquipped(same.equipped))
                    equipped = same.equipped;
            }
        }

        if (IsValidEquipped(equipped))
            ItemTooltipUI.Instance?.ShowNextToWithCompare(item, equipped, selfRect, this);
        else
            ItemTooltipUI.Instance?.ShowNextTo(item, selfRect, this); // compareRoot off
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide(this);
    }
}
