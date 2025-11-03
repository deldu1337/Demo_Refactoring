using System;

// 아이템의 기본 데이터를 저장하는 클래스
[Serializable] // Unity Inspector와 JSON 직렬화를 위해 직렬화 가능하게 함
public class ItemData
{
    public int id;      // 아이템 고유 ID, 게임 내에서 아이템을 식별할 때 사용
    public string name; // 아이템 이름, UI 표시나 로그 출력에 사용
    public string uniqueName;
    public int level;
    public string tier;
    public float hp;
    public float mp;
    public float atk;   // 아이템 공격력 또는 속성 값, 게임 로직에서 수치 계산에 사용
    public float def;
    public float dex;
    public float As;
    public float cc;
    public float cd;
    public string type; // 아이템 고유 타입
}