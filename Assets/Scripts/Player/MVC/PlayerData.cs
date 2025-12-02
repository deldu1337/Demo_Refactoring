using System;

[Serializable]
public class PlayerData
{
    public string Race; // 플레이어가 선택하신 종족 이름입니다.

    // Base: 장비 효과가 적용되기 전 기본 수치입니다.
    public float BaseMaxHP;
    public float BaseMaxMP;
    public float BaseAtk;
    public float BaseDef;
    public float BaseDex;
    public float BaseAttackSpeed;
    public float BaseCritChance;
    public float BaseCritDamage;

    // Final: 장비 효과가 포함된 최종 수치입니다.
    public float MaxHP;
    public float MaxMP;
    public float Atk;
    public float Def;
    public float Dex;
    public float AttackSpeed;
    public float CritChance;
    public float CritDamage;

    // 현재 상태를 나타내는 값입니다.
    public float CurrentHP;
    public float CurrentMP;

    // 성장 관련 정보입니다.
    public int Level;
    public float Exp;
    public float ExpToNextLevel;
}
