using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public string uniqueId;
    public int id;
    public ItemData data;
    public string iconPath;
    public string prefabPath;

    // 획득 시 확정된(롤링) 능력치 (구버전 null 허용)
    public RolledItemStats rolled;

    // ★ 추가: 스택
    public bool stackable;       // 포션은 true
    public int quantity = 1;     // 현재 수량
    public int maxStack = 99;    // 최대 스택(원하면 조절)

    public GameObject prefab => Resources.Load<GameObject>(prefabPath);
}

[Serializable]
public class InventoryData
{
    public List<InventoryItem> items = new List<InventoryItem>();
}

public class InventoryModel
{
    private readonly string race;
    private List<InventoryItem> items = new();  // 순서 보장
    private string filePath;

    public IReadOnlyList<InventoryItem> Items => items;

    // ★ 종족별 생성자
    public InventoryModel(string race = "humanmale")
    {
        this.race = string.IsNullOrEmpty(race) ? "humanmale" : race;
        filePath = Path.Combine(Application.persistentDataPath, $"playerInventory_{this.race}.json");

        Load();
        SaveIfCleaned(); // 로드시 정리된 내용이 있으면 반영
    }

    public InventoryItem GetItemById(string uniqueId)
        => items.Find(i => i.uniqueId == uniqueId);

    public void AddItem(InventoryItem item)
    {
        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[InventoryModel] 무효 아이템 추가 시도 → 무시");
            return;
        }
        if (items.Exists(i => i.uniqueId == item.uniqueId))
        {
            Debug.LogWarning($"[InventoryModel] 중복 uniqueId 추가 시도({item.uniqueId}) → 무시");
            return;
        }

        EnsureRolledShapeIfPresent(item);

        items.Add(item);
        Save();
    }

    public void RemoveById(string uniqueId)
    {
        items.RemoveAll(i => i.uniqueId == uniqueId);
        Save();
    }

    /// <summary>uniqueId 기반으로 아이템 순서를 재배치하고 JSON에 저장</summary>
    public void ReorderByUniqueId(string fromId, string toId)
    {
        int fromIndex = items.FindIndex(i => i.uniqueId == fromId);
        if (fromIndex < 0) return;

        int toIndex = string.IsNullOrEmpty(toId) ? items.Count : items.FindIndex(i => i.uniqueId == toId);
        if (toIndex < 0) toIndex = items.Count - 1;

        var item = items[fromIndex];
        if (InventoryGuards.IsInvalid(item))
        {
            Debug.LogWarning("[InventoryModel] 재배치 중 무효 아이템 발견 → 제거");
            items.RemoveAt(fromIndex);
            Save();
            return;
        }

        items.RemoveAt(fromIndex);
        if (toIndex > fromIndex) toIndex--;
        items.Insert(toIndex, item);
        Save();
    }

    public bool Add(InventoryItem item)
    {
        if (InventoryGuards.IsInvalid(item)) return false;
        if (item == null || items.Exists(i => i.uniqueId == item.uniqueId)) return false;

        EnsureRolledShapeIfPresent(item);

        items.Add(item);
        Save();
        return true;
    }

    /// <summary>동일 포션(id) 발견 시 수량 합치기. 발견하면 true.</summary>
    public bool TryStackPotion(int itemId, int addAmount)
    {
        var found = items.Find(i => i.data != null
                                 && i.data.type == "potion"
                                 && i.id == itemId
                                 && i.stackable);
        if (found == null) return false;

        int before = found.quantity;
        found.quantity = Mathf.Clamp(found.quantity + addAmount, 0, found.maxStack);
        Debug.Log($"[InventoryModel] 포션 스택: id={itemId}, {before} → {found.quantity}");
        Save();
        return true;
    }

    /// <summary>uniqueId로 포션 1개(or n개) 사용. 0 되면 슬롯 제거.</summary>
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
            // 스택형이 아니면 기존처럼 제거
            RemoveById(uniqueId);
        }
    }

    public void Load()
    {
        // 종족별 파일 우선 + 레거시 마이그레이션
        var data = SaveLoadService.LoadInventoryForRaceOrNew(race);
        items = data.items ?? new List<InventoryItem>();

        // 로드시 무효 아이템 정리
        int before = items.Count;
        items.RemoveAll(InventoryGuards.IsInvalid);
        int after = items.Count;
        if (before != after)
            Debug.LogWarning($"[InventoryModel] 로드시 무효 아이템 {before - after}개 정리함");

        // rolled 구조체 정합성 보정(구버전 null 허용)
        for (int i = 0; i < items.Count; i++)
            EnsureRolledShapeIfPresent(items[i]);
    }

    public void Save()
    {
        // 저장 전에도 무효 아이템 정리(이중 안전망)
        items.RemoveAll(InventoryGuards.IsInvalid);

        var data = new InventoryData { items = items };
        SaveLoadService.SaveInventoryForRace(race, data);
    }

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
                Debug.LogWarning($"[InventoryModel] 초기 로드 시 정리 반영: {diskCount} → {memCount}");
                Save();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InventoryModel] SaveIfCleaned 중 파일 비교 실패 → Save 강행: {e}");
            Save();
        }
    }

    /// <summary>rolled가 존재하는 경우 직렬화 호환(구버전 null 허용) 및 기본값 보정.</summary>
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
