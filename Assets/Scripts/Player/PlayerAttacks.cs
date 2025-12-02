using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static DamageTextManager;

/// <summary>
/// 플레이어 공격 상태에 필요한 공통 메서드를 정의합니다.
/// </summary>
public interface IPlayerStates
{
    /// <summary>상태 진입 시 호출되어 초기화를 진행합니다.</summary>
    void Enter(PlayerAttacks player);
    /// <summary>상태 종료 시 호출되어 정리를 진행합니다.</summary>
    void Exit(PlayerAttacks player);
    /// <summary>프레임마다 상태 로직을 업데이트합니다.</summary>
    void Update(PlayerAttacks player);
}

/// <summary>
/// 기본 대기 상태를 담당합니다.
/// </summary>
public class IdleStates : IPlayerStates
{
    /// <summary>대기 상태로 진입하면서 서 있는 애니메이션을 재생합니다.</summary>
    public void Enter(PlayerAttacks player)
    {
        if (player.animationComponent != null)
            player.animationComponent.CrossFade("Stand (ID 0 variation 0)", 0.2f);
    }

    /// <summary>대기 상태 종료 시 특별한 처리는 없습니다.</summary>
    public void Exit(PlayerAttacks player) { }

    /// <summary>우클릭으로 적을 선택하면 공격 상태로 전환합니다.</summary>
    public void Update(PlayerAttacks player)
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (player.TryPickEnemyUnderMouse(out var clickedEnemy))
            {
                player.SetTarget(clickedEnemy);
                player.ChangeState(new AttackingStates());
            }
        }
    }
}

/// <summary>
/// 공격 상태를 담당하며 목표를 추적하고 공격합니다.
/// </summary>
public class AttackingStates : IPlayerStates
{
    /// <summary>공격 상태에 진입하며 마지막 공격 시간을 현재 시각으로 맞춥니다.</summary>
    public void Enter(PlayerAttacks player)
    {
        player.lastAttackTime = Mathf.Max(player.lastAttackTime, Time.time);
    }

    /// <summary>공격 상태 종료 시 특별한 처리는 없습니다.</summary>
    public void Exit(PlayerAttacks player) { }

    /// <summary>적을 향해 회전하고 사거리 내에서 공격을 수행합니다.</summary>
    public void Update(PlayerAttacks player)
    {
        if (player.isCastingSkill) return; // 스킬 시전 중이면 공격 로직을 멈춰 드립니다.

        bool targetDead = player.targetEnemy == null || player.targetEnemy.CurrentHP <= 0;

        if (!targetDead)
        {
            player.RotateTowardsTarget(player.targetEnemy.transform.position);

            if (Time.time >= player.lastAttackTime)
            {
                Collider enemyCollider = player.targetEnemy.GetComponent<Collider>();
                Vector3 playerOrigin = player.transform.position + Vector3.up * player.raycastYOffset;
                Vector3 closest = enemyCollider.ClosestPoint(playerOrigin);
                float distance = Vector3.Distance(playerOrigin, closest);

                if (distance <= player.GetAttackRange())
                {
                    player.PerformAttack();
                    player.lastAttackTime = Time.time + player.GetAttackCooldown();
                }
            }
        }

        // 우클릭 시 다른 적으로 교체하거나 타깃을 해제합니다.
        if (Input.GetMouseButtonDown(1))
        {
            if (player.TryPickEnemyUnderMouse(out var clickedEnemy))
            {
                // 다른 적을 선택하셨다면 타깃을 교체합니다.
                if (clickedEnemy != player.targetEnemy)
                    player.SetTarget(clickedEnemy);
            }
            else
            {
                // 적이 아닌 곳을 누르시면 타깃을 해제합니다.
                player.ClearTarget();
                player.ChangeState(new IdleStates());
            }
        }

        if (targetDead && !player.isAttacking)
        {
            player.ClearTarget();
            player.ChangeState(new IdleStates());
        }
    }
}

/// <summary>
/// 플레이어의 공격과 대상 선택을 관리합니다.
/// </summary>
public class PlayerAttacks : MonoBehaviour
{
    [Header("공격 설정")]
    public float raycastYOffset = 1f;
    public LayerMask enemyLayer;

    [Header("쿨타임")]
    [HideInInspector] public float lastAttackTime;

    [HideInInspector] public EnemyStatsManager targetEnemy;
    [HideInInspector] public HealthBarUI targetHealthBar;
    [HideInInspector] public Animation animationComponent;

    private IPlayerStates currentState;
    private PlayerStatsManager stats;

    [HideInInspector] public bool isAttacking = false; // 공격 중 여부입니다.
    [HideInInspector] public bool isCastingSkill = false;

    /// <summary>
    /// 애니메이션과 스탯 컴포넌트를 준비합니다.
    /// </summary>
    void Awake()
    {
        animationComponent = GetComponent<Animation>();
        stats = PlayerStatsManager.Instance;

        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트를 찾지 못했습니다.");

        if (stats == null)
            Debug.LogError("PlayerCombatStats 컴포넌트를 찾지 못했습니다.");
    }

    /// <summary>
    /// 초기 상태를 대기 상태로 설정합니다.
    /// </summary>
    void Start()
    {
        ChangeState(new IdleStates());
    }

    /// <summary>
    /// 현재 상태의 업데이트 로직을 실행합니다.
    /// </summary>
    void Update()
    {
        currentState?.Update(this);
    }

    /// <summary>
    /// 새 상태로 전환하며 기존 상태를 정리합니다.
    /// </summary>
    public void ChangeState(IPlayerStates newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    /// <summary>
    /// 공격 대상과 체력 UI를 설정합니다.
    /// </summary>
    public void SetTarget(EnemyStatsManager enemy)
    {
        targetEnemy = enemy;
        targetHealthBar = enemy?.GetComponentInChildren<HealthBarUI>();
    }

    /// <summary>
    /// 현재 공격 대상을 해제합니다.
    /// </summary>
    public void ClearTarget()
    {
        targetEnemy = null;
        targetHealthBar = null;
    }

    /// <summary>
    /// 지정한 위치를 향하도록 부드럽게 회전합니다.
    /// </summary>
    public void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    /// <summary>
    /// 적과의 거리를 측정합니다.
    /// </summary>
    public float DistanceTo(EnemyStatsManager enemy)
    {
        if (enemy == null) return float.MaxValue;
        var col = enemy.GetComponent<Collider>();
        Vector3 origin = transform.position + Vector3.up * raycastYOffset;
        Vector3 closest = col != null ? col.ClosestPoint(origin) : enemy.transform.position;
        return Vector3.Distance(origin, closest);
    }

    /// <summary>
    /// 지정한 적이 사거리 안에 있는지 확인합니다.
    /// </summary>
    public bool IsInAttackRange(EnemyStatsManager enemy)
    {
        return DistanceTo(enemy) <= GetAttackRange();
    }

    /// <summary>
    /// 마우스 포인터 아래의 적을 찾고 선택합니다.
    /// </summary>
    public bool TryPickEnemyUnderMouse(out EnemyStatsManager enemy)
    {
        enemy = null;

        // UI 위를 클릭하셨다면 무시합니다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = enemyLayer;

        // RaycastAll로 가장 가까운 적을 우선 찾습니다.
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

        // 보조로 SphereCast를 사용해 가까운 적을 보정해 드립니다.
        RaycastHit sh;
        if (Physics.SphereCast(ray, 0.3f, out sh, 100f, mask, QueryTriggerInteraction.Collide))
        {
            var esm = sh.collider.GetComponentInParent<EnemyStatsManager>();
            if (esm != null && esm.CurrentHP > 0)
            {
                enemy = esm;
                return true;
            }
        }

        return enemy != null;
    }

    private static readonly Collider[] _overlapCache = new Collider[16];

    /// <summary>
    /// 공격 애니메이션을 재생하고 데미지 계산을 예약합니다.
    /// </summary>
    public void PerformAttack()
    {
        if (targetEnemy == null) return;

        string animName = "Attack1H (ID 17 variation 0)";
        if (animationComponent.GetClip(animName) != null)
        {
            // 공격을 시작합니다.
            isAttacking = true;

            // 애니메이션 속도를 스탯에 맞춰 적용합니다.
            animationComponent[animName].speed = stats.Data.AttackSpeed;
            animationComponent.Play(animName);

            // 공격 쿨타임을 갱신합니다.
            lastAttackTime = Time.time + GetAttackCooldown();

            // 임팩트 시점에 데미지를 적용합니다.
            float impactTime = 0.2f;
            StartCoroutine(DelayedDamage(impactTime));

            // 애니메이션 종료 후 공격 상태를 해제합니다.
            float animDuration = animationComponent[animName].length / animationComponent[animName].speed;
            StartCoroutine(AttackAnimationEnd(animDuration));
        }
    }

    /// <summary>
    /// 공격 애니메이션 종료 후 공격 상태를 풀어 드립니다.
    /// </summary>
    private IEnumerator AttackAnimationEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    /// <summary>
    /// 지정된 지연 시간 후 실제 데미지를 적용합니다.
    /// </summary>
    private IEnumerator DelayedDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (targetEnemy == null) yield break;

        bool isCrit;
        float damage = stats.CalculateDamage(out isCrit);

        Debug.Log($"Before Attack: {targetEnemy.name} HP={targetEnemy.CurrentHP}");
        targetEnemy.TakeDamage(damage);
        Debug.Log($"After Attack: {targetEnemy.name} HP={targetEnemy.CurrentHP}");

        // 치명타면 빨간색, 평타면 흰색으로 표시해 드립니다.
        var color = isCrit ? Color.red : Color.white;

        DamageTextManager.Instance.ShowDamage(
            targetEnemy.transform,
            Mathf.RoundToInt(damage),
            color,
            DamageTextManager.DamageTextTarget.Enemy
        );

        targetHealthBar?.CheckHp();
    }

    /// <summary>
    /// 모든 공격 관련 동작을 즉시 중단합니다.
    /// </summary>
    public void ForceStopAttack()
    {
        StopAllCoroutines();
        isAttacking = false;
        if (animationComponent != null)
            animationComponent.Stop();
    }

    /// <summary>
    /// 현재 공격 사거리를 반환합니다.
    /// </summary>
    public float GetAttackRange()
    {
        return 1f;
    }

    /// <summary>
    /// 공격 속도를 바탕으로 공격 쿨타임을 계산합니다.
    /// </summary>
    public float GetAttackCooldown()
    {
        return 1f / stats.Data.AttackSpeed;
    }
}
