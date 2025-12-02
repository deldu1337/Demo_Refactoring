using UnityEngine;

public class BossTopBarUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject root;       // 보스 HP 바를 담는 루트 오브젝트입니다.

    [Header("Components")]
    [SerializeField] private HealthBarUI healthBar; // 체력 표시를 담당하는 자식 HealthBarUI입니다.

    private IHealth bossHealth;

    /// <summary>
    /// 초기 상태로 UI를 숨기고 필수 컴포넌트를 설정합니다.
    /// </summary>
    void Awake()
    {
        if (root != null) root.SetActive(false);
        if (healthBar == null) healthBar = GetComponentInChildren<HealthBarUI>(true);
    }

    /// <summary>
    /// 보스 체력 정보를 체력바와 연결해 표시를 준비합니다.
    /// </summary>
    public void SetBoss(IHealth boss)
    {
        bossHealth = boss;

        if (healthBar != null)
        {
            healthBar.SetTargetIHealth(bossHealth);
            healthBar.CheckHp(); // 초기 체력 상태를 한 번 즉시 반영합니다.
        }

        Show(false); // 시작 시에는 보이지 않도록 설정합니다.
    }

    /// <summary>
    /// 보스 HP UI의 노출 상태를 토글합니다.
    /// </summary>
    public void Show(bool on)
    {
        if (root != null && root.activeSelf != on)
            root.SetActive(on);
    }

    /// <summary>
    /// 보스가 사망했거나 참조가 사라진 경우 UI를 숨깁니다.
    /// </summary>
    void Update()
    {
        if (bossHealth == null || bossHealth.CurrentHP <= 0f)
            Show(false);
    }
}
