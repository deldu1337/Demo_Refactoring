using System.IO;
using UnityEngine;

/// <summary>
/// 퀵바 배치 정보를 파일로 저장하고 불러옵니다.
/// </summary>
public static class QuickBarPersistence
{
    private const string LegacyFileName = "quickbar.json";

    private static string PathOf(string fileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    private static string FileNameFor(string race)
    {
        if (string.IsNullOrEmpty(race)) race = "humanmale";
        return $"quickbar_{race}.json";
    }

    /// <summary>
    /// 종족별 파일로 퀵바 정보를 저장합니다.
    /// </summary>
    public static void SaveForRace(string race, QuickBarSave data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(PathOf(FileNameFor(race)), json);
#if UNITY_EDITOR
            Debug.Log($"[QuickBarPersistence] Saved ({race}): {PathOf(FileNameFor(race))}");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuickBarPersistence] SaveForRace failed: {e}");
        }
    }

    /// <summary>
    /// 종족별 파일을 불러오고 없으면 레거시 파일을 시도합니다.
    /// </summary>
    public static QuickBarSave LoadForRaceOrNull(string race)
    {
        string perRacePath = PathOf(FileNameFor(race));
        if (File.Exists(perRacePath))
        {
            try
            {
                var json = File.ReadAllText(perRacePath);
                return JsonUtility.FromJson<QuickBarSave>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuickBarPersistence] LoadForRace failed: {e}");
                return null;
            }
        }

        string legacyPath = PathOf(LegacyFileName);
        if (File.Exists(legacyPath))
        {
            try
            {
                var json = File.ReadAllText(legacyPath);
                var data = JsonUtility.FromJson<QuickBarSave>(json);
                if (data != null)
                {
                    SaveForRace(race, data);
#if UNITY_EDITOR
                    Debug.Log($"[QuickBarPersistence] Migrated {LegacyFileName} 를 {FileNameFor(race)}로 변환했습니다.");
#endif
                    return data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuickBarPersistence] Migrate legacy failed: {e}");
            }
        }

        return null;
    }

    /// <summary>
    /// 파일이 없을 때 새 데이터를 반환합니다.
    /// </summary>
    public static QuickBarSave LoadForRaceOrNew(string race)
        => LoadForRaceOrNull(race) ?? new QuickBarSave();

    private static string LegacyFilePath => PathOf(LegacyFileName);

    /// <summary>
    /// 레거시 파일 경로에 저장합니다.
    /// </summary>
    public static void Save(QuickBarSave data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(LegacyFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuickBarPersistence] Save (legacy) failed: {e}");
        }
    }

    /// <summary>
    /// 레거시 파일에서 데이터를 읽어옵니다.
    /// </summary>
    public static QuickBarSave Load()
    {
        try
        {
            if (!File.Exists(LegacyFilePath)) return null;
            var json = File.ReadAllText(LegacyFilePath);
            return JsonUtility.FromJson<QuickBarSave>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuickBarPersistence] Load (legacy) failed: {e}");
            return null;
        }
    }
}
