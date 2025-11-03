using UnityEngine;

/// <summary>
/// 인벤토리와 장비창을 연결하고, 아이템 이동/장착/해제 등의 UI와 모델 업데이트를 관리하는 컨트롤러
/// </summary>
public class InventoryController : MonoBehaviour
{
    // 모델
    public InventoryModel inventory;    // 플레이어 인벤토리 데이터
    public EquipmentModel equipment;    // 장비 슬롯 데이터

    // 뷰
    public InventoryView inventoryView; // 인벤토리 UI
    public EquipmentView equipmentView; // 장비창 UI

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리와 장비창 UI 전체 갱신
    /// </summary>
    private void RefreshUI()
    {
        // 인벤토리 UI
        inventoryView.UpdateInventoryUI(
            inventory.Items,
            OnItemDropped,   // 드래그 후 위치 변경
            OnItemRemoved,   // 외부로 드래그 → 삭제
            OnEquipRequest   // 아이템 장착
        );

        // 장비창 UI
        equipmentView.UpdateEquipmentUI(
            equipment.Slots,
            OnUnequipRequest // 슬롯 클릭 → 아이템 해제
        );
    }

    /// <summary>
    /// 인벤토리 아이템 위치 변경 (드래그 앤 드롭)
    /// </summary>
    private void OnItemDropped(string fromId, string toId)
    {
        inventory.ReorderByUniqueId(fromId, toId);
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리 아이템 삭제
    /// </summary>
    private void OnItemRemoved(string uniqueId)
    {
        inventory.RemoveById(uniqueId);
        RefreshUI();
    }

    /// <summary>
    /// 인벤토리에서 장비창으로 아이템 장착
    /// </summary>
    private void OnEquipRequest(string uniqueId)
    {
        var item = inventory.GetItemById(uniqueId);
        if (item == null) return;

        var slotType = item.data.type;

        // 해당 슬롯에 아이템 장착
        equipment.EquipItem(slotType, item);
        inventory.RemoveById(uniqueId);
        
        Debug.Log($"장착: {item.data.name}");
        RefreshUI();
    }

    /// <summary>
    /// 장비창에서 아이템 해제 → 인벤토리로 이동
    /// </summary>
    private void OnUnequipRequest(string slotType)
    {
        var slot = equipment.GetSlot(slotType);
        if (slot == null || slot.equipped == null) return;

        var item = slot.equipped;

        if (inventory.Add(item))
        {
            equipment.UnequipItem(slotType);
            Debug.Log($"해제: {item.data.name}");
        }

        RefreshUI();
    }
}

