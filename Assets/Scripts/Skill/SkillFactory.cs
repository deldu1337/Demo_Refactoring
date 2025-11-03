using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    private static Dictionary<string, ISkill> skillCache = new();

    public static void LoadSkillsFromJson(string json)
    {
        AllSkillData allSkills = JsonUtility.FromJson<AllSkillData>(json);

        LoadClassSkills(allSkills.warrior, "warrior");
        LoadClassSkills(allSkills.mage, "mage");
        LoadClassSkills(allSkills.rogue, "rogue");

        Debug.Log($"총 {skillCache.Count}개의 스킬 로드 완료");
    }

    private static void LoadClassSkills(SkillData[] skills, string className)
    {
        if (skills == null) return;
        foreach (var data in skills)
        {
            ISkill skill = CreateSkill(data);
            skillCache[$"{className}:{data.id}"] = skill;
        }
    }

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

    public static ISkill GetSkill(string className, string id)
    {
        return skillCache.TryGetValue($"{className}:{id}", out var skill) ? skill : null;
    }
}
