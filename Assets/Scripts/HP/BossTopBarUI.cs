using UnityEngine;

public class BossTopBarUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject root;       // ← BossCanvas (처음엔 비활성)

    [Header("Components")]
    [SerializeField] private HealthBarUI healthBar; // ← BG(HealthBarUI) 참조

    private IHealth bossHealth;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        if (healthBar == null) healthBar = GetComponentInChildren<HealthBarUI>(true);
    }

    /// <summary>표시할 보스 지정</summary>
    public void SetBoss(IHealth boss)
    {
        bossHealth = boss;

        if (healthBar != null)
        {
            healthBar.SetTargetIHealth(bossHealth);
            healthBar.CheckHp(); // 즉시 1회 갱신
        }

        Show(false); // 근접 전엔 숨김
    }

    public void Show(bool on)
    {
        if (root != null && root.activeSelf != on)
            root.SetActive(on);
    }

    void Update()
    {
        // 보스 사망/소멸 시 자동 숨김
        if (bossHealth == null || bossHealth.CurrentHP <= 0f)
            Show(false);
    }
}
