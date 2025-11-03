using System.IO;
using UnityEngine;

public static class PotionQuickBarPersistence
{
    private const string LegacyFile = "potion_quickbar.json";

    private static string PathOf(string fileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    private static string FileNameForRace(string race)
    {
        var rk = string.IsNullOrWhiteSpace(race) ? "humanmale" : race.ToLower();
        return $"potion_quickbar_{rk}.json";
    }

    private static string FilePathForRace(string race)
        => PathOf(FileNameForRace(race));

    /// <summary>종족별 저장</summary>
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
    /// 종족별 로드. 없으면 레거시 파일(통합) 있나 보고, 있으면 해당 종족 파일로 1회 마이그레이션해서 반환.
    /// 둘 다 없으면 빈 데이터 반환.
    /// </summary>
    public static PotionQuickBarSave LoadForRaceOrNew(string race)
    {
        // 1) 종족별 파일 우선
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

        // 2) 레거시 파일 있으면 → 현재 종족 파일로 마이그레이션
        var legacyPath = PathOf(LegacyFile);
        if (File.Exists(legacyPath))
        {
            try
            {
                var json = File.ReadAllText(legacyPath);
                var legacy = JsonUtility.FromJson<PotionQuickBarSave>(json) ?? new PotionQuickBarSave();

                // 즉시 현 종족 파일로 저장
                SaveForRace(race, legacy);
#if UNITY_EDITOR
                Debug.Log($"[PotionQuickBarPersistence] Migrated legacy {LegacyFile} → {FileNameForRace(race)}");
#endif
                return legacy;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PotionQuickBarPersistence] Migrate failed: {e}");
                return new PotionQuickBarSave();
            }
        }

        // 3) 아무것도 없으면 빈 데이터
        return new PotionQuickBarSave();
    }
}
