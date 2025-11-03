using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private MonoBehaviour hpSource;
    private IHealth hasHP;
    [SerializeField] private Image barImage;

    void Awake()
    {
        if (hpSource != null) hasHP = hpSource as IHealth;
        if (hasHP == null) hasHP = GetComponentInParent<IHealth>();
        if (barImage == null) barImage = GetComponentInChildren<Image>();
    }

    void Update()
    {
        UpdateBar();
    }

    /// <summary> 외부에서 체력바를 강제로 갱신하고 싶을 때 호출 </summary>
    public void CheckHp()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (hasHP == null || barImage == null) return;
        float maxHp = hasHP.MaxHP > 0 ? hasHP.MaxHP : 1f;
        barImage.fillAmount = hasHP.CurrentHP / maxHp;
    }

    public void SetTarget(MonoBehaviour newSource)
    {
        hpSource = newSource;
        hasHP = hpSource as IHealth;
    }
    public void SetTargetIHealth(IHealth newTarget)
    {
        hasHP = newTarget;
    }
}
