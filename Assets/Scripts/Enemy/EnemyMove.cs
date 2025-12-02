using UnityEngine;

/// <summary>
/// 적의 이동과 추적 로직을 담당하고 애니메이션을 전환합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyMove : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float baseMoveSpeed = 3f;      // 기본 이동 속도입니다.
    [SerializeField] private float baseRotationSpeed = 10f; // 기본 회전 속도입니다.
    [SerializeField] private float detectRadius = 10f;      // 플레이어 탐지 범위입니다.

    public Transform TargetPlayer { get; private set; }

    private TileMapGenerator mapGenerator;
    private Rigidbody rb;
    private Animation anim;
    private EnemyStatsManager stats;
    private Vector3 spawnPosition;

    private int playerLayerMask;

    /// <summary>
    /// 필수 컴포넌트를 초기화하고 기본 위치와 레이어 마스크를 설정합니다.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        anim = GetComponent<Animation>();
        stats = GetComponent<EnemyStatsManager>();

        if (!anim) Debug.LogError($"{name}: Animation 컴포넌트가 없습니다!");
        if (!stats) Debug.LogError($"{name}: EnemyStatsManager가 없습니다!");

        mapGenerator = FindAnyObjectByType<TileMapGenerator>();
        if (!mapGenerator) Debug.LogWarning($"{name}: TileMapGenerator를 찾지 못했습니다. 기본 추적 범위를 사용합니다.");

        spawnPosition = transform.position;
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");
    }

    /// <summary>
    /// 플레이어 사망 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        PlayerStatsManager.OnPlayerDied += HandlePlayerDied;
    }

    /// <summary>
    /// 플레이어 사망 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        PlayerStatsManager.OnPlayerDied -= HandlePlayerDied;
    }

    /// <summary>
    /// 플레이어가 죽었을 때 추적 상태를 초기화합니다.
    /// </summary>
    private void HandlePlayerDied()
    {
        TargetPlayer = null;
    }

    /// <summary>
    /// 생성 위치를 외부에서 지정합니다.
    /// </summary>
    public void SetSpawnPosition(Vector3 position) => spawnPosition = position;

    /// <summary>
    /// 고정 업데이트마다 플레이어를 탐지하고 이동 동작을 수행합니다.
    /// </summary>
    private void FixedUpdate()
    {
        DetectPlayer();
        MoveTowardsTarget();
    }

    /// <summary>
    /// 주변에서 살아있는 플레이어를 탐지하고 가장 가까운 대상을 찾습니다.
    /// </summary>
    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, playerLayerMask);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var pStats = hit.GetComponent<PlayerStatsManager>();
            if (pStats == null) continue;

            if (pStats.CurrentHP <= 0f) continue;

            Vector3 playerPos = hit.transform.position;

            if (mapGenerator && mapGenerator.GetPlayerRoom().Contains(
                new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.z))))
                continue;

            float dist = Vector3.Distance(transform.position, playerPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        TargetPlayer = closest;
    }

    /// <summary>
    /// 목표 위치를 향해 이동하고 상황에 맞는 애니메이션을 재생합니다.
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector3 destination = TargetPlayer ? TargetPlayer.position : spawnPosition;
        Vector3 direction = (destination - rb.position);
        direction.y = 0f;

        float distance = direction.magnitude;
        float moveSpeed = baseMoveSpeed + stats.Data.dex;
        float rotationSpeed = baseRotationSpeed + stats.Data.dex * 0.5f;

        if (distance > 1f)
        {
            Vector3 moveDir = direction.normalized;
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));

            PlayAnimation("Run (ID 5 variation 0)");
        }
        else
        {
            PlayAnimation("Stand (ID 0 variation 0)");
        }
    }

    /// <summary>
    /// 이동 상태에 따라 적절한 애니메이션을 재생합니다.
    /// </summary>
    private void PlayAnimation(string animName)
    {
        if (!anim) return;

        // 공격 동작 중에는 이동 애니메이션을 덮어쓰지 않습니다.
        if (anim.IsPlaying("AttackUnarmed (ID 16 variation 0)"))
            return;

        if (!anim.IsPlaying(animName))
            anim.CrossFade(animName, 0.2f);
    }

    /// <summary>
    /// 에디터에서 탐지 반경을 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
