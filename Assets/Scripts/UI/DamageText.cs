using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour
{
    [Header("Animation")]
    public float duration = 1.0f;       // 표시가 유지되는 시간입니다.
    public float risePixels = 60f;      // 화면에서 상승하는 거리입니다.
    public float horizontalDrift = 20f; // 좌우로 흔들리는 정도입니다.

    private Text text;
    private float elapsed;
    private float driftX;
    private Color baseColor;

    // 따라갈 대상과 카메라 관련 정보입니다.
    private Transform followTarget;
    private Vector3 worldOffset;
    private Camera cam;

    // 분리(detach) 애니메이션 상태입니다.
    private bool detached = false;
    private float detachElapsed = 0f;
    private float detachDuration = 0.5f;       // 분리된 뒤 사라지기까지의 시간입니다.
    private Vector3 detachStartScreenPos;      // 분리 시점의 화면 좌표입니다.
    private float detachStartEase;             // 분리 시점의 이징 값입니다.
    private float currentAlpha = 1f;           // 현재 알파 값입니다.

    /// <summary>
    /// 텍스트 컴포넌트를 가져오고 기본 상태를 준비합니다.
    /// </summary>
    void Awake()
    {
        text = GetComponent<Text>();
        if (!text) Debug.LogWarning("[DamageText] Text 컴포넌트가 없습니다.");
    }

    /// <summary>
    /// 데미지 숫자와 색상을 설정하고 이동 대상 정보를 초기화합니다.
    /// </summary>
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

        // 시작 위치를 대상 기준 화면 좌표로 맞춥니다.
        if (followTarget && cam != null)
        {
            Vector3 baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);
            transform.position = baseScreen;
        }
    }

    /// <summary>
    /// 매 프레임마다 텍스트 위치와 알파 값을 갱신합니다.
    /// </summary>
    void Update()
    {
        if (!text) return;

        // 대상이 사라지면 분리 모드로 전환합니다.
        if (!detached && (followTarget == null || !followTarget.gameObject.activeInHierarchy))
        {
            EnterDetachMode();
        }

        if (!detached)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 2f);

            Vector3 baseScreen = transform.position;
            if (followTarget && cam != null)
                baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);

            float x = baseScreen.x + Mathf.Sin(t * Mathf.PI) * driftX * 0.3f;
            float y = baseScreen.y + Mathf.Lerp(0f, risePixels, ease);
            transform.position = new Vector3(x, y, 0f);

            currentAlpha = 1f - t;
            var c = baseColor;
            c.a = currentAlpha;
            text.color = c;

            if (elapsed >= duration)
                Destroy(gameObject);
        }
        else
        {
            detachElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(detachElapsed / detachDuration);
            float ease = 1f - Mathf.Pow(1f - t, 2f);

            float remainingRise = risePixels * (1f - detachStartEase);

            float x = detachStartScreenPos.x;
            float y = detachStartScreenPos.y + Mathf.Lerp(0f, remainingRise, ease);
            transform.position = new Vector3(x, y, 0f);

            var c = baseColor;
            c.a = Mathf.Lerp(currentAlpha, 0f, t);
            text.color = c;

            if (detachElapsed >= detachDuration)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// 따라가는 대상이 사라졌을 때 분리 애니메이션을 시작합니다.
    /// </summary>
    private void EnterDetachMode()
    {
        detached = true;

        // 분리 시점의 화면 좌표를 그대로 저장합니다.
        detachStartScreenPos = transform.position;

        // 진행된 시간으로 분리 시점의 이징 값을 계산합니다.
        float tSoFar = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
        detachStartEase = 1f - Mathf.Pow(1f - tSoFar, 2f);

        // 남은 시간을 고려하여 분리 애니메이션 길이를 정합니다.
        float remainingTime = Mathf.Max(0f, duration - elapsed);
        detachDuration = Mathf.Max(remainingTime, 0.2f);

        // 더 이상 대상을 추적하지 않습니다.
        followTarget = null;
    }
}
