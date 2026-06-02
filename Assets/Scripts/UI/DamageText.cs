using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour, IPoolable
{
    [Header("Animation")]
    public float duration = 1.0f;
    public float risePixels = 60f;
    public float horizontalDrift = 20f;

    private Text text;
    private float elapsed;
    private float driftX;
    private Color baseColor;

    private Transform followTarget;
    private Vector3 worldOffset;
    private Camera cam;
    private ObjectPoolManager poolManager;

    private bool detached;
    private float detachElapsed;
    private float detachDuration = 0.5f;
    private Vector3 detachStartScreenPos;
    private float detachStartEase;
    private float currentAlpha = 1f;

    /// <summary>
    /// 텍스트 컴포넌트와 풀 매니저 참조를 준비합니다.
    /// </summary>
    private void Awake()
    {
        text = GetComponent<Text>();
        if (!text) Debug.LogWarning("[DamageText] Text component is missing.");
        poolManager = ObjectPoolManager.GetOrCreate();
    }

    /// <summary>
    /// 풀에서 재사용될 때 이전 애니메이션 상태와 추적 대상을 초기화합니다.
    /// </summary>
    public void OnSpawnedFromPool()
    {
        if (!text) text = GetComponent<Text>();

        // 이전 사용에서 남은 시간, 분리 상태, 알파 값을 초기 상태로 되돌립니다.
        elapsed = 0f;
        detachElapsed = 0f;
        detachDuration = 0.5f;
        detached = false;
        currentAlpha = 1f;
        followTarget = null;
        worldOffset = Vector3.zero;
        cam = null;
    }

    /// <summary>
    /// 풀로 반환될 때 외부 참조를 정리합니다.
    /// </summary>
    public void OnReturnedToPool()
    {
        followTarget = null;
        cam = null;
    }

    /// <summary>
    /// 데미지 값, 색상, 추적 대상 정보를 설정하고 애니메이션을 시작 상태로 초기화합니다.
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

        // 추적 대상이 있는 경우 대상의 월드 위치를 화면 좌표로 변환해 시작 위치를 맞춥니다.
        if (followTarget && cam != null)
        {
            Vector3 baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);
            transform.position = baseScreen;
        }
    }

    /// <summary>
    /// 데미지 텍스트의 상승, 좌우 흔들림, 투명도 감소를 매 프레임 갱신합니다.
    /// </summary>
    private void Update()
    {
        if (!text) return;

        // 대상이 사라지거나 비활성화되면 현재 화면 위치에서 분리 애니메이션으로 전환합니다.
        if (!detached && (followTarget == null || !followTarget.gameObject.activeInHierarchy))
            EnterDetachMode();

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
                // Destroy하지 않고 풀로 반환해 다음 데미지 표시 때 재사용합니다.
                Release();
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
                // 분리 애니메이션이 끝난 경우에도 동일하게 풀로 반환합니다.
                Release();
        }
    }

    /// <summary>
    /// 추적 대상이 사라졌을 때 현재 위치에서 자연스럽게 사라지도록 분리 상태로 전환합니다.
    /// </summary>
    private void EnterDetachMode()
    {
        detached = true;

        // 대상이 사라진 순간의 화면 좌표를 기준으로 남은 애니메이션을 이어갑니다.
        detachStartScreenPos = transform.position;

        float tSoFar = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
        detachStartEase = 1f - Mathf.Pow(1f - tSoFar, 2f);

        float remainingTime = Mathf.Max(0f, duration - elapsed);
        detachDuration = Mathf.Max(remainingTime, 0.2f);

        followTarget = null;
    }

    /// <summary>
    /// 데미지 텍스트 사용을 끝내고 오브젝트 풀로 반환합니다.
    /// </summary>
    private void Release()
    {
        if (poolManager == null) poolManager = ObjectPoolManager.GetOrCreate();
        poolManager.Release(gameObject);
    }
}
