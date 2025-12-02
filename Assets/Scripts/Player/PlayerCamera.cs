using UnityEngine;

/// <summary>
/// 플레이어를 따라다니며 상황에 맞게 카메라 오프셋을 조정합니다.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("Offsets")]
    [Tooltip("기본 카메라 오프셋입니다. 이 값이 현재 위치 기준으로 적용됩니다.")]
    public Vector3 baseOffset = new Vector3(-9f, 16.5f, -9f);

    [Tooltip("시작 시점에 현재 카메라 오프셋을 사용하도록 설정합니다.")]
    public bool useCurrentCameraOffsetOnStart = false;

    [Tooltip("보스에게 가까워질 때 얼마나 멀리 카메라를 뺄지 결정하는 배수입니다.")]
    public float zoomOutMultiplier = 1.7f;

    [Header("Boss Zoom")]
    [Tooltip("보스로 인식할 거리입니다. 이 범위 안에서는 카메라를 멀리합니다.")]
    public float bossTriggerRadius = 40f;

    [Tooltip("카메라 오프셋을 보간할 때 사용할 속도입니다.")]
    public float zoomLerpSpeed = 6f;

    [Tooltip("보스를 판별할 때 사용할 태그입니다.")]
    public string bossTag = "Boss";

    [Header("Rotation (optional)")]
    public bool lockRotation = false;
    public Vector3 lockedEuler = new Vector3(55f, 45f, 0f);

    private Transform camT;
    private Transform nearestBoss;

    private Vector3 currentOffset;

    /// <summary>
    /// 메인 카메라 참조를 준비합니다.
    /// </summary>
    void Awake()
    {
        camT = Camera.main ? Camera.main.transform : null;
        if (!camT) Debug.LogWarning("[PlayerCamera] 메인 카메라를 찾지 못했습니다.");
    }

    /// <summary>
    /// 시작 시 카메라 위치와 회전을 초기화합니다.
    /// </summary>
    void Start()
    {
        if (!camT)
        {
            var main = Camera.main;
            if (main) camT = main.transform;
        }

        if (camT && useCurrentCameraOffsetOnStart)
            baseOffset = camT.position - transform.position;

        currentOffset = baseOffset;

        if (camT) camT.position = transform.position + currentOffset;
        if (camT && lockRotation) camT.rotation = Quaternion.Euler(lockedEuler);
    }

    /// <summary>
    /// 물리 프레임마다 카메라 위치와 회전을 갱신합니다.
    /// </summary>
    void FixedUpdate()
    {
        if (!camT) return;

        UpdateNearestBoss();

        Vector3 targetOffset = baseOffset;
        if (nearestBoss)
        {
            float d = Vector3.Distance(transform.position, nearestBoss.position);
            float t = 1f - Mathf.Clamp01(d / bossTriggerRadius);
            targetOffset = Vector3.Lerp(baseOffset, baseOffset * zoomOutMultiplier, t);
        }

        float k = 1f - Mathf.Exp(-zoomLerpSpeed * Time.deltaTime);
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, k);

        camT.position = transform.position + currentOffset;

        if (lockRotation)
            camT.rotation = Quaternion.Euler(lockedEuler);
    }

    /// <summary>
    /// 가장 가까운 보스를 검색합니다.
    /// </summary>
    private void UpdateNearestBoss()
    {
        GameObject[] bosses = GameObject.FindGameObjectsWithTag(bossTag);
        float best = float.MaxValue;
        Transform bestT = null;

        foreach (var b in bosses)
        {
            float d = Vector3.Distance(transform.position, b.transform.position);
            if (d < best) { best = d; bestT = b.transform; }
        }
        nearestBoss = bestT;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 카메라 반응 반경을 시각화합니다.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bossTriggerRadius);
    }
#endif
}
