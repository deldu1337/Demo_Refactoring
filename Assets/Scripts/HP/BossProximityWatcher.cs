using System.Linq;
using UnityEngine;

public class BossProximityWatcher : MonoBehaviour
{
    [Header("Player (Layer로 탐색)")]
    [SerializeField] private Transform player;         // 비우면 playerLayer로 자동 탐색
    [SerializeField] private LayerMask playerLayer;    // 예: Player 레이어 체크

    [Header("UI")]
    [SerializeField] private BossTopBarUI bossTopUI;   // BossCanvas에 붙은 스크립트

    [Header("Settings")]
    [SerializeField] private float showRadius = 100f;   // 이 거리 이내면 HP바 표시

    private EnemyStatsManager bossESM;                 // IHealth 구현체

    void Awake()
    {
        // 플레이어를 레이어로 자동 탐색
        if (player == null)
        {
            // 씬 내 모든 Transform 중 레이어 일치하는 첫번째를 찾음
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var t = all.FirstOrDefault(tf => (playerLayer.value & (1 << tf.gameObject.layer)) != 0);
            if (t != null) player = t;
        }

        if (bossTopUI == null)
            bossTopUI = FindAnyObjectByType<BossTopBarUI>();
    }

    /// <summary>EnemySpawn에서 보스 스폰 후 호출</summary>
    public void SetBoss(EnemyStatsManager esm)
    {
        bossESM = esm;
        if (bossTopUI != null && bossESM != null)
        {
            bossTopUI.SetBoss(bossESM); // HealthBarUI 내부까지 타깃 세팅됨
        }
    }

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
