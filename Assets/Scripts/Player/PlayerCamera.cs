using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Offsets")]
    [Tooltip("기본 카메라 오프셋(플레이어 기준). 시작 시 현재 카메라 오프셋 사용 옵션이 켜져있으면 무시됩니다.")]
    public Vector3 baseOffset = new Vector3(-9f, 16.5f, -9f);

    [Tooltip("게임 시작 시 (현재 카메라 위치 - 플레이어 위치)로 오프셋 자동 설정")]
    public bool useCurrentCameraOffsetOnStart = false;

    [Tooltip("보스 근접시 확대될(멀어질) 오프셋 배수")]
    public float zoomOutMultiplier = 1.7f;

    [Header("Boss Zoom")]
    [Tooltip("보스와 이 거리 이내로 접근하면 줌아웃 시작")]
    public float bossTriggerRadius = 40f;

    [Tooltip("오프셋 전환 속도(초당). 값이 클수록 빠르게 전환")]
    public float zoomLerpSpeed = 6f;

    [Tooltip("보스 탐색에 사용할 태그")]
    public string bossTag = "Boss";

    [Header("Rotation (optional)")]
    public bool lockRotation = false;
    public Vector3 lockedEuler = new Vector3(55f, 45f, 0f);

    private Transform camT;
    private Transform nearestBoss;

    private Vector3 currentOffset; // 카메라가 매 프레임 즉시 사용할 오프셋(지연 없음)

    void Awake()
    {
        camT = Camera.main ? Camera.main.transform : null;
        if (!camT) Debug.LogWarning("[PlayerCamera] Main Camera를 찾지 못했습니다.");
    }

    void Start()
    {
        if (!camT)
        {
            var main = Camera.main;
            if (main) camT = main.transform;
        }

        // 시작 시 기존 스크립트처럼 현재 카메라 기준 오프셋 계산
        if (camT && useCurrentCameraOffsetOnStart)
            baseOffset = camT.position - transform.position;

        currentOffset = baseOffset;

        // 첫 프레임부터 정확히 붙이기
        if (camT) camT.position = transform.position + currentOffset;
        if (camT && lockRotation) camT.rotation = Quaternion.Euler(lockedEuler);
    }

    void FixedUpdate() // 플레이어 이동 후 카메라 덮어쓰기
    {
        if (!camT) return;

        // 1) 가장 가까운 보스 찾기
        UpdateNearestBoss();

        // 2) 목표 오프셋 계산(보스 거리 기반)
        Vector3 targetOffset = baseOffset;
        if (nearestBoss)
        {
            float d = Vector3.Distance(transform.position, nearestBoss.position);
            float t = 1f - Mathf.Clamp01(d / bossTriggerRadius); // 0(멀)~1(가깝)
            targetOffset = Vector3.Lerp(baseOffset, baseOffset * zoomOutMultiplier, t);
        }

        // 3) 오프셋만 부드럽게 변경 (카메라 위치 자체는 즉시 적용)
        //    지수형 보간: 프레임레이트와 무관하게 균일한 전환감
        float k = 1f - Mathf.Exp(-zoomLerpSpeed * Time.deltaTime);
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, k);

        // 4) 카메라 위치를 '즉시' 지정 → 플레이어와 동일하게 움직임(지연 없음)
        camT.position = transform.position + currentOffset;

        if (lockRotation)
            camT.rotation = Quaternion.Euler(lockedEuler);
    }

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
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bossTriggerRadius);
    }
#endif
}

