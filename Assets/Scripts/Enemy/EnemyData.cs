using System;

/// <summary>
/// 적의 기본 스탯과 스폰 정보를 담는 데이터 구조체입니다.
/// </summary>
[Serializable]
public class EnemyData
{
    public string id;
    public string name;
    public float hp;
    public float atk;
    public float def;
    public float dex;
    public float As;
    public float exp; // 처치 시 지급되는 경험치입니다.
    public int unlockStage = 1;
    public bool isBoss = false;
    public float weight = 1f;
    public int minStage;
    public int maxStage;
}

/// <summary>
/// JSON에서 직렬화된 적 데이터 목록을 담는 래퍼 클래스입니다.
/// </summary>
[Serializable]
public class EnemyDatabase
{
    public EnemyData[] enemies;
}
