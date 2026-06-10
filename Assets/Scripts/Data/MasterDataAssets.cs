using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Skill Database", fileName = "SkillDatabase")]
public class SkillDatabaseAsset : ScriptableObject
{
    public SkillData[] warrior = Array.Empty<SkillData>();
    public SkillData[] mage = Array.Empty<SkillData>();
    public SkillData[] rogue = Array.Empty<SkillData>();

    public SkillData[] GetSkills(string className)
    {
        return className switch
        {
            "warrior" => warrior,
            "mage" => mage,
            "rogue" => rogue,
            _ => Array.Empty<SkillData>()
        };
    }
}

[CreateAssetMenu(menuName = "Data/Enemy Database", fileName = "EnemyDatabase")]
public class EnemyDatabaseAsset : ScriptableObject
{
    public EnemyData[] enemies = Array.Empty<EnemyData>();

    public EnemyData FindById(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId) || enemies == null)
            return null;

        return Array.Find(enemies, enemy => enemy.id == enemyId);
    }
}

[CreateAssetMenu(menuName = "Data/Item Database", fileName = "ItemDatabase")]
public class ItemDatabaseAsset : ScriptableObject
{
    public ItemData[] items = Array.Empty<ItemData>();
    public ItemRangeEntry[] itemRanges = Array.Empty<ItemRangeEntry>();
}
