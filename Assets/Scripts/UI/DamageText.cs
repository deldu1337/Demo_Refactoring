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

    private void Awake()
    {
        text = GetComponent<Text>();
        if (!text) Debug.LogWarning("[DamageText] Text component is missing.");
        poolManager = ObjectPoolManager.GetOrCreate();
    }

    public void OnSpawnedFromPool()
    {
        if (!text) text = GetComponent<Text>();

        elapsed = 0f;
        detachElapsed = 0f;
        detachDuration = 0.5f;
        detached = false;
        currentAlpha = 1f;
        followTarget = null;
        worldOffset = Vector3.zero;
        cam = null;
    }

    public void OnReturnedToPool()
    {
        followTarget = null;
        cam = null;
    }

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

        if (followTarget && cam != null)
        {
            Vector3 baseScreen = cam.WorldToScreenPoint(followTarget.position + worldOffset);
            transform.position = baseScreen;
        }
    }

    private void Update()
    {
        if (!text) return;

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
                Release();
        }
    }

    private void EnterDetachMode()
    {
        detached = true;
        detachStartScreenPos = transform.position;

        float tSoFar = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
        detachStartEase = 1f - Mathf.Pow(1f - tSoFar, 2f);

        float remainingTime = Mathf.Max(0f, duration - elapsed);
        detachDuration = Mathf.Max(remainingTime, 0.2f);

        followTarget = null;
    }

    private void Release()
    {
        if (poolManager == null) poolManager = ObjectPoolManager.GetOrCreate();
        poolManager.Release(gameObject);
    }
}
