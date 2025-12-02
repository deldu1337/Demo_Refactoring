using System;
using UnityEngine;

/// <summary>
/// 포션 퀵바의 슬롯을 초기화하고 입력을 처리하는 관리 클래스입니다.
/// </summary>
public class PotionQuickBar : MonoBehaviour
{
    public static PotionQuickBar Instance { get; private set; }

    [Header("Slots (0~3)")]
    public PotionSlotUI[] slots = new PotionSlotUI[4];

    [Header("ɼ")]
    public KeyCode key1 = KeyCode.Alpha1;
    public KeyCode key2 = KeyCode.Alpha2;
    public KeyCode key3 = KeyCode.Alpha3;
    public KeyCode key4 = KeyCode.Alpha4;

    [SerializeField] private InventoryPresenter inventoryPresenter; // 인벤토리 프리젠터 인스턴스를 참조합니다.
    private PlayerStatsManager stats;

    // 슬롯마다 고유 식별자와 아이템 정보를 기억합니다.
    private string[] slotUID = new string[4];
    private int[] slotItemId = new int[4];
    private string[] slotIconPath = new string[4];
    private string[] slotPrefabPath = new string[4];

    private float[] cachedHP = new float[4];
    private float[] cachedMP = new float[4];

    // 슬롯에 쌓여 있는 수량을 기록합니다.
    private int[] slotQty = new int[4];

    // 슬롯 변경을 알리는 이벤트입니다.
    public event System.Action OnChanged;

    /// <summary>
    /// 인스턴스를 초기화합니다.
    /// </summary>
    void Awake() => Instance = this;

    /// <summary>
    /// 의존성을 찾고 저장된 데이터를 불러옵니다.
    /// </summary>
    void Start()
    {
        AutoWireByHierarchy();
        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        stats = PlayerStatsManager.Instance ?? FindAnyObjectByType<PlayerStatsManager>();

        for (int i = 0; i < slots.Length; i++)
            slots[i]?.Clear();

        // 저장된 퀵바 데이터가 있다면 불러오겠습니다.
        var save = PotionQuickBarPersistence.LoadForRaceOrNew(CurrentRace());
        ApplySaveData(save);
    }

    /// <summary>
    /// 현재 선택된 종족 이름을 가져옵니다. 값이 없으면 기본값을 제공합니다.
    /// </summary>
    private string CurrentRace()
    {
        // 1) PlayerStatsManager에서 우선 가져옵니다.
        var r = (stats != null && stats.Data != null) ? stats.Data.Race : null;
        if (!string.IsNullOrWhiteSpace(r)) return r.ToLower();

        // 2) 선택된 종족이 있다면 사용합니다.
        var sel = GameContext.SelectedRace;
        if (!string.IsNullOrWhiteSpace(sel)) return sel.ToLower();

        // 3) 기본값을 반환합니다.
        return "humanmale";
    }

    /// <summary>
    /// 슬롯 단축키 입력을 감지합니다.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(key1)) Use(0);
        if (Input.GetKeyDown(key2)) Use(1);
        if (Input.GetKeyDown(key3)) Use(2);
        if (Input.GetKeyDown(key4)) Use(3);
    }

    /// <summary>
    /// 지정한 슬롯에 포션 아이템을 배정합니다.
    /// </summary>
    public void Assign(int index, InventoryItem item, Sprite icon)
    {
        if (!ValidIndex(index) || item == null || item.data == null) return;
        if (!string.Equals(item.data.type, "potion", StringComparison.OrdinalIgnoreCase)) return;

        // 이미 같은 아이템이 있을 경우 수량을 추가하겠습니다.
        if (!string.IsNullOrEmpty(slotUID[index]) && slotItemId[index] == item.id)
        {
            slotQty[index] = Mathf.Clamp(slotQty[index] + Mathf.Max(1, item.quantity), 1, 99);
            slots[index].SetQty(slotQty[index]);

            if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
            inventoryPresenter?.RemoveItemFromInventory(item.uniqueId); // 인벤토리에서 사용된 아이템을 제거합니다.

            PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
            OnChanged?.Invoke();
            return;
        }

        // 다른 아이템이 있었다면 먼저 반환하겠습니다.
        if (!string.IsNullOrEmpty(slotUID[index]))
            ReturnToInventory(index, refreshUI: false);

        // 새 아이템을 슬롯에 설정합니다.
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

    /// <summary>
    /// 두 슬롯의 내용을 서로 교환합니다.
    /// </summary>
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

        // 수량도 함께 교환합니다.
        (slotQty[from], slotQty[to]) = (slotQty[to], slotQty[from]);

        var spA = a.icon ? a.icon.sprite : null;
        var spB = b.icon ? b.icon.sprite : null;
        if (a.icon) { a.icon.sprite = spB; a.icon.enabled = (spB != null) && !string.IsNullOrEmpty(a.boundUniqueId); }
        if (b.icon) { b.icon.sprite = spA; b.icon.enabled = (spA != null) && !string.IsNullOrEmpty(b.boundUniqueId); }

        a.RefreshEmptyOverlay(); b.RefreshEmptyOverlay();

        // 교환된 수량을 UI에 반영합니다.
        a.SetQty(slotQty[from]);
        b.SetQty(slotQty[to]);

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    /// <summary>
    /// 지정한 슬롯을 비웁니다.
    /// </summary>
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
        slotQty[index] = 0; // 수량을 초기화합니다.

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    /// <summary>
    /// 슬롯에 배정된 포션을 사용합니다.
    /// </summary>
    public void Use(int index)
    {
        if (!ValidIndex(index) || slots[index] == null) return;
        if (string.IsNullOrEmpty(slotUID[index])) return;

        if (stats == null)
            stats = PlayerStatsManager.Instance ?? FindAnyObjectByType<PlayerStatsManager>();

        int hp = Mathf.RoundToInt(cachedHP[index]);
        int mp = Mathf.RoundToInt(cachedMP[index]);
        if (stats != null) { stats.Heal(hp); stats.RestoreMana(mp); }

        // 남은 수량이 있다면 차감하고, 없으면 슬롯을 비웁니다.
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

    /// <summary>
    /// 슬롯의 아이템을 인벤토리로 되돌립니다.
    /// </summary>
    public void ReturnToInventory(int index, bool refreshUI = true)
    {
        if (!ValidIndex(index)) return;
        if (string.IsNullOrEmpty(slotUID[index])) return;

        var dataMgr = DataManager.Instance;
        if (!dataMgr || !dataMgr.dicItemDatas.ContainsKey(slotItemId[index]))
        {
            Debug.LogWarning("[PotionQuickBar] DataManager에서 해당 아이템 정보를 찾을 수 없습니다.");
            return;
        }

        var item = new InventoryItem
        {
            uniqueId = slotUID[index],
            id = slotItemId[index],
            data = dataMgr.dicItemDatas[slotItemId[index]],
            iconPath = slotIconPath[index],
            prefabPath = slotPrefabPath[index],

            // 저장된 수량을 사용합니다.
            quantity = Mathf.Max(1, slotQty[index]),
            stackable = true,
            maxStack = 99
        };

        if (!inventoryPresenter) inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        inventoryPresenter?.AddExistingItem(item);

        Clear(index);
    }

    /// <summary>
    /// 현재 슬롯 정보를 저장 데이터로 변환합니다.
    /// </summary>
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
                qty = Mathf.Max(1, slotQty[i]) // 저장 시 수량이 최소 1이 되도록 합니다.
            });
        }
        return save;
    }

    /// <summary>
    /// 저장된 슬롯 데이터를 UI에 반영합니다.
    /// </summary>
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
            slotQty[e.index] = Mathf.Max(1, e.qty); // 저장된 수량이 1 미만이면 1로 맞춥니다.

            Sprite sp = null;
            if (!string.IsNullOrEmpty(e.iconPath))
                sp = Resources.Load<Sprite>(e.iconPath);

            // 저장된 데이터를 바탕으로 슬롯을 채우겠습니다.
            slots[e.index].SetBySave(e.uniqueId, sp, slotQty[e.index]);
        }

        PotionQuickBarPersistence.SaveForRace(CurrentRace(), ToSaveData());
    }

    /// <summary>
    /// 동일한 아이템이 있는 슬롯에 수량을 추가하려 시도합니다.
    /// </summary>
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

    /// <summary>
    /// 주어진 화면 좌표에 해당하는 슬롯 인덱스를 찾습니다.
    /// </summary>
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

    /// <summary>
    /// 배열 범위 내에 있는 인덱스인지 확인합니다.
    /// </summary>
    private bool ValidIndex(int i) => i >= 0 && i < (slots?.Length ?? 0);

    /// <summary>
    /// 하이라키에서 필요한 UI 요소를 찾아 슬롯과 연결합니다.
    /// </summary>
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

            // 드래그가 가능하도록 드래그 핸들러를 추가합니다.
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
