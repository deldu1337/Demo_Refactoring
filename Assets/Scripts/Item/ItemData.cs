using System;

/// <summary>
/// 아이템의 기본 스탯과 식별 정보를 보관하는 데이터 클래스입니다.
/// </summary>
[Serializable]
public class ItemData
{
    public int id;      // 아이템의 고유 ID입니다.
    public string name; // UI에 표시할 아이템 이름입니다.
    public string uniqueName;
    public int level;
    public string tier;
    public float hp;
    public float mp;
    public float atk;   // 공격력에 해당하는 수치입니다.
    public float def;
    public float dex;
    public float As;
    public float cc;
    public float cd;
    public string type; // 아이템의 분류 정보입니다.
}
