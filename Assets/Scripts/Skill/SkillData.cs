/// <summary>
/// 개별 스킬의 기본 데이터를 보관하는 직렬화 클래스입니다.
/// </summary>
[System.Serializable]
public class SkillData
{
    public string id;
    public string name;
    public float cooldown;
    public float damage;
    public float mpCost;
    public float range;
    public float impactDelay;
    public string type;
    public string animation;
}

/// <summary>
/// 직업별 스킬 데이터를 한 번에 보관합니다.
/// </summary>
[System.Serializable]
public class AllSkillData
{
    public SkillData[] warrior;
    public SkillData[] mage;
    public SkillData[] rogue;
}
