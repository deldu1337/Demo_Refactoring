using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [SerializeField] private Image cooldownOverlay; // Inspector 연결 or BindOverlay로 주입
    private float cooldownTime;
    private float cooldownRemaining;
    private bool isRunning; // ★ 추가: 실제로 카운트 중인지

    // 외부에서 Overlay 연결
    public void BindOverlay(Image overlay)
    {
        cooldownOverlay = overlay;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = 2; // Top
            cooldownOverlay.fillClockwise = false;

            // 아직 러닝이 아니라면 0으로 초기화 (러닝 중이면 유지)
            if (!isRunning) cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = true;
        }
    }

    void Awake()
    {
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        isRunning = false;
        cooldownTime = 0f;
        cooldownRemaining = 0f;
    }

    void Update()
    {
        // 러닝 중이 아니거나 오버레이가 없으면 터치하지 않음
        if (!isRunning || cooldownOverlay == null) return;

        // 시간 진행
        cooldownRemaining -= Time.deltaTime;

        if (cooldownTime > 0f)
        {
            float t = Mathf.Clamp01(cooldownRemaining / cooldownTime);
            cooldownOverlay.fillAmount = t;
        }

        // 끝났으면 한 번만 0으로 만들고 정지
        if (cooldownRemaining <= 0f)
        {
            cooldownOverlay.fillAmount = 0f;
            isRunning = false;
        }
    }

    public void StartCooldown(float duration)
    {
        // 방어: 0이하 쿨다운은 의미 없으니 그냥 종료 처리
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
