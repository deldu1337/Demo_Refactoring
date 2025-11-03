using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ItemOrigin
{
    Inventory,
    Equipment
}

public class DraggableItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler
{
    public Action<string, ItemOrigin> onItemEquipped;    // 인벤토리 → 장비창

    public Action<string, ItemOrigin> onItemUnequipped;  // 장비창 → 인벤토리

    public Action<string, string> onItemDropped;         // 순서 변경 (fromId, toId)
    public Action<string> onItemRemoved;                // 삭제 이벤트

    public InventoryItem Item { get; private set; }

    private string uniqueId;
    private ItemOrigin originType;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalIndex;
    private InventoryPresenter inventoryPresenter;

    private GameObject placeholder; // 맨 마지막에 둘 placeholder

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
    }

    public void Initialize(
        InventoryItem item,
        ItemOrigin origin,
        Action<string, string> dropCallback = null,
        Action<string> removeCallback = null,
        Action<string, ItemOrigin> equipCallback = null,
        Action<string, ItemOrigin> unequipCallback = null
    )
    {
        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[DraggableItemView] 무효 아이템으로 초기화 시도 → 비활성화");
            gameObject.SetActive(false);
            return;
        }

        Item = item;
        uniqueId = item.uniqueId;
        originType = origin;

        onItemDropped = dropCallback;
        onItemRemoved = removeCallback;
        onItemEquipped = equipCallback;
        onItemUnequipped = unequipCallback;

    }

    public void SnapBackToOriginal()
    {
        if (originalParent)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[OnPointerClick] obj={gameObject.name}, origin={originType}, button={eventData.button}");
        
        // 좌클릭은 장비 해제 금지 → 그냥 리턴
        if (eventData.button == PointerEventData.InputButton.Left)
            return;

        if (eventData.button != PointerEventData.InputButton.Right) return;

        // 1. 장비창 슬롯 안에서 눌렀는지 직접 검사
        bool inEquipmentSlot = false;
        string slotType = null;

        var equipmentUI = GameObject.Find("EquipmentUI");
        if (equipmentUI != null)
        {
            var buttonPanel = equipmentUI.transform.Find("ButtonPanel");
            if (buttonPanel != null)
            {
                foreach (var button in buttonPanel.GetComponentsInChildren<Button>(true))
                {
                    RectTransform btnRect = button.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(btnRect, eventData.position, canvas.worldCamera))
                    {
                        inEquipmentSlot = true;
                        slotType = button.name.Replace("Button", "").ToLower();
                        break;
                    }
                }
            }
        }

        // 2. 인벤토리 아이템이면 → 장착 시도
        if (originType == ItemOrigin.Inventory && !inEquipmentSlot)
        {
            onItemEquipped?.Invoke(uniqueId, originType);
        }
        // 3. 장비창 슬롯 안에서 우클릭 → 해제 시도
        else if (inEquipmentSlot)
        {
            Debug.Log($"[Equipment] {uniqueId} → {slotType} 해제 시도");
            onItemUnequipped?.Invoke(slotType, ItemOrigin.Equipment);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide(); // ← 추가

        if (originType == ItemOrigin.Equipment)
        {
            // 장비창에서는 드래그 금지
            return;
        }

        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();

        // 활성화된 슬롯 개수 계산
        int activeCount = 0;
        for (int i = 0; i < originalParent.childCount; i++)
        {
            if (originalParent.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }

        // placeholder 생성
        placeholder = new GameObject("Placeholder");
        var placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = rectTransform.sizeDelta;
        placeholder.transform.SetParent(originalParent);
        placeholder.transform.SetSiblingIndex(activeCount); // 활성 슬롯 마지막 다음 인덱스

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform, true); // 최상위 캔버스로 이동
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (originType == ItemOrigin.Equipment)
        {
            // 장비창에서는 드래그 금지
            return;
        }

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide();

        if (originType == ItemOrigin.Equipment)
            return;

        canvasGroup.blocksRaycasts = true;

        // 0) ★ 포션 슬롯 위 체크: 드롭 처리는 OnDrop에서 하므로 여기선 '삭제'만 막고 종료
        var potionSlotUnderPointer = eventData.pointerEnter
            ? eventData.pointerEnter.GetComponentInParent<PotionSlotUI>()
            : null;
        if (potionSlotUnderPointer != null)
        {
            // placeholder만 정리하고 아무 것도 하지 않는다.
            if (placeholder)
            {
                placeholder.transform.SetParent(null, false);
                Destroy(placeholder);
                placeholder = null;
            }
            return; // ← 삭제 분기로 가지 않게 조기 종료
        }

        // 1) 인벤토리 영역 확인
        RectTransform inventoryRect = originalParent as RectTransform;
        bool inInventory = RectTransformUtility.RectangleContainsScreenPoint(
            inventoryRect, eventData.position, canvas.worldCamera);

        // 2) 장비창 영역 확인
        bool inEquipmentSlot = false;
        Button targetSlot = null;
        var equipmentUI = GameObject.Find("EquipmentUI");
        if (equipmentUI != null)
        {
            var buttonPanel = equipmentUI.transform.Find("ButtonPanel");
            if (buttonPanel != null)
            {
                foreach (var button in buttonPanel.GetComponentsInChildren<Button>(true))
                {
                    RectTransform btnRect = button.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(btnRect, eventData.position, canvas.worldCamera))
                    {
                        inEquipmentSlot = true;
                        targetSlot = button;
                        break;
                    }
                }
            }
        }

        // placeholder 위치로 아이템 이동
        int newIndex = placeholder ? placeholder.transform.GetSiblingIndex() : originalIndex;

        if (inInventory)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < originalParent.childCount; i++)
            {
                float dist = Vector2.SqrMagnitude(eventData.position - (Vector2)originalParent.GetChild(i).position);
                if (dist < closestDistance) { closestDistance = dist; closestIndex = i; }
            }

            string toId = null;
            if (closestIndex < originalParent.childCount)
            {
                var target = originalParent.GetChild(closestIndex).GetComponent<DraggableItemView>();
                if (target != null) toId = target.Item.uniqueId;
            }

            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalIndex);
            onItemDropped?.Invoke(uniqueId, toId);
        }
        else if (inEquipmentSlot)
        {
            if (originType == ItemOrigin.Inventory)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalIndex);
                onItemEquipped?.Invoke(uniqueId, originType);
            }
            else
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalIndex);
            }
        }
        else
        {
            // ★ 여기로 오면 'UI 밖'이므로 삭제. (포션 슬롯의 경우 위에서 이미 return 했음)
            if (placeholder)
            {
                placeholder.transform.SetParent(null, false);
                Destroy(placeholder);
                placeholder = null;
            }

            gameObject.SetActive(false);
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalIndex);
            onItemRemoved?.Invoke(uniqueId);
            return;
        }

        // 마지막 정리
        if (placeholder)
        {
            placeholder.transform.SetParent(null, false);
            Destroy(placeholder);
            placeholder = null;
        }
    }

}

