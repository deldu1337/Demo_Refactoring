using System;

[Serializable]
public class PlayerData
{
    public string Race; // 추가: 저장/비교용

    // Base (종족/레벨업으로만 변함)
    public float BaseMaxHP;
    public float BaseMaxMP;
    public float BaseAtk;
    public float BaseDef;
    public float BaseDex;
    public float BaseAttackSpeed;
    public float BaseCritChance;
    public float BaseCritDamage;

    // 최종 스탯
    public float MaxHP;
    public float MaxMP;
    public float Atk;
    public float Def;
    public float Dex;
    public float AttackSpeed;
    public float CritChance;
    public float CritDamage;

    // 현재 상태
    public float CurrentHP;
    public float CurrentMP;

    // 레벨 / 경험치
    public int Level;
    public float Exp;
    public float ExpToNextLevel;
}