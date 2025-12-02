using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 쿨다운을 이미지 필로 표현하는 UI 컴포넌트입니다.
/// </summary>
public class SkillCooldownUI : MonoBehaviour
{
    [SerializeField] private Image cooldownOverlay;
    private float cooldownTime;
    private float cooldownRemaining;
    private bool isRunning;

    /// <summary>
    /// 외부에서 제공한 이미지로 오버레이를 연결합니다.
    /// </summary>
    public void BindOverlay(Image overlay)
    {
        cooldownOverlay = overlay;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = 2;
            cooldownOverlay.fillClockwise = false;

            if (!isRunning) cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = true;
        }
    }

    /// <summary>
    /// 초기 상태로 쿨다운을 리셋합니다.
    /// </summary>
    void Awake()
    {
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        isRunning = false;
        cooldownTime = 0f;
        cooldownRemaining = 0f;
    }

    /// <summary>
    /// 매 프레임 쿨다운 남은 시간을 계산합니다.
    /// </summary>
    void Update()
    {
        if (!isRunning || cooldownOverlay == null) return;

        cooldownRemaining -= Time.deltaTime;

        if (cooldownTime > 0f)
        {
            float t = Mathf.Clamp01(cooldownRemaining / cooldownTime);
            cooldownOverlay.fillAmount = t;
        }

        if (cooldownRemaining <= 0f)
        {
            cooldownOverlay.fillAmount = 0f;
            isRunning = false;
        }
    }

    /// <summary>
    /// 주어진 시간으로 쿨다운을 시작합니다.
    /// </summary>
    public void StartCooldown(float duration)
    {
        if (duration <= 0f)
        {
            isRunning = false;
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            return;
        }

        cooldownTime = duration;
        cooldownRemaining = duration;
        isRunning = true;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 1f;
    }
}
