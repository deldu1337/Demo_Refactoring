using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class EquipmentSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public string slotType; // 슬롯 타입
    public Action<string, InventoryItem> onItemDropped; // 드롭 이벤트 콜백

    /// <summary>아이템 드래그 앤 드롭 처리</summary>
    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<DraggableItemView>();
        if (draggedItem == null || draggedItem.Item == null)
            return;

        // 슬롯 타입과 아이템 타입 일치 확인
        if (draggedItem.Item.data.type == slotType)
        {
            Debug.Log($"드롭 성공 → {slotType} 슬롯에 {draggedItem.Item.data.name}");
            onItemDropped?.Invoke(slotType, draggedItem.Item);
        }
        else
        {
            Debug.LogWarning($"드롭 실패: {slotType} 슬롯은 {draggedItem.Item.data.type} 아이템 장착 불가");
        }
    }

    /// <summary>슬롯 클릭 감지 (우클릭 등 추후 확장 가능)</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Click detected on {slotType}: {eventData.button}");
    }
}
