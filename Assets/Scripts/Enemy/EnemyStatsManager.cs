using UnityEngine;

public class EnemyStatsManager : MonoBehaviour, IHealth
{
    [Header("적 ID (enemyData.json의 id와 일치)")]
    public string enemyId;

    public EnemyData Data { get; private set; }
    public float CurrentHP { get; private set; }
    public float MaxHP => Data.hp;

    private ItemDropManager dropManager;

    void Awake()
    {
        dropManager = GetComponent<ItemDropManager>();
        LoadEnemyData();
    }

    private void LoadEnemyData()
    {
        TextAsset json = Resources.Load<TextAsset>("Datas/enemyData");
        if (json == null) { Debug.LogError("Resources/Datas/enemyData.json 필요!"); return; }

        EnemyDatabase db = JsonUtility.FromJson<EnemyDatabase>(json.text);
        Data = System.Array.Find(db.enemies, e => e.id == enemyId);
        if (Data == null) { Debug.LogError($"enemyId '{enemyId}' 데이터가 없습니다!"); return; }

        CurrentHP = Data.hp;
    }

    public void TakeDamage(float damage)
    {
        damage = Mathf.Max(damage - Data.def, 1f);
        CurrentHP = Mathf.Max(CurrentHP - damage, 0);
        Debug.Log($"{Data.name} HP: {CurrentHP}/{Data.hp}");

        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{Data.name} 사망!");

        var player = PlayerStatsManager.Instance;   // ← 변경
        if (player != null)
        {
            player.GainExp(Data.exp);
            Debug.Log($"플레이어가 {Data.exp} EXP를 획득!");
        }

        dropManager?.DropItems();
        Destroy(gameObject);
    }


    public void Heal(float amount)
    {
        if (CurrentHP <= 0) return;
        CurrentHP = Mathf.Min(CurrentHP + amount, Data.hp);
    }
}
