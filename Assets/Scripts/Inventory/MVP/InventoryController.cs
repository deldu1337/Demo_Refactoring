using UnityEngine;

/// <summary>
/// 인벤토리와 장비 UI를 모두 초기화하고 상호작용을 연결하는 컨트롤러입니다.
/// </summary>
public class InventoryController : MonoBehaviour
{
    // 모델
    public InventoryModel inventory;    // 플레이어 인벤토리
    public EquipmentModel equipment;    // 장비 슬롯 정보

    // 뷰
    public InventoryView inventoryView; // 인벤토리 UI
    public EquipmentView equipmentView; // 장비 UI

    /// <summary>
    /// 시작 시 UI를 최신 데이터로 갱신합니다.
    /// </summary>
    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리와 장비 UI를 모두 갱신합니다.
    /// </summary>
    private void RefreshUI()
    {
        // 인벤토리 UI
        inventoryView.UpdateInventoryUI(
            inventory.Items,
            OnItemDropped,   // 위치 변경
            OnItemRemoved,   // 아이템 제거
            OnEquipRequest   // 장비 요청
        );

        // 장비 UI
        equipmentView.UpdateEquipmentUI(
            equipment.Slots,
            OnUnequipRequest // 해제 요청
        );
    }

    /// <summary>
    /// 인벤토리에서 아이템 순서를 변경합니다.
    /// </summary>
    private void OnItemDropped(string fromId, string toId)
    {
        inventory.ReorderByUniqueId(fromId, toId);
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리에서 아이템을 제거합니다.
    /// </summary>
    private void OnItemRemoved(string uniqueId)
    {
        inventory.RemoveById(uniqueId);
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리 아이템을 장비로 옮기는 요청을 처리합니다.
    /// </summary>
    private void OnEquipRequest(string uniqueId)
    {
        var item = inventory.GetItemById(uniqueId);
        if (item == null) return;

        var slotType = item.data.type;

        // 해당 슬롯에 장비 배치
        equipment.EquipItem(slotType, item);
        inventory.RemoveById(uniqueId);

        Debug.Log($"장비 장착: {item.data.name}");
        RefreshUI();
    }

    /// <summary>
    /// 장비를 해제하고 인벤토리로 되돌립니다.
    /// </summary>
    private void OnUnequipRequest(string slotType)
    {
        var slot = equipment.GetSlot(slotType);
        if (slot == null || slot.equipped == null) return;

        var item = slot.equipped;

        if (inventory.Add(item))
        {
            equipment.UnequipItem(slotType);
            Debug.Log($"장비 해제: {item.data.name}");
        }

        RefreshUI();
    }
}
