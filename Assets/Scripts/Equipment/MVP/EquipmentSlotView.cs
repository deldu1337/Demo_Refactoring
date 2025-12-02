using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class EquipmentSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public string slotType; // 슬롯 유형 이름
    public Action<string, InventoryItem> onItemDropped; // 장비 슬롯에 아이템이 떨어졌을 때 실행할 콜백

    /// <summary>
    /// 장비 슬롯 위에 드래그한 아이템을 놓았을 때 처리한다.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<DraggableItemView>();
        if (draggedItem == null || draggedItem.Item == null)
            return;

        // 슬롯 유형이 일치하면 장착 처리
        if (draggedItem.Item.data.type == slotType)
        {
            Debug.Log($"슬롯 {slotType} 에 {draggedItem.Item.data.name} 장착 요청");
            onItemDropped?.Invoke(slotType, draggedItem.Item);
        }
        else
        {
            Debug.LogWarning($"장착 실패: 슬롯 {slotType} 과 아이템 유형 {draggedItem.Item.data.type} 이 다름");
        }
    }

    /// <summary>
    /// 슬롯을 클릭했을 때 동작을 기록한다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"슬롯 {slotType} 클릭 감지: {eventData.button}");
    }
}
