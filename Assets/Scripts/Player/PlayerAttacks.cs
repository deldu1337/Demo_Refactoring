using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class IdleStates : IState<PlayerAttacks>
{
    public void Enter(PlayerAttacks player)
    {
        if (player.animationComponent != null)
            player.animationComponent.CrossFade(PlayerAttacks.IdleAnimation, 0.2f);
    }

    public void Tick(PlayerAttacks player)
    {
        if (Input.GetMouseButtonDown(1) && player.TryPickEnemyUnderMouse(out var clickedEnemy))
        {
            player.SetTarget(clickedEnemy);
            player.ChangeState(player.IsInAttackRange(clickedEnemy) ? new AttackingStates() : new MovingStates());
        }
    }

    public void FixedTick(PlayerAttacks player) { }
    public void Exit(PlayerAttacks player) { }
}

public class MovingStates : IState<PlayerAttacks>
{
    public void Enter(PlayerAttacks player) { }

    public void Tick(PlayerAttacks player)
    {
        if (player.isCastingSkill) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (player.TryPickEnemyUnderMouse(out var clickedEnemy))
            {
                player.SetTarget(clickedEnemy);
            }
            else
            {
                player.ClearTarget();
                player.ChangeState(player.IsMoving() ? new MovingStates() : new IdleStates());
            }
        }

        if (player.targetEnemy != null && player.targetEnemy.CurrentHP > 0 && player.IsInAttackRange(player.targetEnemy))
            player.ChangeState(new AttackingStates());
    }

    public void FixedTick(PlayerAttacks player) { }
    public void Exit(PlayerAttacks player) { }
}

public class AttackingStates : IState<PlayerAttacks>
{
    public void Enter(PlayerAttacks player)
    {
        player.CancelMovementForCombat();
        player.lastAttackTime = Mathf.Max(player.lastAttackTime, Time.time);
    }

    public void Tick(PlayerAttacks player)
    {
        if (player.isCastingSkill) return;

        bool targetDead = player.targetEnemy == null || player.targetEnemy.CurrentHP <= 0;

        if (!targetDead)
        {
            if (!player.IsInAttackRange(player.targetEnemy))
            {
                player.ChangeState(new MovingStates());
                return;
            }

            player.RotateTowardsTarget(player.targetEnemy.transform.position);

            if (Time.time >= player.lastAttackTime)
            {
                player.PerformAttack();
                player.lastAttackTime = Time.time + player.GetAttackCooldown();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (player.TryPickEnemyUnderMouse(out var clickedEnemy))
            {
                if (clickedEnemy != player.targetEnemy)
                    player.SetTarget(clickedEnemy);
            }
            else
            {
                player.ClearTarget();
                player.ChangeState(player.IsMoving() ? new MovingStates() : new IdleStates());
            }
        }

        if (targetDead && !player.isAttacking)
        {
            player.ClearTarget();
            player.ChangeState(new IdleStates());
        }
    }

    public void FixedTick(PlayerAttacks player) { }
    public void Exit(PlayerAttacks player) { }
}

public class PlayerAttacks : MonoBehaviour
{
    public const string IdleAnimation = "Stand (ID 0 variation 0)";
    public const string AttackAnimation = "Attack1H (ID 17 variation 0)";

    [Header("Attack Settings")]
    public float raycastYOffset = 1f;
    public LayerMask enemyLayer;

    [HideInInspector] public float lastAttackTime;
    [HideInInspector] public EnemyStatsManager targetEnemy;
    [HideInInspector] public HealthBarUI targetHealthBar;
    [HideInInspector] public Animation animationComponent;
    [HideInInspector] public bool isAttacking;
    [HideInInspector] public bool isCastingSkill;

    private StateMachine<PlayerAttacks> stateMachine;
    private PlayerStatsManager stats;
    private PlayerMove mover;

    private void Awake()
    {
        animationComponent = GetComponent<Animation>();
        stats = PlayerStatsManager.Instance;
        mover = GetComponent<PlayerMove>();
        stateMachine = new StateMachine<PlayerAttacks>(this);

        if (animationComponent == null)
            Debug.LogError("Animation component not found.");

        if (stats == null)
            Debug.LogError("PlayerStatsManager not found.");
    }

    private void Start()
    {
        ChangeState(new IdleStates());
    }

    private void Update()
    {
        stateMachine.Tick();
    }

    private void FixedUpdate()
    {
        stateMachine.FixedTick();
    }

    public void ChangeState(IState<PlayerAttacks> newState)
    {
        stateMachine.ChangeState(newState);
    }

    public void SetTarget(EnemyStatsManager enemy)
    {
        targetEnemy = enemy;
        targetHealthBar = enemy ? enemy.GetComponentInChildren<HealthBarUI>() : null;
    }

    public void ClearTarget()
    {
        targetEnemy = null;
        targetHealthBar = null;
    }

    public void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    public float DistanceTo(EnemyStatsManager enemy)
    {
        if (enemy == null) return float.MaxValue;

        var col = enemy.GetComponent<Collider>();
        Vector3 origin = transform.position + Vector3.up * raycastYOffset;
        Vector3 closest = col != null ? col.ClosestPoint(origin) : enemy.transform.position;
        return Vector3.Distance(origin, closest);
    }

    public bool IsInAttackRange(EnemyStatsManager enemy)
    {
        return DistanceTo(enemy) <= GetAttackRange();
    }

    public bool TryPickEnemyUnderMouse(out EnemyStatsManager enemy)
    {
        enemy = null;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = enemyLayer;

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, mask, QueryTriggerInteraction.Collide);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var esm = h.collider.GetComponentInParent<EnemyStatsManager>();
                if (esm != null && esm.CurrentHP > 0)
                {
                    enemy = esm;
                    return true;
                }
            }
        }

        if (Physics.SphereCast(ray, 0.3f, out RaycastHit sh, 100f, mask, QueryTriggerInteraction.Collide))
        {
            var esm = sh.collider.GetComponentInParent<EnemyStatsManager>();
            if (esm != null && esm.CurrentHP > 0)
            {
                enemy = esm;
                return true;
            }
        }

        return false;
    }

    public void CancelMovementForCombat()
    {
        mover?.CancelMovementForCombat();
    }

    public bool IsMoving()
    {
        return mover != null && mover.IsMoving();
    }

    public void PerformAttack()
    {
        if (targetEnemy == null || stats == null || animationComponent == null)
            return;

        if (animationComponent.GetClip(AttackAnimation) == null)
            return;

        isAttacking = true;
        animationComponent[AttackAnimation].speed = stats.Data.AttackSpeed;
        animationComponent.Play(AttackAnimation);

        float impactTime = 0.2f;
        StartCoroutine(DelayedDamage(impactTime));

        float animDuration = animationComponent[AttackAnimation].length / animationComponent[AttackAnimation].speed;
        StartCoroutine(AttackAnimationEnd(animDuration));
    }

    private IEnumerator AttackAnimationEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    private IEnumerator DelayedDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (targetEnemy == null || stats == null)
            yield break;

        bool isCrit;
        float damage = stats.CalculateDamage(out isCrit);

        targetEnemy.TakeDamage(damage);

        DamageTextManager.Instance.ShowDamage(
            targetEnemy.transform,
            Mathf.RoundToInt(damage),
            isCrit ? Color.red : Color.white,
            DamageTextManager.DamageTextTarget.Enemy
        );

        targetHealthBar?.CheckHp();
    }

    public void ForceStopAttack()
    {
        StopAllCoroutines();
        isAttacking = false;
        if (animationComponent != null)
            animationComponent.Stop();
    }

    public float GetAttackRange()
    {
        return 1f;
    }

    public float GetAttackCooldown()
    {
        if (stats == null || stats.Data == null)
            return 1f;

        return 1f / Mathf.Max(stats.Data.AttackSpeed, 0.1f);
    }
}
