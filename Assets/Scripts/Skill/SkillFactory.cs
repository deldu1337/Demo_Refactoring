using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 데이터를 읽어 실제 스킬 인스턴스를 생성하는 팩토리입니다.
/// </summary>
public static class SkillFactory
{
    private static Dictionary<string, ISkill> skillCache = new();

    /// <summary>
    /// JSON 문자열에서 스킬을 로드합니다.
    /// </summary>
    public static void LoadSkillsFromJson(string json)
    {
        AllSkillData allSkills = JsonUtility.FromJson<AllSkillData>(json);

        LoadClassSkills(allSkills.warrior, "warrior");
        LoadClassSkills(allSkills.mage, "mage");
        LoadClassSkills(allSkills.rogue, "rogue");

        Debug.Log($"총 {skillCache.Count}개의 스킬을 불러왔습니다.");
    }

    /// <summary>
    /// 직업별 스킬 목록을 캐시에 적재합니다.
    /// </summary>
    private static void LoadClassSkills(SkillData[] skills, string className)
    {
        if (skills == null) return;
        foreach (var data in skills)
        {
            ISkill skill = CreateSkill(data);
            skillCache[$"{className}:{data.id}"] = skill;
        }
    }

    /// <summary>
    /// 스킬 타입에 맞춰 인스턴스를 생성합니다.
    /// </summary>
    private static ISkill CreateSkill(SkillData data)
    {
        return data.type switch
        {
            "ActiveSkill" => new ActiveSkill(data),
            "ProjectileSkill" => new ProjectileSkill(data),
            "ChargeSkill" => new ChargeSkill(data),
            _ => new ActiveSkill(data)
        };
    }

    /// <summary>
    /// 직업과 아이디로 스킬을 조회합니다.
    /// </summary>
    public static ISkill GetSkill(string className, string id)
    {
        return skillCache.TryGetValue($"{className}:{id}", out var skill) ? skill : null;
    }
}
