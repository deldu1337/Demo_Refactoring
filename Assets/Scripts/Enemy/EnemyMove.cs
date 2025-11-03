using UnityEngine;

/// <summary>
/// 적 이동과 플레이어 추적/복귀, 애니메이션 제어
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyMove : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float baseMoveSpeed = 3f;      // 기본 이동 속도
    [SerializeField] private float baseRotationSpeed = 10f; // 기본 회전 속도
    [SerializeField] private float detectRadius = 10f;      // 플레이어 탐지 범위

    public Transform TargetPlayer { get; private set; }     // 추적 대상

    private TileMapGenerator mapGenerator;
    private Rigidbody rb;
    private Animation anim;
    private EnemyStatsManager stats;
    private Vector3 spawnPosition;

    private int playerLayerMask; // Awake에서 초기화할 변수

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        anim = GetComponent<Animation>();
        stats = GetComponent<EnemyStatsManager>();

        if (!anim) Debug.LogError($"{name}: Animation 컴포넌트가 없습니다!");
        if (!stats) Debug.LogError($"{name}: EnemyStatsManager가 없습니다!");

        mapGenerator = FindAnyObjectByType<TileMapGenerator>();
        if (!mapGenerator) Debug.LogWarning($"{name}: TileMapGenerator를 찾지 못했습니다. 방 제약 없이 탐지합니다.");

        spawnPosition = transform.position;

        // 여기서 레이어 마스크 초기화
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");
    }

    private void OnEnable()
    {
        PlayerStatsManager.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerStatsManager.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        // 플레이어가 죽는 즉시 타겟 해제 → 다음 FixedUpdate부터 스폰 복귀
        TargetPlayer = null;

        // 이동 애니/상태를 즉시 전환하고 싶다면(선택)
        // Run/Stand는 MoveTowardsTarget가 처리하므로 여기서는 생략 가능
    }

    /// <summary>적 스폰 위치 설정</summary>
    public void SetSpawnPosition(Vector3 position) => spawnPosition = position;

    private void FixedUpdate()
    {
        DetectPlayer();
        MoveTowardsTarget();
    }

    /// <summary>플레이어 탐지</summary>
    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, playerLayerMask);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var pStats = hit.GetComponent<PlayerStatsManager>();
            if (pStats == null) continue;

            // 죽은 플레이어는 무시
            if (pStats.CurrentHP <= 0f) continue;

            Vector3 playerPos = hit.transform.position;

            // 같은 방 플레이어 제외
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

    /// <summary>타겟 또는 스폰 위치로 이동</summary>
    private void MoveTowardsTarget()
    {
        Vector3 destination = TargetPlayer ? TargetPlayer.position : spawnPosition;
        Vector3 direction = (destination - rb.position);
        direction.y = 0f;

        float distance = direction.magnitude;
        float moveSpeed = baseMoveSpeed + stats.Data.dex;                 // 민첩 기반 이동속도
        float rotationSpeed = baseRotationSpeed + stats.Data.dex * 0.5f;  // 민첩 기반 회전속도

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

    /// <summary>애니메이션 재생 (공격 중이면 이동/대기 애니메이션 덮지 않음)</summary>
    private void PlayAnimation(string animName)
    {
        if (!anim) return;

        // 공격 애니메이션이 재생 중이면 다른 애니메이션 재생 안 함
        if (anim.IsPlaying("AttackUnarmed (ID 16 variation 0)"))
            return;

        if (!anim.IsPlaying(animName))
            anim.CrossFade(animName, 0.2f);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}


