using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveLoadService
{
    private const string LegacyPlayerFile = "playerData.json";

    /// <summary>
    /// 레거시 플레이어 데이터를 저장하는 파일명입니다.
    /// </summary>
    private const string InventoryFile = "playerInventory.json";

    /// <summary>
    /// 레거시 장비 데이터를 저장하는 파일명입니다.
    /// </summary>
    private const string EquipmentFile = "playerEquipment.json";

    /// <summary>
    /// 파일명을 받아 영구 저장 경로와 합쳐진 전체 경로를 반환합니다.
    /// </summary>
    private static string PathOf(string fileName) =>
        System.IO.Path.Combine(Application.persistentDataPath, fileName);

    /// <summary>
    /// 저장할 경로에 상위 디렉터리가 없으면 생성합니다.
    /// </summary>
    private static void EnsureDir(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// 데이터를 JSON으로 변환하여 지정된 파일에 저장합니다.
    /// </summary>
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

    /// <summary>
    /// 파일에서 JSON을 읽어 지정된 타입으로 변환하는 데 성공하면 true를 반환합니다.
    /// </summary>
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

    // 플레이어 데이터 파일을 종족별로 구분하여 관리합니다.

    /// <summary>
    /// 종족 이름을 받아 해당 종족의 플레이어 데이터 파일명을 반환합니다.
    /// </summary>
    private static string PlayerFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerData_{race}.json";
    }

    /// <summary>
    /// 주어진 종족에 대한 플레이어 데이터를 저장합니다.
    /// </summary>
    public static void SavePlayerDataForRace(string race, PlayerData data)
        => Save(data, PlayerFileFor(race));

    /// <summary>
    /// 종족별 플레이어 데이터를 불러오고 없으면 null을 반환합니다.
    /// </summary>
    public static PlayerData LoadPlayerDataForRaceOrNull(string race)
        => TryLoad(PlayerFileFor(race), out PlayerData data) ? data : null;

    /// <summary>
    /// 레거시 플레이어 데이터를 불러오고 없으면 null을 반환합니다.
    /// </summary>
    public static PlayerData LoadLegacyPlayerDataOrNull()
        => TryLoad(LegacyPlayerFile, out PlayerData data) ? data : null;

    /// <summary>
    /// 레거시 플레이어 데이터가 요청한 종족과 일치하면 신규 파일로 저장합니다.
    /// </summary>
    public static void MigrateLegacyIfMatchRace(PlayerData legacy, string race)
    {
        if (legacy == null) return;
        if (string.IsNullOrEmpty(legacy.Race) || string.Equals(legacy.Race, race, StringComparison.OrdinalIgnoreCase))
        {
            legacy.Race = race;
            SavePlayerDataForRace(race, legacy);
        }
    }

    // 인벤토리 데이터를 종족별로 관리합니다.

    /// <summary>
    /// 종족 이름을 받아 해당 종족의 인벤토리 파일명을 반환합니다.
    /// </summary>
    private static string InventoryFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerInventory_{race}.json";
    }

    /// <summary>
    /// 종족별 인벤토리 데이터를 저장합니다.
    /// </summary>
    public static void SaveInventoryForRace(string race, InventoryData data)
        => Save(data, InventoryFileFor(race));

    /// <summary>
    /// 종족별 인벤토리를 우선 로드하고 없으면 레거시 파일을 마이그레이션합니다.
    /// 데이터가 없으면 새 인벤토리를 생성합니다.
    /// </summary>
    public static InventoryData LoadInventoryForRaceOrNew(string race)
    {
        // 종족별 파일을 우선 확인합니다.
        if (TryLoad(InventoryFileFor(race), out InventoryData perRace) && perRace != null)
            return perRace;

        // 레거시 통합 파일이 있으면 현재 종족 파일로 저장합니다.
        if (TryLoad(InventoryFile, out InventoryData legacy) && legacy != null)
        {
            SaveInventoryForRace(race, legacy);
#if UNITY_EDITOR
            Debug.Log($"[SaveLoadService] Migrated legacy {InventoryFile} → {InventoryFileFor(race)}");
#endif
            return legacy;
        }

        // 아무 데이터가 없으면 새 인벤토리를 만듭니다.
        return new InventoryData();
    }

    // 레거시 인벤토리 API를 유지합니다.

    /// <summary>
    /// 레거시 인벤토리 파일에 데이터를 저장합니다.
    /// </summary>
    public static void SaveInventory(InventoryData data) => Save(data, InventoryFile);

    /// <summary>
    /// 레거시 인벤토리를 불러오고 없으면 새 인벤토리를 생성합니다.
    /// </summary>
    public static InventoryData LoadInventoryOrNew()
        => TryLoad(InventoryFile, out InventoryData data) && data != null ? data : new InventoryData();

    // 장비 데이터를 종족별로 관리합니다.

    /// <summary>
    /// 종족 이름을 받아 해당 종족의 장비 파일명을 반환합니다.
    /// </summary>
    private static string EquipmentFileFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"playerEquipment_{race}.json";
    }

    /// <summary>
    /// 종족별 장비 데이터를 저장합니다.
    /// </summary>
    public static void SaveEquipmentForRace(string race, EquipmentData data)
        => Save(data, EquipmentFileFor(race));

    /// <summary>
    /// 종족별 장비를 우선 로드하고 없으면 레거시 파일을 마이그레이션합니다.
    /// 데이터가 없으면 새 장비 데이터를 생성합니다.
    /// </summary>
    public static EquipmentData LoadEquipmentForRaceOrNew(string race)
    {
        // 종족별 파일을 먼저 확인합니다.
        if (TryLoad(EquipmentFileFor(race), out EquipmentData perRace) && perRace != null)
            return perRace;

        // 레거시 통합 파일이 있으면 현재 종족 파일로 저장합니다.
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

    // 레거시 장비 API를 유지합니다.

    /// <summary>
    /// 레거시 장비 데이터를 저장합니다.
    /// </summary>
    public static void SaveEquipment(EquipmentData data) => Save(data, EquipmentFile);

    /// <summary>
    /// 레거시 장비 데이터를 불러오고 없으면 새 장비 데이터를 생성합니다.
    /// </summary>
    public static EquipmentData LoadEquipmentOrNew()
        => TryLoad(EquipmentFile, out EquipmentData data) && data != null ? data : new EquipmentData();
}
