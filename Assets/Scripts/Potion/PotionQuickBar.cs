using System;
using UnityEngine;

public class PotionQuickBar : MonoBehaviour
{
    public static PotionQuickBar Instance { get; private set; }

    [Header("Slots (0~3)")]
    public PotionSlotUI[] slots = new PotionSlotUI[4];

    [Header("옵션")]
    public KeyCode key1 = KeyCode.Alpha1;
    public KeyCode key2 = KeyCode.Alpha2;
    public KeyCode key3 = KeyCode.Alpha3;
    public KeyCode key4 = KeyCode.Alpha4;

    [SerializeField] private InventoryPresenter inventoryPresenter; // 인스펙터로 연결 권장
    private PlayerStatsManager stats;

    // 슬롯별 캐시
    private string[] slotUID = new string[4];
    private int[] slotItemId = new int[4];
    private string[] slotIconPath = new string[4];
    private string[] slotPrefabPath = new string[4];

    private float[] cachedHP = new float[4];
    private float[] cachedMP = new float[4];

    // 슬롯별 수량 캐시
    private int[] slotQty = new int[4];

    // 이벤트(선택): 슬롯 구성 변경 때 바깥에서 후킹하고 싶으면
    public event System.Action OnChanged;

    void Awake() => Instance = this;

    void Start()
    {
        AutoWireByHierarchy();
        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        stats = PlayerStatsManager.Instance ?? FindAnyObjectByType<PlayerStatsManager>();

        for (int i = 0; i < slots.Length; i++)
            slots[i]?.Clear();

        // ★ 현재 종족으로 로드 (없으면 레거시에서 마이그레이션)
        var save = PotionQuickBarPersistence.LoadForRaceOrNew(CurrentRace());
        ApplySaveData(save);
    }

    /// <summary>
    /// 현재 세션의 레이스 키. PlayerStatsManager가 없으면 GameContext.SelectedRace로 보완.
    /// </summary>
    private string CurrentRace()
    {
        // 1) PlayerStatsManager의 저장 레이스
        var r = (stats != null && stats.Data != null) ? stats.Data.Race : null;
        if (!string.IsNullOrWhiteSpace(r)) return r.ToLower();

        // 2) 아직 초기화 전이면 선택 레이스 사용
        var sel = GameContext.SelectedRace;
        if (!string.IsNullOrWhiteSpace(sel)) return sel.ToLower();

        // 3) 최종 안전값
        return "humanmale";
    }

    void Update()
    {
        if (Input.GetKeyDown(key1)) Use(0);
        if (Input.GetKeyDown(key2)) Use(1);
        if (Input.GetKeyDown(key3)) Use(2);
        if (Input.GetKeyDown(key4)) Use(3);
    }

    public void Assign(int index, InventoryItem item, Sprite icon)
    {
        if (!ValidIndex(index) || item == null || item.data == null) return;
        if (!string.Equals(item.data.type, "potion", StringComparison.OrdinalIgnoreCase)) return;

        // ★ 같은 포션이면 "합치기"만 하고 끝
        if (!string.IsNullOrEmpty(slotUID[index]) && slotItemId[index] == item.id)
        {
            slotQty[index] = Mathf.Clamp(slotQty[index] + Mathf.Max(1, item.quantity), 1, 99);
            slots[index].SetQty(slotQty[index]);

            if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
            inventoryPresenter?.RemoveItemFromInventory(item.uniqueId); // 인벤 스택 제거

            PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
            OnChanged?.Invoke();
            return;
        }

        // 기존: 다른 포션이 들어있으면 인벤으로 반환 후 교체
        if (!string.IsNullOrEmpty(slotUID[index]))
            ReturnToInventory(index, refreshUI: false);

        // 신규 배치(+ 수량 세팅)
        slots[index].Set(item, icon, Mathf.Max(1, item.quantity));
        slotUID[index] = item.uniqueId;
        slotItemId[index] = item.id;
        slotIconPath[index] = item.iconPath;
        slotPrefabPath[index] = item.prefabPath;
        cachedHP[index] = item.data.hp;
        cachedMP[index] = item.data.mp;
        slotQty[index] = Mathf.Max(1, item.quantity);

        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        inventoryPresenter?.RemoveItemFromInventory(item.uniqueId);

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
        OnChanged?.Invoke();
    }

    public void Move(int from, int to)
    {
        if (!ValidIndex(from) || !ValidIndex(to) || from == to) return;
        var a = slots[from]; var b = slots[to]; if (!a || !b) return;

        (a.boundUniqueId, b.boundUniqueId) = (b.boundUniqueId, a.boundUniqueId);
        (slotUID[from], slotUID[to]) = (slotUID[to], slotUID[from]);
        (slotItemId[from], slotItemId[to]) = (slotItemId[to], slotItemId[from]);
        (slotIconPath[from], slotIconPath[to]) = (slotIconPath[to], slotIconPath[from]);
        (slotPrefabPath[from], slotPrefabPath[to]) = (slotPrefabPath[to], slotPrefabPath[from]);
        (cachedHP[from], cachedHP[to]) = (cachedHP[to], cachedHP[from]);
        (cachedMP[from], cachedMP[to]) = (cachedMP[to], cachedMP[from]);

        // ★ 수량도
        (slotQty[from], slotQty[to]) = (slotQty[to], slotQty[from]);

        var spA = a.icon ? a.icon.sprite : null;
        var spB = b.icon ? b.icon.sprite : null;
        if (a.icon) { a.icon.sprite = spB; a.icon.enabled = (spB != null) && !string.IsNullOrEmpty(a.boundUniqueId); }
        if (b.icon) { b.icon.sprite = spA; b.icon.enabled = (spA != null) && !string.IsNullOrEmpty(b.boundUniqueId); }

        a.RefreshEmptyOverlay(); b.RefreshEmptyOverlay();

        // ★ 수량 라벨 갱신
        a.SetQty(slotQty[from]);
        b.SetQty(slotQty[to]);

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    public void Clear(int index)
    {
        if (!ValidIndex(index) || slots[index] == null) return;

        slots[index].Clear();
        slotUID[index] = null;
        slotItemId[index] = 0;
        slotIconPath[index] = null;
        slotPrefabPath[index] = null;
        cachedHP[index] = 0f;
        cachedMP[index] = 0f;
        slotQty[index] = 0; // ★

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    public void Use(int index)
    {
        if (!ValidIndex(index) || slots[index] == null) return;
        if (string.IsNullOrEmpty(slotUID[index])) return;

        if (stats == null)
            stats = PlayerStatsManager.Instance ?? FindAnyObjectByType<PlayerStatsManager>();

        int hp = Mathf.RoundToInt(cachedHP[index]);
        int mp = Mathf.RoundToInt(cachedMP[index]);
        if (stats != null) { stats.Heal(hp); stats.RestoreMana(mp); }

        // ★ 수량 처리
        if (slotQty[index] > 1)
        {
            slotQty[index]--;
            slots[index].SetQty(slotQty[index]);
            PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
        }
        else
        {
            Clear(index);
        }
    }

    public void ReturnToInventory(int index, bool refreshUI = true)
    {
        if (!ValidIndex(index)) return;
        if (string.IsNullOrEmpty(slotUID[index])) return;

        var dataMgr = DataManager.Instance;
        if (!dataMgr || !dataMgr.dicItemDatas.ContainsKey(slotItemId[index]))
        {
            Debug.LogWarning("[PotionQuickBar] DataManager에 itemId가 없어 반환 실패");
            return;
        }

        var item = new InventoryItem
        {
            uniqueId = slotUID[index],
            id = slotItemId[index],
            data = dataMgr.dicItemDatas[slotItemId[index]],
            iconPath = slotIconPath[index],
            prefabPath = slotPrefabPath[index],

            // ★ 중요: 현재 퀵슬롯 수량으로 복원
            quantity = Mathf.Max(1, slotQty[index]),
            stackable = true,
            maxStack = 99
        };

        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        inventoryPresenter?.AddExistingItem(item);

        Clear(index);
    }

    public PotionQuickBarSave ToSaveData()
    {
        var save = new PotionQuickBarSave();
        for (int i = 0; i < slots.Length; i++)
        {
            if (string.IsNullOrEmpty(slotUID[i])) continue;
            save.slots.Add(new PotionSlotEntry
            {
                index = i,
                uniqueId = slotUID[i],
                itemId = slotItemId[i],
                iconPath = slotIconPath[i],
                prefabPath = slotPrefabPath[i],
                hp = cachedHP[i],
                mp = cachedMP[i],
                qty = Mathf.Max(1, slotQty[i]) // ★
            });
        }
        return save;
    }

    public void ApplySaveData(PotionQuickBarSave save)
    {
        for (int i = 0; i < slots.Length; i++) Clear(i);
        if (save == null) return;

        foreach (var e in save.slots)
        {
            if (!ValidIndex(e.index)) continue;

            slotUID[e.index] = e.uniqueId;
            slotItemId[e.index] = e.itemId;
            slotIconPath[e.index] = e.iconPath;
            slotPrefabPath[e.index] = e.prefabPath;
            cachedHP[e.index] = e.hp;
            cachedMP[e.index] = e.mp;
            slotQty[e.index] = Mathf.Max(1, e.qty); // ★

            Sprite sp = null;
            if (!string.IsNullOrEmpty(e.iconPath))
                sp = Resources.Load<Sprite>(e.iconPath);

            // ★ 로드시에도 수량을 함께 반영
            slots[e.index].SetBySave(e.uniqueId, sp, slotQty[e.index]);
        }

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    public bool TryAddToExistingSlot(int itemId, int amount = 1)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(slotUID[i]) && slotItemId[i] == itemId)
            {
                slotQty[i] = Mathf.Clamp(slotQty[i] + Mathf.Max(1, amount), 1, 99);
                slots[i].SetQty(slotQty[i]);
                PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
                return true;
            }
        }
        return false;
    }


    public bool TryGetSlotIndexAtScreenPosition(Vector2 screenPos, out int index)
    {
        index = -1;
        var cam = slots[0] ? slots[0].GetCanvasCamera() : null;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var rt = slots[i].GetRect();
            if (rt && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam))
            {
                index = i;
                return true;
            }
        }
        return false;
    }

    private bool ValidIndex(int i) => i >= 0 && i < (slots?.Length ?? 0);

    private void AutoWireByHierarchy()
    {
        var canvas = GameObject.Find("ItemCanvas");
        if (!canvas) return;
        var potionUI = canvas.transform.Find("PotionUI");
        if (!potionUI) return;

        slots = new PotionSlotUI[4];
        for (int i = 0; i < 4; i++)
        {
            var panel = potionUI.Find($"Potion{i + 1}");
            if (!panel) continue;

            var slot = panel.GetComponent<PotionSlotUI>();
            if (!slot) slot = panel.gameObject.AddComponent<PotionSlotUI>();
            slot.index = i;
            slot.AutoWireIconByChildName($"{i + 1}");

            // 아이콘에 드래그 핸들 자동 부착
            if (slot.icon && !slot.icon.gameObject.GetComponent<QuickSlotDraggable>())
            {
                var d = slot.icon.gameObject.AddComponent<QuickSlotDraggable>();
                d.slot = slot;
                d.canvas = canvas.GetComponent<Canvas>();
            }

            slots[i] = slot;
        }
    }
}
