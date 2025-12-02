using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static DamageTextManager;

// 상태 인터페이스
public interface IPlayerStates
{
    void Enter(PlayerAttacks player);
    void Exit(PlayerAttacks player);
    void Update(PlayerAttacks player);
}

// Idle 상태 (대기)
public class IdleStates : IPlayerStates
{
    public void Enter(PlayerAttacks player)
    {
        if (player.animationComponent != null)
            player.animationComponent.CrossFade("Stand (ID 0 variation 0)", 0.2f);
    }

    public void Exit(PlayerAttacks player) { }

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

// Attacking 상태
public class AttackingStates : IPlayerStates
{
    public void Enter(PlayerAttacks player)
    {
        player.lastAttackTime = Mathf.Max(player.lastAttackTime, Time.time);
    }

    public void Exit(PlayerAttacks player) { }

    public void Update(PlayerAttacks player)
    {
        if (player.isCastingSkill) return; // 스킬 시전 중이면 공격 로직 중단

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

        // 우클릭: 다른 적으로 전환 또는 해제
        if (Input.GetMouseButtonDown(1))
        {
            if (player.TryPickEnemyUnderMouse(out var clickedEnemy))
            {
                // 다른 적을 눌렀으면 타겟 교체
                if (clickedEnemy != player.targetEnemy)
                    player.SetTarget(clickedEnemy);
            }
            else
            {
                // 적이 아닌 곳을 눌렀다면 타겟 해제
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

// PlayerAttack 클래스
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

    [HideInInspector] public bool isAttacking = false; // 공격 중 여부
    [HideInInspector] public bool isCastingSkill = false;

    void Awake()
    {
        animationComponent = GetComponent<Animation>();
        //stats = GetComponent<PlayerStatsManager>();
        stats = PlayerStatsManager.Instance; // ← 싱글톤

        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트가 Player 프리팹 또는 자식에 없습니다!");

        if (stats == null)
            Debug.LogError("PlayerCombatStats 컴포넌트가 Player에 없습니다!");
    }

    void Start()
    {
        ChangeState(new IdleStates());
    }

    void Update()
    {
        currentState?.Update(this);
    }

    public void ChangeState(IPlayerStates newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void SetTarget(EnemyStatsManager enemy)
    {
        targetEnemy = enemy;
        targetHealthBar = enemy?.GetComponentInChildren<HealthBarUI>();
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
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
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

    // 마우스 아래 적 선택 (근접 보정 포함)
    public bool TryPickEnemyUnderMouse(out EnemyStatsManager enemy)
    {
        enemy = null;

        // UI 위면 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = enemyLayer; // Enemy 레이어만

        // 1) RaycastAll: 가장 가까운 Enemy 히트 선택
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

        // 2) 근접 보정: SphereCast
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

    // PlayerAttacks.cs 내부 교체
    private static readonly Collider[] _overlapCache = new Collider[16];


    public void PerformAttack()
    {
        if (targetEnemy == null) return;

        string animName = "Attack1H (ID 17 variation 0)";
        if (animationComponent.GetClip(animName) != null)
        {
            // 공격 시작
            isAttacking = true;

            // 애니메이션 속도 적용
            animationComponent[animName].speed = stats.Data.AttackSpeed;
            animationComponent.Play(animName);

            // 공격 쿨타임
            lastAttackTime = Time.time + GetAttackCooldown();

            // 애니메이션 임팩트 시점에 데미지 적용
            float impactTime = 0.2f;
            StartCoroutine(DelayedDamage(impactTime));

            // 애니메이션 끝날 때 Idle로 전환
            float animDuration = animationComponent[animName].length / animationComponent[animName].speed;
            StartCoroutine(AttackAnimationEnd(animDuration));
        }
    }

    // 애니메이션 종료 후 처리
    private IEnumerator AttackAnimationEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    // 딜레이 후 데미지 적용
    private IEnumerator DelayedDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (targetEnemy == null) yield break;

        bool isCrit;
        float damage = stats.CalculateDamage(out isCrit);

        Debug.Log($"Before Attack: {targetEnemy.name} HP={targetEnemy.CurrentHP}");
        targetEnemy.TakeDamage(damage);
        Debug.Log($"After Attack: {targetEnemy.name} HP={targetEnemy.CurrentHP}");

        // 치명타면 빨간색, 평타면 흰색
        var color = isCrit ? Color.red : Color.white;

        DamageTextManager.Instance.ShowDamage(
            targetEnemy.transform,
            Mathf.RoundToInt(damage),
            color,
            DamageTextManager.DamageTextTarget.Enemy
        );

        targetHealthBar?.CheckHp();
    }

    // PlayerAttacks.cs
    public void ForceStopAttack()
    {
        StopAllCoroutines();        // 공격 관련 코루틴 중단
        isAttacking = false;
        if (animationComponent != null)
            animationComponent.Stop(); // 현재 공격 모션 중단
    }


    // === PlayerCombatStats에서 가져오는 헬퍼 ===
    public float GetAttackRange()
    {
        return 1f;
    }

    public float GetAttackCooldown()
    {
        return 1f / stats.Data.AttackSpeed; // 공격 속도가 높으면 쿨타임 짧아짐
    }
}