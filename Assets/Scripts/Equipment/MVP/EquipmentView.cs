using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentView : MonoBehaviour
{
    [SerializeField] private GameObject equipmentUI; // 장비 UI 루트 오브젝트
    [SerializeField] private Button exitButton;      // 닫기 버튼

    [Header("슬롯 버튼")]
    [SerializeField] private Button headSlot;
    [SerializeField] private Button rShoulderSlot;
    [SerializeField] private Button lShoulderSlot;
    [SerializeField] private Button gemSlot;
    [SerializeField] private Button weaponSlot;
    [SerializeField] private Button shieldSlot;

    public RectTransform RootRect
    {
        get
        {
            if (!equipmentUI) equipmentUI = GameObject.Find("EquipmentUI");
            return equipmentUI ? equipmentUI.GetComponent<RectTransform>() : null;
        }
    }

    /// <summary>
    /// 초기화 시 UI 연결과 슬롯 버튼을 설정한다.
    /// </summary>
    public void Initialize(Action onExit, Action<string, InventoryItem> onEquipDropped)
    {
        if (equipmentUI == null)
            equipmentUI = GameObject.Find("EquipmentUI");

        if (equipmentUI != null)
        {
            exitButton = equipmentUI.GetComponentInChildren<Button>();
            exitButton.onClick.AddListener(() => onExit?.Invoke());
        }

        // 버튼 패널에서 슬롯 버튼 찾기
        headSlot = GameObject.Find("ButtonPanel").transform.GetChild(0).GetComponentInChildren<Button>();
        rShoulderSlot = GameObject.Find("ButtonPanel").transform.GetChild(1).GetComponentInChildren<Button>();
        lShoulderSlot = GameObject.Find("ButtonPanel").transform.GetChild(2).GetComponentInChildren<Button>();
        gemSlot = GameObject.Find("ButtonPanel").transform.GetChild(3).GetComponentInChildren<Button>();
        weaponSlot = GameObject.Find("ButtonPanel").transform.GetChild(4).GetComponentInChildren<Button>();
        shieldSlot = GameObject.Find("ButtonPanel").transform.GetChild(5).GetComponentInChildren<Button>();

        Show(false);
    }

    /// <summary>
    /// 장비 UI를 표시하거나 숨긴다.
    /// </summary>
    public void Show(bool show)
    {
        equipmentUI?.SetActive(show);
        if (exitButton != null)
            exitButton.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 슬롯 컴포넌트를 설정하고 드롭 이벤트를 연결한다.
    /// </summary>
    private void SetupSlot(Button button, string slotType, Action<string, InventoryItem> onEquipDropped)
    {
        if (button == null) return;
        var slotView = button.GetComponent<EquipmentSlotView>();
        if (slotView == null)
            slotView = button.gameObject.AddComponent<EquipmentSlotView>();

        slotView.slotType = slotType;
        slotView.onItemDropped = onEquipDropped;
    }

    /// <summary>
    /// UI를 갱신하여 장착 상태와 상호작용을 설정한다.
    /// </summary>
    public void UpdateEquipmentUI(IReadOnlyList<EquipmentSlot> slots, Action<string> onSlotClicked)
    {
        foreach (var slot in slots)
        {
            Button btn = GetSlotButton(slot.slotType);
            if (btn == null) continue;

            if (slot.equipped == null || string.IsNullOrEmpty(slot.equipped.iconPath))
            {
                btn.gameObject.SetActive(false); // 비어 있으면 비활성화
            }
            else
            {
                btn.gameObject.SetActive(true);

                // 아이콘 설정
                var image = btn.GetComponent<Image>();
                var icon = Resources.Load<Sprite>(slot.equipped.iconPath);
                if (image != null) image.sprite = icon;

                // 클릭 이벤트 재설정
                btn.onClick.RemoveAllListeners();

                // 슬롯 정보 설정
                var slotView = btn.GetComponent<EquipmentSlotView>();
                if (slotView == null) slotView = btn.gameObject.AddComponent<EquipmentSlotView>();
                slotView.slotType = slot.slotType;
                slotView.onItemDropped = null;

                // 드래그 가능한 아이템 설정
                var draggable = btn.GetComponent<DraggableItemView>();
                if (draggable == null)
                    draggable = btn.gameObject.AddComponent<DraggableItemView>();

                draggable.Initialize(
                    slot.equipped,
                    ItemOrigin.Equipment,
                    dropCallback: null,
                    removeCallback: null,
                    equipCallback: null,
                    unequipCallback: (slotType, origin) =>
                    {
                        Debug.Log($"장비 슬롯 클릭: {slotType}, {origin}");
                        onSlotClicked?.Invoke(slotType);
                    }
                );

                // 툴팁 설정
                var hover = btn.GetComponent<ItemHoverTooltip>();
                if (hover == null) hover = btn.gameObject.AddComponent<ItemHoverTooltip>();
                hover.SetItem(slot.equipped);
                hover.SetContext(ItemOrigin.Equipment);

                // 우클릭 이벤트 등록
                var trigger = btn.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                trigger.triggers.Clear();

                var entry = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
                };
                entry.callback.AddListener((data) =>
                {
                    var ev = (UnityEngine.EventSystems.PointerEventData)data;
                    if (ev.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                    {
                        Debug.Log($"우클릭: {slot.slotType}");
                        onSlotClicked?.Invoke(slot.slotType);
                    }
                });
                trigger.triggers.Add(entry);
            }
        }
    }

    private Button GetSlotButton(string slotType)
    {
        return slotType switch
        {
            "head" => headSlot,
            "rshoulder" => rShoulderSlot,
            "lshoulder" => lShoulderSlot,
            "gem" => gemSlot,
            "weapon" => weaponSlot,
            "shield" => shieldSlot,
            _ => null
        };
    }

    /// <summary>
    /// 슬롯에 지정된 아이콘을 설정한다.
    /// </summary>
    public void SetEquipmentIcon(Sprite icon, string slotType)
    {
        var button = GetSlotButton(slotType);
        if (button != null)
        {
            var image = button.GetComponent<Image>();
            image.sprite = icon;
        }
    }
}
