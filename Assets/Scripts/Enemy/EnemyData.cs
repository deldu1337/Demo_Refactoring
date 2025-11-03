using System;

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
    public float exp; // 적 처치 시 주는 경험치
    public int unlockStage = 1;
    public bool isBoss = false;
    public float weight = 1f;
    public int minStage;
    public int maxStage;
}

[Serializable]
public class EnemyDatabase
{
    public EnemyData[] enemies;
}