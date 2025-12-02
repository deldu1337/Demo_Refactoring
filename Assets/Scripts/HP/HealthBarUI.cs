using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private MonoBehaviour hpSource; // IHealth를 구현한 원본 컴포넌트입니다.
    private IHealth hasHP;
    [SerializeField] private Image barImage;         // 실제 체력바 이미지를 채울 UI입니다.

    /// <summary>
    /// 초기화 과정에서 체력 정보를 제공할 대상과 체력바 이미지를 찾습니다.
    /// </summary>
    void Awake()
    {
        if (hpSource != null) hasHP = hpSource as IHealth;
        if (hasHP == null) hasHP = GetComponentInParent<IHealth>();
        if (barImage == null) barImage = GetComponentInChildren<Image>();
    }

    /// <summary>
    /// 매 프레임 체력 변화를 반영해 체력바를 갱신합니다.
    /// </summary>
    void Update()
    {
        UpdateBar();
    }

    /// <summary>
    /// 외부에서 체력을 변경한 직후 즉시 체력바를 갱신합니다.
    /// </summary>
    public void CheckHp()
    {
        UpdateBar();
    }

    /// <summary>
    /// 현재 체력과 최대 체력을 이용해 체력바의 채움 비율을 계산합니다.
    /// </summary>
    private void UpdateBar()
    {
        if (hasHP == null || barImage == null) return;
        float maxHp = hasHP.MaxHP > 0 ? hasHP.MaxHP : 1f;
        barImage.fillAmount = hasHP.CurrentHP / maxHp;
    }

    /// <summary>
    /// 새로운 모노비헤이비어를 대상 체력 소스로 설정합니다.
    /// </summary>
    public void SetTarget(MonoBehaviour newSource)
    {
        hpSource = newSource;
        hasHP = hpSource as IHealth;
    }

    /// <summary>
    /// IHealth 인터페이스를 구현한 대상 객체로 체력 소스를 직접 지정합니다.
    /// </summary>
    public void SetTargetIHealth(IHealth newTarget)
    {
        hasHP = newTarget;
    }
}
