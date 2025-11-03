using UnityEngine;

[System.Serializable]
public class SkillUnlockDef
{
    public string skillId;
    public int unlockLevel;

    public SkillUnlockDef(string id, int lv)
    {
        skillId = id; unlockLevel = lv;
    }
}
