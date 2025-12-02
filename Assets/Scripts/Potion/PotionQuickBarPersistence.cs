using System.IO;
using UnityEngine;

/// <summary>
/// 포션 퀵바 데이터를 파일로 저장하고 불러오는 기능을 제공합니다.
/// </summary>
public static class PotionQuickBarPersistence
{
    private const string LegacyFile = "potion_quickbar.json";

    /// <summary>
    /// 저장 파일의 전체 경로를 생성합니다.
    /// </summary>
    private static string PathOf(string fileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    /// <summary>
    /// 종족 이름에 맞는 파일 이름을 반환합니다.
    /// </summary>
    private static string FileNameForRace(string race)
    {
        var rk = string.IsNullOrWhiteSpace(race) ? "humanmale" : race.ToLower();
        return $"potion_quickbar_{rk}.json";
    }

    /// <summary>
    /// 종족 이름에 맞는 저장 파일 경로를 반환합니다.
    /// </summary>
    private static string FilePathForRace(string race)
        => PathOf(FileNameForRace(race));

    /// <summary>저장 데이터를 파일로 기록합니다.</summary>
    public static void SaveForRace(string race, PotionQuickBarSave data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePathForRace(race), json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PotionQuickBarPersistence] Save failed: {e}");
        }
    }

    /// <summary>
    /// 저장된 데이터를 불러오거나, 없을 경우 새 데이터를 제공합니다.
    /// 기존 포맷이 있다면 한 번만 마이그레이션합니다.
    /// </summary>
    public static PotionQuickBarSave LoadForRaceOrNew(string race)
    {
        // 1) 종족별 파일이 있으면 우선 사용합니다.
        var perRacePath = FilePathForRace(race);
        if (File.Exists(perRacePath))
        {
            try
            {
                var json = File.ReadAllText(perRacePath);
                return JsonUtility.FromJson<PotionQuickBarSave>(json) ?? new PotionQuickBarSave();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PotionQuickBarPersistence] Load failed (per race): {e}");
                return new PotionQuickBarSave();
            }
        }

        // 2) 이전 포맷 파일이 있다면 이를 마이그레이션합니다.
        var legacyPath = PathOf(LegacyFile);
        if (File.Exists(legacyPath))
        {
            try
            {
                var json = File.ReadAllText(legacyPath);
                var legacy = JsonUtility.FromJson<PotionQuickBarSave>(json) ?? new PotionQuickBarSave();

                // 새 파일로 저장하여 이후부터 사용하도록 합니다.
                SaveForRace(race, legacy);
#if UNITY_EDITOR
                Debug.Log($"[PotionQuickBarPersistence] Migrated legacy {LegacyFile}  {FileNameForRace(race)}");
#endif
                return legacy;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PotionQuickBarPersistence] Migrate failed: {e}");
                return new PotionQuickBarSave();
            }
        }

        // 3) 어떤 파일도 없으면 빈 데이터를 반환합니다.
        return new PotionQuickBarSave();
    }
}
