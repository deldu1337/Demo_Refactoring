using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 인벤토리에 저장되는 아이템 데이터를 나타냅니다.
/// </summary>
[Serializable]
public class InventoryItem
{
    public string uniqueId;
    public int id;
    public ItemData data;
    public string iconPath;
    public string prefabPath;

    // 드롭 시 부여된 추가 스탯 정보(없으면 null)
    public RolledItemStats rolled;

    // 중첩 관련 정보
    public bool stackable;       // 중첩 가능 여부
    public int quantity = 1;     // 현재 개수
    public int maxStack = 99;    // 최대 중첩 개수

    public GameObject prefab => Resources.Load<GameObject>(prefabPath);
}

[Serializable]
public class InventoryData
{
    public List<InventoryItem> items = new List<InventoryItem>();
}

/// <summary>
/// 인벤토리 데이터를 관리하고 저장을 담당하는 모델 클래스입니다.
/// </summary>
public class InventoryModel
{
    private readonly string race;
    private List<InventoryItem> items = new();  // 실제 아이템 목록
    private string filePath;

    public IReadOnlyList<InventoryItem> Items => items;

    /// <summary>
    /// 종족별 인벤토리를 초기화하고 저장 파일 경로를 설정합니다.
    /// </summary>
    public InventoryModel(string race = "humanmale")
    {
        this.race = string.IsNullOrEmpty(race) ? "humanmale" : race;
        filePath = Path.Combine(Application.persistentDataPath, $"playerInventory_{this.race}.json");

        Load();
        SaveIfCleaned(); // 로드 과정에서 정리된 내용이 있으면 즉시 저장
    }

    /// <summary>
    /// 고유 ID로 아이템을 조회합니다.
    /// </summary>
    public InventoryItem GetItemById(string uniqueId)
        => items.Find(i => i.uniqueId == uniqueId);

    /// <summary>
    /// 새로운 아이템을 추가하고 저장합니다. 중복이나 잘못된 데이터는 거부합니다.
    /// </summary>
    public void AddItem(InventoryItem item)
    {
        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[InventoryModel] 유효하지 않은 아이템 추가 시도");
            return;
        }
        if (items.Exists(i => i.uniqueId == item.uniqueId))
        {
            Debug.LogWarning($"[InventoryModel] 중복 uniqueId 추가 시도({item.uniqueId}) 거부");
            return;
        }

        EnsureRolledShapeIfPresent(item);

        items.Add(item);
        Save();
    }

    /// <summary>
    /// 고유 ID로 아이템을 제거하고 저장합니다.
    /// </summary>
    public void RemoveById(string uniqueId)
    {
        items.RemoveAll(i => i.uniqueId == uniqueId);
        Save();
    }

    /// <summary>
    /// fromId 위치의 아이템을 toId 앞에 재배치하고 결과를 저장합니다.
    /// </summary>
    public void ReorderByUniqueId(string fromId, string toId)
    {
        int fromIndex = items.FindIndex(i => i.uniqueId == fromId);
        if (fromIndex < 0) return;

        int toIndex = string.IsNullOrEmpty(toId) ? items.Count : items.FindIndex(i => i.uniqueId == toId);
        if (toIndex < 0) toIndex = items.Count - 1;

        var item = items[fromIndex];
        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[InventoryModel] 재정렬 대상 아이템이 무효라 삭제합니다");
            items.RemoveAt(fromIndex);
            Save();
            return;
        }

        items.RemoveAt(fromIndex);
        if (toIndex > fromIndex) toIndex--;
        items.Insert(toIndex, item);
        Save();
    }

    /// <summary>
    /// 중복되지 않은 아이템을 추가하고 성공 여부를 반환합니다.
    /// </summary>
    public bool Add(InventoryItem item)
    {
        if (InventoryGuards.IsInvalid(item)) return false;
        if (item == null || items.Exists(i => i.uniqueId == item.uniqueId)) return false;

        EnsureRolledShapeIfPresent(item);

        items.Add(item);
        Save();
        return true;
    }

    /// <summary>
    /// 동일한 포션 ID가 있다면 수량을 늘립니다. 성공 시 true를 반환합니다.
    /// </summary>
    public bool TryStackPotion(int itemId, int addAmount)
    {
        var found = items.Find(i => i.data != null
                                 && i.data.type == "potion"
                                 && i.id == itemId
                                 && i.stackable);
        if (found == null) return false;

        int before = found.quantity;
        found.quantity = Mathf.Clamp(found.quantity + addAmount, 0, found.maxStack);
        Debug.Log($"[InventoryModel] 포션 스택 변경: id={itemId}, {before} -> {found.quantity}");
        Save();
        return true;
    }

    /// <summary>
    /// 지정한 포션을 사용하여 수량을 줄이고 0 이하이면 제거합니다.
    /// </summary>
    public void ConsumePotionByUniqueId(string uniqueId, int count = 1)
    {
        var it = items.Find(i => i.uniqueId == uniqueId);
        if (it == null) return;

        if (it.data != null && it.data.type == "potion" && it.stackable)
        {
            it.quantity -= count;
            if (it.quantity <= 0)
            {
                items.Remove(it);
            }
            Save();
        }
        else
        {
            // 포션이 아니면 일반 제거로 처리
            RemoveById(uniqueId);
        }
    }

    /// <summary>
    /// 디스크에서 데이터를 불러오고 잘못된 항목을 정리합니다.
    /// </summary>
    public void Load()
    {
        // 저장된 파일을 불러오고 없으면 기본 값을 반환
        var data = SaveLoadService.LoadInventoryForRaceOrNew(race);
        items = data.items ?? new List<InventoryItem>();

        // 유효하지 않은 데이터 정리
        int before = items.Count;
        items.RemoveAll(InventoryGuards.IsInvalid);
        int after = items.Count;
        if (before != after)
            Debug.LogWarning($"[InventoryModel] 로드 시 정리된 항목 수: {before - after}");

        // 롤링된 스탯에 잘못된 값이 있으면 기본값으로 고정
        for (int i = 0; i < items.Count; i++)
            EnsureRolledShapeIfPresent(items[i]);
    }

    /// <summary>
    /// 현재 메모리의 인벤토리 데이터를 파일로 저장합니다.
    /// </summary>
    public void Save()
    {
        // 저장 전에 잘못된 데이터 제거
        items.RemoveAll(InventoryGuards.IsInvalid);

        var data = new InventoryData { items = items };
        SaveLoadService.SaveInventoryForRace(race, data);
    }

    /// <summary>
    /// 로드 후 메모리 상태와 디스크 상태가 다르면 정리된 내용을 저장합니다.
    /// </summary>
    private void SaveIfCleaned()
    {
        if (!File.Exists(filePath)) { Save(); return; }

        try
        {
            string json = File.ReadAllText(filePath);
            var onDisk = JsonUtility.FromJson<InventoryData>(json)?.items ?? new List<InventoryItem>();
            int diskCount = onDisk.Count;
            int memCount = items.Count;
            if (diskCount != memCount)
            {
                Debug.LogWarning($"[InventoryModel] 로드 후 항목 수가 달라 저장을 갱신합니다: {diskCount} -> {memCount}");
                Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InventoryModel] SaveIfCleaned 중 예외 발생, 데이터를 다시 저장합니다: {e}");
            Save();
        }
    }

    /// <summary>
    /// 롤링된 스탯이 NaN이나 무한대인 경우 0으로 교정합니다.
    /// </summary>
    private static void EnsureRolledShapeIfPresent(InventoryItem item)
    {
        if (item == null) return;
        if (item.rolled == null) return;

        void Fix(ref float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
        }

        Fix(ref item.rolled.hp);
        Fix(ref item.rolled.mp);
        Fix(ref item.rolled.atk);
        Fix(ref item.rolled.def);
        Fix(ref item.rolled.dex);
        Fix(ref item.rolled.As);
        Fix(ref item.rolled.cc);
        Fix(ref item.rolled.cd);
    }
}
