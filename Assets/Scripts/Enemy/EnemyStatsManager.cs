using UnityEngine;

/// <summary>
/// 적의 능력치와 체력을 관리하고 피해 및 회복 처리를 담당합니다.
/// </summary>
public class EnemyStatsManager : MonoBehaviour, IHealth
{
    [Header("적 ID (enemyData.json의 id 필드)")]
    public string enemyId;

    public EnemyData Data { get; private set; }
    public float CurrentHP { get; private set; }
    public float MaxHP => Data.hp;

    private ItemDropManager dropManager;

    /// <summary>
    /// 아이템 드랍 매니저를 찾고 적 데이터를 불러옵니다.
    /// </summary>
    private void Awake()
    {
        dropManager = GetComponent<ItemDropManager>();
        LoadEnemyData();
    }

    /// <summary>
    /// 리소스에서 적 데이터를 로드하고 현재 체력을 초기화합니다.
    /// </summary>
    private void LoadEnemyData()
    {
        TextAsset json = Resources.Load<TextAsset>("Datas/enemyData");
        if (json == null) { Debug.LogError("Resources/Datas/enemyData.json을 찾을 수 없습니다!"); return; }

        EnemyDatabase db = JsonUtility.FromJson<EnemyDatabase>(json.text);
        Data = System.Array.Find(db.enemies, e => e.id == enemyId);
        if (Data == null) { Debug.LogError($"enemyId '{enemyId}'에 해당하는 데이터가 없습니다!"); return; }

        CurrentHP = Data.hp;
    }

    /// <summary>
    /// 받은 피해량을 계산해 체력을 감소시키고 사망 여부를 확인합니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        damage = Mathf.Max(damage - Data.def, 1f);
        CurrentHP = Mathf.Max(CurrentHP - damage, 0);
        Debug.Log($"{Data.name} HP: {CurrentHP}/{Data.hp}");

        if (CurrentHP <= 0)
            Die();
    }

    /// <summary>
    /// 적 사망 처리와 경험치 지급, 드랍을 수행합니다.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{Data.name} 처치!");

        var player = PlayerStatsManager.Instance;
        if (player != null)
        {
            player.GainExp(Data.exp);
            Debug.Log($"플레이어가 {Data.exp} EXP를 획득했습니다!");
        }

        dropManager?.DropItems();
        Destroy(gameObject);
    }

    /// <summary>
    /// 체력을 회복하되 최대 체력을 초과하지 않도록 제한합니다.
    /// </summary>
    public void Heal(float amount)
    {
        if (CurrentHP <= 0) return;
        CurrentHP = Mathf.Min(CurrentHP + amount, Data.hp);
    }
}
