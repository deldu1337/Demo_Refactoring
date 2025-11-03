using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour
{
    [Header("Animation")]
    public float duration = 1.0f;       // 전체 재생 시간(초)
    public float risePixels = 60f;      // 화면 픽셀 기준 위로 떠오르는 총 거리
    public float horizontalDrift = 20f; // 좌우로 살짝 흔들릴 최대 픽셀

    private Text text;
    private float elapsed;
    private float driftX;
    private Color baseColor;

    // 추적 대상/오프셋/카메라
    private Transform followTarget;
    private Vector3 worldOffset;
    private Camera cam;

    // 분리(Detach) 모드 관련
    private bool detached = false;
    private float detachElapsed = 0f;
    private float detachDuration = 0.5f;       // 분리 후 마무리 시간(남은 시간 기반으로 계산)
    private Vector3 detachStartScreenPos;      // 분리 시작 화면 좌표
    private float detachStartEase;             // 분리 시점까지 진행된 ease 값 (0~1)
    private float currentAlpha = 1f;           // 분리 시점 알파

    void Awake()
    {
        text = GetComponent<Text>();
        if (!text) Debug.LogWarning("[DamageText] Text 컴포넌트가 없습니다.");
    }

    /// <summary>대상에 고정되는 데미지 텍스트 설정</summary>
    public void Setup(int damage, Color color, Transform target, Vector3 followWorldOffset, Camera cameraIfNullUseMain = null)
    {
        if (!text) return;

        text.text = damage.ToString();
        baseColor = new Color(color.r, color.g, color.b, 1f);
        text.color = baseColor;

        followTarget = target;
        worldOffset = followWorldOffset;
        cam = cameraIfNullUseMain ?? Camera.main;

        driftX = Random.Range(-horizontalDrift, horizontalDrift);

        elapsed = 0f;
        detached = false;
        detachElapsed = 0f;
        currentAlpha = 1f;

        // [중요] 생성 즉시 현재 화면 좌표로 고정 (첫 프레임 전에 분리돼도 시작 위치가 정확)
        if (followTarget && cam != null)
        {
            Vector3 baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);
            transform.position = baseScreen;
        }
    }


    void Update()
    {
        if (!text) return;

        // 대상이 사라지거나 비활성화되면 분리 모드로 전환
        if (!detached && (followTarget == null || !followTarget.gameObject.activeInHierarchy))
        {
            EnterDetachMode();
        }

        if (!detached)
        {
            // ===== 추적 모드 =====
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 2f); // ease-out

            Vector3 baseScreen = transform.position; // fallback
            if (followTarget && cam != null)
                baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);

            float x = baseScreen.x + Mathf.Sin(t * Mathf.PI) * driftX * 0.3f;
            float y = baseScreen.y + Mathf.Lerp(0f, risePixels, ease);
            transform.position = new Vector3(x, y, 0f);

            currentAlpha = 1f - t;
            var c = baseColor; c.a = currentAlpha;
            text.color = c;

            if (elapsed >= duration)
                Destroy(gameObject);
        }
        else
        {
            // ===== 분리 모드 =====
            detachElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(detachElapsed / detachDuration);
            float ease = 1f - Mathf.Pow(1f - t, 2f);

            // 분리 전까지 올라간 만큼 제외한 '남은 상승량'만 추가
            float remainingRise = risePixels * (1f - detachStartEase);

            // X는 고정, Y만 남은 상승 적용 (튀는 현상 방지)
            float x = detachStartScreenPos.x;
            float y = detachStartScreenPos.y + Mathf.Lerp(0f, remainingRise, ease);
            transform.position = new Vector3(x, y, 0f);

            // 알파도 분리 시점의 currentAlpha에서 0까지 서서히
            var c = baseColor;
            c.a = Mathf.Lerp(currentAlpha, 0f, t);
            text.color = c;

            if (detachElapsed >= detachDuration)
                Destroy(gameObject);
        }
    }

    private void EnterDetachMode()
    {
        detached = true;

        // [중요] 분리 시작 좌표는 "현재 transform.position" 그대로 사용
        // (이미 적용된 상승/드리프트를 다시 더하지 않음)
        detachStartScreenPos = transform.position;

        // 분리 시점까지의 진행도(ease)만 기록해서 남은 상승량 계산에만 활용
        float tSoFar = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
        detachStartEase = 1f - Mathf.Pow(1f - tSoFar, 2f);

        // 남은 시간(너무 짧으면 최소 보장)
        float remainingTime = Mathf.Max(0f, duration - elapsed);
        detachDuration = Mathf.Max(remainingTime, 0.2f);

        // 이후엔 대상 추적 중단
        followTarget = null;
    }
}

