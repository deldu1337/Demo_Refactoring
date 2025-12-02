using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI와 모델을 연결하고 입력을 처리하는 프레젠터입니다.
/// </summary>
public class InventoryPresenter : MonoBehaviour
{
    private InventoryModel model;
    private InventoryView view;
    private bool isOpen;

    private Button InvenButton;

    /// <summary>
    /// 인벤토리 초기화를 수행하고 입력 리스너를 설정합니다.
    /// </summary>
    private void Start()
    {
        UIEscapeStack.GetOrCreate(); // ESC 스택 보장
        view = FindAnyObjectByType<InventoryView>();
        if (view == null) return;

        // 플레이어 종족에 맞는 인벤토리 파일을 사용
        var ps = PlayerStatsManager.Instance;
        string race = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
                        ? ps.Data.Race
                        : "humanmale";

        model = new InventoryModel(race);
        view.Initialize(CloseInventory);
        isOpen = false;

        InvenButton = GameObject.Find("QuickUI").transform.GetChild(3).GetComponent<Button>();
        InvenButton.onClick.AddListener(ToggleInventory);
    }

    /// <summary>
    /// 단축키 입력을 확인하여 인벤토리를 토글합니다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
        // ESC는 중앙 스택에서 처리
    }

    /// <summary>
    /// 인벤토리 창을 열거나 닫고 UI를 갱신합니다.
    /// </summary>
    private void ToggleInventory()
    {
        if (view == null) return;
        isOpen = !isOpen;
        view.Show(isOpen);

        if (isOpen)
        {
            view.UpdateInventoryUI(model.Items, OnItemDropped, OnItemRemoved, OnItemEquipped);
            UIEscapeStack.Instance.Push(
                key: "inventory",
                close: CloseInventory,
                isOpen: () => isOpen
            );
        }
        else
        {
            UIEscapeStack.Instance.Remove("inventory");
        }
    }

    /// <summary>
    /// 인벤토리 창을 닫고 ESC 스택에서 제거합니다.
    /// </summary>
    private void CloseInventory()
    {
        if (!isOpen) return;
        isOpen = false;
        view.Show(false);
        UIEscapeStack.Instance.Remove("inventory");
    }

    /// <summary>
    /// 새로운 아이템을 생성하거나 스택을 늘린 뒤 UI를 갱신합니다.
    /// </summary>
    public void AddItem(int id, Sprite icon, string prefabPath)
    {
        var dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.dicItemDatas.ContainsKey(id))
        {
            Debug.LogWarning($"아이템 ID {id}가 DataManager에 없음");
            return;
        }

        var baseData = dataManager.dicItemDatas[id];

        // 우선 포션 퀵슬롯 스택 증가 시도
        if (baseData.type == "potion" && PotionQuickBar.Instance != null)
        {
            if (PotionQuickBar.Instance.TryAddToExistingSlot(id, +1))
            {
                // 퀵슬롯 라벨은 내부에서 갱신. 인벤토리가 열려있다면 UI만 새로고침.
                Refresh();
                return;
            }
        }

        // 다음으로 인벤토리 내 스택 증가 시도
        if (baseData.type == "potion")
        {
            if (model.TryStackPotion(id, +1))
            {
                Refresh();
                return;
            }
        }

        // 스택이 없다면 새로운 아이템 생성
        var item = new InventoryItem
        {
            uniqueId = Guid.NewGuid().ToString(),
            id = id,
            data = baseData,
            iconPath = icon ? "Icons/" + icon.name : null,
            prefabPath = prefabPath,
            rolled = ItemRoller.CreateRolledStats(id),

            stackable = baseData.type == "potion",
            quantity = 1,
            maxStack = 99
        };

        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[InventoryPresenter] 생성된 아이템이 무효라 추가를 취소합니다");
            return;
        }

        model.AddItem(item);
        Refresh();
    }


    /// <summary>
    /// 아이템을 제거하고 실제 삭제 여부를 반환합니다.
    /// </summary>
    public bool RemoveItemFromInventory(string uniqueId)
    {
        if (model == null)
        {
            var ps = PlayerStatsManager.Instance;
            string race = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
                            ? ps.Data.Race
                            : "humanmale";
            model = new InventoryModel(race); // 안전장치
        }

        var before = model.GetItemById(uniqueId) != null;
        model.RemoveById(uniqueId);
        var after = model.GetItemById(uniqueId) != null;

        // 인벤 UI는 닫혀 있어도 강제로 갱신
        ForceRefresh();

        return before && !after;
    }

    /// <summary>
    /// 인벤토리 UI를 강제로 갱신합니다.
    /// </summary>
    public void ForceRefresh()
        => view?.UpdateInventoryUI(model.Items, OnItemDropped, OnItemRemoved, OnItemEquipped);

    /// <summary>
    /// 장비 착용 요청을 처리합니다.
    /// </summary>
    private void OnItemEquipped(string uniqueId)
    {
        var item = model.GetItemById(uniqueId);
        if (item == null) return;

        // 포션은 즉시 사용 후 수량 감소
        if (item.data != null && string.Equals(item.data.type, "potion", StringComparison.OrdinalIgnoreCase))
        {
            var stats = PlayerStatsManager.Instance;
            if (stats != null)
            {
                float hp = item.rolled != null && item.rolled.hasHp ? item.rolled.hp : item.data.hp;
                float mp = item.rolled != null && item.rolled.hasMp ? item.rolled.mp : item.data.mp;
                if (hp > 0) stats.Heal(hp);
                if (mp > 0) stats.RestoreMana(mp);
            }

            model.ConsumePotionByUniqueId(uniqueId, 1);
            Refresh();
            return;
        }

        // 장비 장착은 장비 프레젠터에 위임
        var equipPresenter = FindAnyObjectByType<EquipmentPresenter>();
        equipPresenter?.HandleEquipItem(item);

        Refresh();
    }

    /// <summary>
    /// 아이템 삭제 콜백을 처리합니다.
    /// </summary>
    private void OnItemRemoved(string uniqueId)
    {
        model.RemoveById(uniqueId);
        Refresh();
    }

    /// <summary>
    /// 아이템 위치 이동 콜백을 처리합니다.
    /// </summary>
    private void OnItemDropped(string fromId, string toId)
    {
        model.ReorderByUniqueId(fromId, toId);
        Refresh();
    }

    /// <summary>
    /// 기존 아이템을 추가하고 UI를 갱신합니다.
    /// </summary>
    public void AddExistingItem(InventoryItem item)
    {
        model.Add(item);
        Refresh();
    }

    /// <summary>
    /// 고유 ID로 아이템을 반환합니다.
    /// </summary>
    public InventoryItem GetItemByUniqueId(string uniqueId)
        => model.GetItemById(uniqueId);

    /// <summary>
    /// 인벤토리가 열려 있을 때만 UI를 새로고칩니다.
    /// </summary>
    public void Refresh()
    {
        if (isOpen)
            view.UpdateInventoryUI(model.Items, OnItemDropped, OnItemRemoved, OnItemEquipped);
    }
}
