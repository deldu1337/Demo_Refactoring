using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyMove : MonoBehaviour
{
    public const string IdleAnimation = "Stand (ID 0 variation 0)";
    public const string RunAnimation = "Run (ID 5 variation 0)";
    public const string AttackAnimation = "AttackUnarmed (ID 16 variation 0)";

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private float baseRotationSpeed = 10f;
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float attackStopDistance = 2f;

    public Transform TargetPlayer { get; private set; }
    public Vector3 SpawnPosition => spawnPosition;

    private StateMachine<EnemyMove> stateMachine;
    private TileMapGenerator mapGenerator;
    private Rigidbody rb;
    private Animation anim;
    private EnemyStatsManager stats;
    private Vector3 spawnPosition;
    private int playerLayerMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        anim = GetComponent<Animation>();
        stats = GetComponent<EnemyStatsManager>();
        stateMachine = new StateMachine<EnemyMove>(this);

        if (!anim) Debug.LogError($"{name}: Animation component not found.");
        if (!stats) Debug.LogError($"{name}: EnemyStatsManager not found.");

        mapGenerator = FindAnyObjectByType<TileMapGenerator>();
        if (!mapGenerator)
            Debug.LogWarning($"{name}: TileMapGenerator not found. Enemy will use simple target detection.");

        spawnPosition = transform.position;
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");
    }

    private void Start()
    {
        ChangeState(new EnemyIdleState());
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
        TargetPlayer = null;
        ChangeState(new EnemyIdleState());
    }

    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
    }

    private void FixedUpdate()
    {
        stateMachine.FixedTick();
    }

    public void ChangeState(IState<EnemyMove> newState)
    {
        stateMachine.ChangeState(newState);
    }

    public void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, playerLayerMask);
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var pStats = hit.GetComponent<PlayerStatsManager>();
            if (pStats == null || pStats.CurrentHP <= 0f)
                continue;

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

    public bool HasLiveTarget()
    {
        if (!TargetPlayer)
            return false;

        var targetStats = TargetPlayer.GetComponent<PlayerStatsManager>();
        return targetStats != null && targetStats.CurrentHP > 0f;
    }

    public bool IsTargetInAttackRange()
    {
        if (!HasLiveTarget())
            return false;

        return Vector3.Distance(transform.position, TargetPlayer.position) <= attackStopDistance;
    }

    public bool IsAtSpawn()
    {
        Vector3 delta = spawnPosition - rb.position;
        delta.y = 0f;
        return delta.magnitude <= 1f;
    }

    public void MoveToTargetOrSpawn()
    {
        Vector3 destination = HasLiveTarget() ? TargetPlayer.position : spawnPosition;
        MoveTowards(destination);
    }

    public void MoveTowards(Vector3 destination)
    {
        Vector3 direction = destination - rb.position;
        direction.y = 0f;

        float distance = direction.magnitude;
        if (distance <= 1f)
        {
            PlayAnimation(IdleAnimation);
            return;
        }

        float moveSpeed = baseMoveSpeed + stats.Data.dex;
        float rotationSpeed = baseRotationSpeed + stats.Data.dex * 0.5f;
        Vector3 moveDir = direction.normalized;

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.fixedDeltaTime));

        PlayAnimation(RunAnimation);
    }

    public void FaceTarget()
    {
        if (!HasLiveTarget())
            return;

        Vector3 direction = TargetPlayer.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, baseRotationSpeed * Time.fixedDeltaTime));
    }

    public void PlayAnimation(string animName)
    {
        if (!anim)
            return;

        if (anim.IsPlaying(AttackAnimation))
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

public sealed class EnemyIdleState : IState<EnemyMove>
{
    public void Enter(EnemyMove enemy)
    {
        enemy.PlayAnimation(EnemyMove.IdleAnimation);
    }

    public void Tick(EnemyMove enemy) { }

    public void FixedTick(EnemyMove enemy)
    {
        enemy.DetectPlayer();

        if (enemy.HasLiveTarget())
            enemy.ChangeState(new EnemyMoveState());
        else if (!enemy.IsAtSpawn())
            enemy.ChangeState(new EnemyMoveState());
    }

    public void Exit(EnemyMove enemy) { }
}

public sealed class EnemyMoveState : IState<EnemyMove>
{
    public void Enter(EnemyMove enemy) { }
    public void Tick(EnemyMove enemy) { }

    public void FixedTick(EnemyMove enemy)
    {
        enemy.DetectPlayer();

        if (enemy.IsTargetInAttackRange())
        {
            enemy.ChangeState(new EnemyAttackState());
            return;
        }

        if (!enemy.HasLiveTarget() && enemy.IsAtSpawn())
        {
            enemy.ChangeState(new EnemyIdleState());
            return;
        }

        enemy.MoveToTargetOrSpawn();
    }

    public void Exit(EnemyMove enemy) { }
}

public sealed class EnemyAttackState : IState<EnemyMove>
{
    public void Enter(EnemyMove enemy)
    {
        enemy.PlayAnimation(EnemyMove.IdleAnimation);
    }

    public void Tick(EnemyMove enemy) { }

    public void FixedTick(EnemyMove enemy)
    {
        enemy.DetectPlayer();

        if (!enemy.IsTargetInAttackRange())
        {
            enemy.ChangeState(new EnemyMoveState());
            return;
        }

        enemy.FaceTarget();
    }

    public void Exit(EnemyMove enemy) { }
}
