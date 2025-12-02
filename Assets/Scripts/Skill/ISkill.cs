using UnityEngine;

/// <summary>
/// 플레이어가 사용할 수 있는 스킬의 공통 인터페이스입니다.
/// </summary>
public interface ISkill
{
    string Id { get; }
    string Name { get; }
    float Cooldown { get; }
    float MpCost { get; }
    float Range { get; }
    float ImpactDelay { get; }

    /// <summary>
    /// 사용자와 능력치 정보를 받아 스킬을 실행합니다.
    /// </summary>
    bool Execute(GameObject user, PlayerStatsManager stats);
}
