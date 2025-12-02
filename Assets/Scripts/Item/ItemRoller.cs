using UnityEngine;

public static class ItemRoller
{
    private static readonly string[] Stats = { "hp", "mp", "atk", "def", "dex", "As", "cc", "cd" };

    /// <summary>
    /// 지정된 아이템 ID에 따라 무작위 스탯을 생성합니다.
    /// </summary>
    /// <param name="itemId">범위를 조회할 아이템 ID입니다.</param>
    /// <returns>하나라도 굴려진 스탯이 있을 때 결과를 반환합니다.</returns>
    public static RolledItemStats CreateRolledStats(int itemId)
    {
        var dm = DataManager.Instance;
        if (dm == null) return null;

        var rolled = new RolledItemStats();
        bool any = false;

        foreach (var stat in Stats)
        {
            if (dm.TryGetRange(itemId, stat, out var r) && r != null)
            {
                float v = RollByRule(stat, r.min, r.max);
                rolled.Set(stat, v);
                any = true;
            }
        }

        return any ? rolled : null;
    }

    /// <summary>
    /// 스탯 유형에 맞춰 정수 또는 소수점 한 자리까지의 값을 굴립니다.
    /// </summary>
    /// <param name="stat">굴릴 스탯 키입니다.</param>
    /// <param name="min">최소 허용 값입니다.</param>
    /// <param name="max">최대 허용 값입니다.</param>
    /// <returns>규칙에 맞춰 생성된 난수 값을 반환합니다.</returns>
    private static float RollByRule(string stat, float min, float max)
    {
        if (stat == "hp" || stat == "mp")
        {
            int imin = Mathf.CeilToInt(min);
            int imax = Mathf.FloorToInt(max);
            if (imax < imin) imax = imin;
            return Random.Range(imin, imax + 1);
        }
        else
        {
            // 소수 첫째 자리 단위로 범위를 계산합니다.
            int start = Mathf.RoundToInt(min * 10f);
            int end = Mathf.RoundToInt(max * 10f);
            if (end < start) end = start;
            int k = Random.Range(start, end + 1);
            return Mathf.Round(k / 10f * 10f) / 10f;
        }
    }

    /// <summary>
    /// 주어진 값이 해당 스탯 범위의 최대치인지 확인합니다.
    /// </summary>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <param name="stat">확인할 스탯 키입니다.</param>
    /// <param name="value">비교할 값입니다.</param>
    /// <returns>최대치일 때 참을 반환합니다.</returns>
    public static bool IsMaxRoll(int itemId, string stat, float value)
    {
        var dm = DataManager.Instance;
        if (dm != null && dm.TryGetRange(itemId, stat, out var r) && r != null)
        {
            if (stat == "hp" || stat == "mp")
            {
                return Mathf.RoundToInt(value) == Mathf.RoundToInt(r.max);
            }
            else
            {
                int v10 = Mathf.RoundToInt(value * 10f);
                int max10 = Mathf.RoundToInt(r.max * 10f);
                return v10 == max10;
            }
        }
        return false;
    }
}
