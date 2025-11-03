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
    private readonly string race;   // ★ 추가: 종족
    private EquipmentData data;

    public IReadOnlyList<EquipmentSlot> Slots => data.slots;

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

    public void EquipItem(string slotType, InventoryItem item)
    {
        var slot = data.slots.Find(s => s.slotType == slotType);
        if (slot != null)
        {
            slot.equipped = item;
            Save();
            Debug.Log($"{slotType} 슬롯에 {item.data.name} 장착됨");
        }
        else
        {
            Debug.LogWarning($"EquipItem 실패: {slotType} 슬롯을 찾을 수 없음");
        }
    }

    public void UnequipItem(string slotType)
    {
        var slot = data.slots.Find(s => s.slotType == slotType);
        if (slot != null)
        {
            Debug.Log($"{slotType} 슬롯에서 {slot.equipped?.data?.name ?? "없음"} 해제");
            slot.equipped = null;
            Save();
        }
        else
        {
            Debug.LogWarning($"UnequipItem 실패: {slotType} 슬롯을 찾을 수 없음");
        }
    }

    public void Unequip(int index)
    {
        if (index < 0 || index >= data.slots.Count) return;
        var slot = data.slots[index];
        Debug.Log($"{slot.slotType} 슬롯에서 {slot.equipped?.data?.name ?? "없음"} 해제");
        slot.equipped = null;
        Save();
    }

    public EquipmentSlot GetSlot(string slotType) => data.slots.Find(s => s.slotType == slotType);

    public void Load()
    {
        data = SaveLoadService.LoadEquipmentForRaceOrNew(race);
    }

    public void Save()
    {
        SaveLoadService.SaveEquipmentForRace(race, data);
    }
}
