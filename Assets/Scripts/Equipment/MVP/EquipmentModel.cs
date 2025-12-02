using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentSlot
{
    public string slotType;
    public InventoryItem equipped;
}

[Serializable]
public class EquipmentData
{
    public List<EquipmentSlot> slots = new List<EquipmentSlot>();
}

public class EquipmentModel
{
    private readonly string race;   // 플레이어 종족 이름입니다.
    private EquipmentData data;

    public IReadOnlyList<EquipmentSlot> Slots => data.slots;

    /// <summary>
    /// 종족 정보를 받아 기본 장비 슬롯을 초기화합니다.
    /// </summary>
    public EquipmentModel(string race)
    {
        this.race = string.IsNullOrEmpty(race) ? "humanmale" : race;

        Load();

        if (data.slots.Count == 0)
        {
            data.slots.Add(new EquipmentSlot { slotType = "head", equipped = null });
            data.slots.Add(new EquipmentSlot { slotType = "rshoulder", equipped = null });
            data.slots.Add(new EquipmentSlot { slotType = "lshoulder", equipped = null });
            data.slots.Add(new EquipmentSlot { slotType = "gem", equipped = null });
            data.slots.Add(new EquipmentSlot { slotType = "weapon", equipped = null });
            data.slots.Add(new EquipmentSlot { slotType = "shield", equipped = null });
            Save();
        }
    }

    /// <summary>
    /// 슬롯 유형에 맞춰 아이템을 장착하고 저장합니다.
    /// </summary>
    public void EquipItem(string slotType, InventoryItem item)
    {
        var slot = data.slots.Find(s => s.slotType == slotType);
        if (slot != null)
        {
            slot.equipped = item;
            Save();
            Debug.Log($"{slotType} 슬롯에 {item.data.name} 장착");
        }
        else
        {
            Debug.LogWarning($"EquipItem: {slotType} 슬롯을 찾을 수 없음");
        }
    }

    /// <summary>
    /// 슬롯 유형에 맞춰 장착된 아이템을 해제합니다.
    /// </summary>
    public void UnequipItem(string slotType)
    {
        var slot = data.slots.Find(s => s.slotType == slotType);
        if (slot != null)
        {
            Debug.Log($"{slotType} 슬롯에서 {slot.equipped?.data?.name ?? ""} 해제");
            slot.equipped = null;
            Save();
        }
        else
        {
            Debug.LogWarning($"UnequipItem: {slotType} 슬롯을 찾을 수 없음");
        }
    }

    /// <summary>
    /// 인덱스로 장비 슬롯을 찾아 아이템을 해제합니다.
    /// </summary>
    public void Unequip(int index)
    {
        if (index < 0 || index >= data.slots.Count) return;
        var slot = data.slots[index];
        Debug.Log($"{slot.slotType} 슬롯에서 {slot.equipped?.data?.name ?? ""} 해제");
        slot.equipped = null;
        Save();
    }

    /// <summary>
    /// 슬롯 유형으로 슬롯 정보를 가져옵니다.
    /// </summary>
    public EquipmentSlot GetSlot(string slotType) => data.slots.Find(s => s.slotType == slotType);

    /// <summary>
    /// 저장소에서 현재 종족의 장비 데이터를 불러옵니다.
    /// </summary>
    public void Load()
    {
        data = SaveLoadService.LoadEquipmentForRaceOrNew(race);
    }

    /// <summary>
    /// 현재 장비 데이터를 저장합니다.
    /// </summary>
    public void Save()
    {
        SaveLoadService.SaveEquipmentForRace(race, data);
    }
}
