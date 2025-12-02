using UnityEngine;

/// <summary>
/// 스킬 해제에 필요한 정보를 담는 정의 클래스입니다.
/// </summary>
[System.Serializable]
public class SkillUnlockDef
{
    public string skillId;
    public int unlockLevel;

    /// <summary>
    /// 아이디와 해제 레벨을 받아 정의를 생성합니다.
    /// </summary>
    public SkillUnlockDef(string id, int lv)
    {
        skillId = id; unlockLevel = lv;
    }
}
