using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 아이템이 어디에서 왔는지 나타내는 원본 위치입니다.
/// </summary>
public enum ItemOrigin
{
    Inventory,
    Equipment
}

/// <summary>
/// 인벤토리나 장비 슬롯에서 아이템을 드래그하여 이동하거나 사용하도록 돕는 뷰 클래스입니다.
/// </summary>
public class DraggableItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler
{
    /// <summary>
    /// 아이템을 장비하려고 할 때 호출되는 콜백입니다.
    /// </summary>
    public Action<string, ItemOrigin> onItemEquipped;

    /// <summary>
    /// 장비를 해제하려고 할 때 호출되는 콜백입니다.
    /// </summary>
    public Action<string, ItemOrigin> onItemUnequipped;

    /// <summary>
    /// 아이템을 다른 슬롯으로 이동할 때 호출되는 콜백입니다. (fromId, toId)
    /// </summary>
    public Action<string, string> onItemDropped;

    /// <summary>
    /// 아이템을 인벤토리에서 제거할 때 호출되는 콜백입니다.
    /// </summary>
    public Action<string> onItemRemoved;

    /// <summary>
    /// 현재 아이템 정보를 제공합니다.
    /// </summary>
    public InventoryItem Item { get; private set; }

    private string uniqueId;
    private ItemOrigin originType;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalIndex;
    private InventoryPresenter inventoryPresenter;

    private GameObject placeholder; // 원래 위치를 표시하기 위한 플레이스홀더 오브젝트

    /// <summary>
    /// 컴포넌트가 생성될 때 필요한 참조를 가져옵니다.
    /// </summary>
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
    }

    /// <summary>
    /// 아이템 정보를 설정하고 드래그 동작을 위한 콜백을 주입합니다.
    /// </summary>
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
            Debug.LogWarning("[DraggableItemView] 잘못된 아이템이 전달되어 비활성화합니다");
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

    /// <summary>
    /// 드래그 중 문제가 생겼을 때 원래 위치로 돌려놓습니다.
    /// </summary>
    public void SnapBackToOriginal()
    {
        if (originalParent)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalIndex);
        }
    }

    /// <summary>
    /// 클릭 입력을 받아 우클릭 시 장착 또는 해제 동작을 수행합니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[OnPointerClick] obj={gameObject.name}, origin={originType}, button={eventData.button}");

        // 왼쪽 클릭은 무시
        if (eventData.button == PointerEventData.InputButton.Left)
            return;

        if (eventData.button != PointerEventData.InputButton.Right) return;

        // 1. 우클릭 지점이 장비 슬롯인지 확인
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

        // 2. 인벤토리에서 우클릭하면 장비 시도
        if (originType == ItemOrigin.Inventory && !inEquipmentSlot)
        {
            onItemEquipped?.Invoke(uniqueId, originType);
        }
        // 3. 장비 슬롯에서 우클릭하면 해제 시도
        else if (inEquipmentSlot)
        {
            Debug.Log($"[Equipment] {uniqueId} 슬롯 {slotType} 해제 시도");
            onItemUnequipped?.Invoke(slotType, ItemOrigin.Equipment);
        }
    }

    /// <summary>
    /// 드래그를 시작할 때 플레이스홀더를 생성하고 위치를 기록합니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide(); // 드래그 중 툴팁 숨김

        if (originType == ItemOrigin.Equipment)
        {
            // 장비 슬롯에서는 드래그를 막습니다
            return;
        }

        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();

        // 현재 부모에서 활성화된 자식 수 계산
        int activeCount = 0;
        for (int i = 0; i < originalParent.childCount; i++)
        {
            if (originalParent.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }

        // 플레이스홀더 생성
        placeholder = new GameObject("Placeholder");
        var placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = rectTransform.sizeDelta;
        placeholder.transform.SetParent(originalParent);
        placeholder.transform.SetSiblingIndex(activeCount); // 보이는 순서 유지

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform, true); // 캔버스로 임시 이동
    }

    /// <summary>
    /// 드래그 중 마우스 위치에 맞춰 아이템을 이동합니다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (originType == ItemOrigin.Equipment)
        {
            // 장비 슬롯에서는 드래그를 막습니다
            return;
        }

        rectTransform.position = eventData.position;
    }

    /// <summary>
    /// 드래그를 끝냈을 때 위치를 판정하고 콜백을 호출합니다.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide();

        if (originType == ItemOrigin.Equipment)
            return;

        canvasGroup.blocksRaycasts = true;

        // 0) 포션 슬롯으로 이동했는지 먼저 확인합니다
        var potionSlotUnderPointer = eventData.pointerEnter
            ? eventData.pointerEnter.GetComponentInParent<PotionSlotUI>()
            : null;
        if (potionSlotUnderPointer != null)
        {
            // 포션 슬롯에서는 플레이스홀더를 사용하지 않으므로 정리합니다
            if (placeholder)
            {
                placeholder.transform.SetParent(null, false);
                Destroy(placeholder);
                placeholder = null;
            }
            return; // 포션 퀵 슬롯에서 처리가 끝났으므로 종료
        }

        // 1) 인벤토리 영역 안인지 확인
        RectTransform inventoryRect = originalParent as RectTransform;
        bool inInventory = RectTransformUtility.RectangleContainsScreenPoint(
            inventoryRect, eventData.position, canvas.worldCamera);

        // 2) 장비 슬롯 위인지 확인
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

        // 플레이스홀더 기준으로 삽입 위치를 계산
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
            // 인벤토리와 장비가 아닌 영역으로 드롭하면 아이템을 버립니다
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

        // 플레이스홀더 정리
        if (placeholder)
        {
            placeholder.transform.SetParent(null, false);
            Destroy(placeholder);
            placeholder = null;
        }
    }
}
