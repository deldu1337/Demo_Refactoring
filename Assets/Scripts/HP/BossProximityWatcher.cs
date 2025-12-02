using System.Linq;
using UnityEngine;

public class BossProximityWatcher : MonoBehaviour
{
    [Header("Player (Layer)")]
    [SerializeField] private Transform player;         // 플레이어 트랜스폼 참조
    [SerializeField] private LayerMask playerLayer;    // 플레이어가 속한 레이어 마스크

    [Header("UI")]
    [SerializeField] private BossTopBarUI bossTopUI;   // 보스 HP를 표시할 최상단 UI

    [Header("Settings")]
    [SerializeField] private float showRadius = 100f;   // 보스 HP를 표시할 최대 거리

    private EnemyStatsManager bossESM;                 // 보스 스탯과 HP 정보를 가진 객체

    /// <summary>
    /// 씬에서 필요한 참조를 자동으로 찾아 초기화한다.
    /// </summary>
    void Awake()
    {
        if (player == null)
        {
            // 레이어 마스크에 속한 첫 번째 트랜스폼을 찾아 플레이어로 설정
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var t = all.FirstOrDefault(tf => (playerLayer.value & (1 << tf.gameObject.layer)) != 0);
            if (t != null) player = t;
        }

        if (bossTopUI == null)
            bossTopUI = FindAnyObjectByType<BossTopBarUI>();
    }

    /// <summary>
    /// 새로 등장한 보스의 체력 정보를 UI에 연결한다.
    /// </summary>
    public void SetBoss(EnemyStatsManager esm)
    {
        bossESM = esm;
        if (bossTopUI != null && bossESM != null)
        {
            bossTopUI.SetBoss(bossESM); // 체력바가 새 보스를 바라보도록 설정
        }
    }

    /// <summary>
    /// 플레이어와 보스의 거리를 확인해 보스 HP UI 노출 여부를 제어한다.
    /// </summary>
    void Update()
    {
        if (bossTopUI == null) return;

        if (bossESM == null || player == null || bossESM.CurrentHP <= 0f)
        {
            bossTopUI.Show(false);
            return;
        }

        float d = Vector3.Distance(player.position, bossESM.transform.position);
        bossTopUI.Show(d <= showRadius);
    }
}
