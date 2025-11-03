using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPresenter : MonoBehaviour
{
    private InventoryModel model;
    private InventoryView view;
    private bool isOpen;

    private Button InvenButton;

    void Start()
    {
        UIEscapeStack.GetOrCreate(); // 스택 보장
        view = FindAnyObjectByType<InventoryView>();
        if (view == null) return;

        // ★ 플레이어 종족 가져와서 종족별 인벤토리 사용
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
        // ESC는 중앙 스택에서 처리
    }

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

    private void CloseInventory()
    {
        if (!isOpen) return;
        isOpen = false;
        view.Show(false);
        UIEscapeStack.Instance.Remove("inventory");
    }

    public void AddItem(int id, Sprite icon, string prefabPath)
    {
        var dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.dicItemDatas.ContainsKey(id))
        {
            Debug.LogWarning($"아이템 ID {id}가 DataManager에 없음!");
            return;
        }

        var baseData = dataManager.dicItemDatas[id];

        // ★ 1순위: 퀵슬롯 스택 증가 시도 (포션만)
        if (baseData.type == "potion" && PotionQuickBar.Instance != null)
        {
            if (PotionQuickBar.Instance.TryAddToExistingSlot(id, +1))
            {
                // 퀵슬롯 라벨은 TryAddToExistingSlot에서 갱신됨. 인벤 토글이 열려있다면 UI만 새로고침.
                Refresh();
                return;
            }
        }

        // ★ 2순위: 인벤토리 스택 증가 시도
        if (baseData.type == "potion")
        {
            if (model.TryStackPotion(id, +1))
            {
                Refresh();
                return;
            }
        }

        // 3순위: 새 아이템 생성
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
            Debug.LogWarning("[InventoryPresenter] 생성된 아이템이 무효 → 추가 취소");
            return;
        }

        model.AddItem(item);
        Refresh();
    }


    // 반환값으로 '실제 제거 성공' 여부
    public bool RemoveItemFromInventory(string uniqueId)
    {
        if (model == null)
        {
            var ps = PlayerStatsManager.Instance;
            string race = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
                            ? ps.Data.Race
                            : "humanmale";
            model = new InventoryModel(race); // 안전망
        }

        var before = model.GetItemById(uniqueId) != null;
        model.RemoveById(uniqueId);
        var after = model.GetItemById(uniqueId) != null;

        // 인벤 UI는 닫혀 있어도 강제로 갱신
        ForceRefresh();

        return before && !after;
    }

    public void ForceRefresh()
        => view?.UpdateInventoryUI(model.Items, OnItemDropped, OnItemRemoved, OnItemEquipped);

    private void OnItemEquipped(string uniqueId)
    {
        var item = model.GetItemById(uniqueId);
        if (item == null) return;

        // ★ 포션 즉시사용(수량 1 감소)
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

            // 기존엔 RemoveById였는데 → ★ 수량 감소 API로 변경
            model.ConsumePotionByUniqueId(uniqueId, 1);
            Refresh();
            return;
        }

        // 장비 장착은 장비 프레젠터에 위임
        var equipPresenter = FindAnyObjectByType<EquipmentPresenter>();
        equipPresenter?.HandleEquipItem(item);

        Refresh();
    }

    private void OnItemRemoved(string uniqueId)
    {
        model.RemoveById(uniqueId);
        Refresh();
    }

    private void OnItemDropped(string fromId, string toId)
    {
        model.ReorderByUniqueId(fromId, toId);
        Refresh();
    }

    public void AddExistingItem(InventoryItem item)
    {
        model.Add(item);
        Refresh();
    }

    public InventoryItem GetItemByUniqueId(string uniqueId)
        => model.GetItemById(uniqueId);

    public void Refresh()
    {
        if (isOpen)
            view.UpdateInventoryUI(model.Items, OnItemDropped, OnItemRemoved, OnItemEquipped);
    }
}

