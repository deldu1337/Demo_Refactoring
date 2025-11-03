using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveLoadService
{
    private const string LegacyPlayerFile = "playerData.json";

    // 레거시(통합) 파일명
    private const string InventoryFile = "playerInventory.json";
    private const string EquipmentFile = "playerEquipment.json";

    private static string PathOf(string fileName) =>
        System.IO.Path.Combine(Application.persistentDataPath, fileName);

    private static void EnsureDir(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public static void Save<T>(T data, string fileName, bool prettyPrint = true)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint);
            File.WriteAllText(PathOf(fileName), json);
#if UNITY_EDITOR
            Debug.Log($"[SaveLoadService] Saved {typeof(T).Name} → {PathOf(fileName)}");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadService] Save failed ({typeof(T).Name}): {e}");
        }
    }

    public static bool TryLoad<T>(string fileName, out T data)
    {
        string path = PathOf(fileName);
        if (!File.Exists(path))
        {
            data = default;
#if UNITY_EDITOR
            Debug.LogWarning($"[SaveLoadService] Not found: {path}");
#endif
            return false;
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<T>(json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadService] Load failed ({typeof(T).Name}): {e}");
            data = default;
            return false;
        }
    }

    // ---------- PlayerData: 종족별 ----------
    private static string PlayerFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerData_{race}.json";
    }

    public static void SavePlayerDataForRace(string race, PlayerData data)
        => Save(data, PlayerFileFor(race));

    public static PlayerData LoadPlayerDataForRaceOrNull(string race)
        => TryLoad(PlayerFileFor(race), out PlayerData data) ? data : null;

    public static PlayerData LoadLegacyPlayerDataOrNull()
        => TryLoad(LegacyPlayerFile, out PlayerData data) ? data : null;

    public static void MigrateLegacyIfMatchRace(PlayerData legacy, string race)
    {
        if (legacy == null) return;
        if (string.IsNullOrEmpty(legacy.Race) || string.Equals(legacy.Race, race, StringComparison.OrdinalIgnoreCase))
        {
            legacy.Race = race;
            SavePlayerDataForRace(race, legacy);
        }
    }

    // ---------- Inventory: 종족별 ----------
    private static string InventoryFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerInventory_{race}.json";
    }

    public static void SaveInventoryForRace(string race, InventoryData data)
        => Save(data, InventoryFileFor(race));

    /// <summary>
    /// 종족별 인벤토리 로드. 없으면 레거시 통합 파일을 찾아 1회 마이그레이션 후 반환.
    /// 아무 것도 없으면 빈 데이터 반환.
    /// </summary>
    public static InventoryData LoadInventoryForRaceOrNew(string race)
    {
        // 1) 종족별 파일 우선
        if (TryLoad(InventoryFileFor(race), out InventoryData perRace) && perRace != null)
            return perRace;

        // 2) 레거시 통합 파일 있으면 → 현재 종족 파일로 마이그레이션
        if (TryLoad(InventoryFile, out InventoryData legacy) && legacy != null)
        {
            SaveInventoryForRace(race, legacy);
#if UNITY_EDITOR
            Debug.Log($"[SaveLoadService] Migrated legacy {InventoryFile} → {InventoryFileFor(race)}");
#endif
            return legacy;
        }

        // 3) 없으면 새 데이터
        return new InventoryData();
    }

    // (레거시 API: 남겨두면 호환에 도움됨)
    public static void SaveInventory(InventoryData data) => Save(data, InventoryFile);
    public static InventoryData LoadInventoryOrNew()
        => TryLoad(InventoryFile, out InventoryData data) && data != null ? data : new InventoryData();

    // ---------- Equipment: 종족별 ----------
    private static string EquipmentFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerEquipment_{race}.json";
    }

    public static void SaveEquipmentForRace(string race, EquipmentData data)
        => Save(data, EquipmentFileFor(race));

    public static EquipmentData LoadEquipmentForRaceOrNew(string race)
    {
        if (TryLoad(EquipmentFileFor(race), out EquipmentData perRace) && perRace != null)
            return perRace;

        if (TryLoad(EquipmentFile, out EquipmentData legacy) && legacy != null)
        {
            SaveEquipmentForRace(race, legacy);
#if UNITY_EDITOR
            Debug.Log($"[SaveLoadService] Migrated legacy {EquipmentFile} → {EquipmentFileFor(race)}");
#endif
            return legacy;
        }
        return new EquipmentData();
    }

    // (레거시 API)
    public static void SaveEquipment(EquipmentData data) => Save(data, EquipmentFile);
    public static EquipmentData LoadEquipmentOrNew()
        => TryLoad(EquipmentFile, out EquipmentData data) && data != null ? data : new EquipmentData();
}
