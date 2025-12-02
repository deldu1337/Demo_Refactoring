using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryItem item;
    private RectTransform selfRect;

    private ItemOrigin context = ItemOrigin.Inventory;

    /// <summary>
    /// 툴팁이 어느 컨텍스트에서 사용되는지 설정합니다.
    /// </summary>
    public void SetContext(ItemOrigin origin) => context = origin;

    /// <summary>
    /// 자신의 RectTransform을 준비합니다.
    /// </summary>
    void Awake() => selfRect = transform as RectTransform;

    /// <summary>
    /// 비활성화될 때 표시 중인 툴팁을 숨깁니다.
    /// </summary>
    void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide(this);
    }

    /// <summary>
    /// 툴팁에 연결할 인벤토리 아이템을 지정합니다.
    /// </summary>
    public void SetItem(InventoryItem it) => item = it;

    /// <summary>
    /// 착용 아이템이 올바른 참조인지 확인합니다.
    /// </summary>
    private static bool IsValidEquipped(InventoryItem it)
        => it != null
        && it.data != null
        && !string.IsNullOrEmpty(it.uniqueId)
        && it.id != 0;

    /// <summary>
    /// 마우스가 아이콘 위에 올라갔을 때 툴팁을 표시합니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null)
        {
            ItemTooltipUI.Instance?.Hide(this);
            return;
        }

        if (context == ItemOrigin.Equipment)
        {
            ItemTooltipUI.Instance?.ShowNextTo(item, selfRect, this);
            return;
        }

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
            ItemTooltipUI.Instance?.ShowNextTo(item, selfRect, this);
    }

    /// <summary>
    /// 마우스가 아이콘을 벗어날 때 툴팁을 숨깁니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide(this);
    }
}
