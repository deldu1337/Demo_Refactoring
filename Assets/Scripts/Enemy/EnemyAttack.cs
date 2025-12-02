using System.Collections;
using UnityEngine;
using static DamageTextManager;

/// <summary>
/// 적의 공격 동작을 관리하고 애니메이션과 데미지 적용을 조율한다.
/// </summary>
[RequireComponent(typeof(EnemyMove))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : MonoBehaviour
{
    private EnemyStatsManager stats;
    private EnemyMove enemyMove;
    private PlayerStatsManager targetPlayer;
    private Animation anim;
    private float lastAttackTime;

    [Header("전투 설정")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float damageDelay = 0.3f; // 데미지가 적용되기까지의 지연 시간

    // 공격 동작이 진행 중인지 여부
    private bool isAttacking = false;
    private Coroutine attackRoutine;

    /// <summary>
    /// 필수 컴포넌트를 캐싱하고 기본 값을 초기화한다.
    /// </summary>
    private void Awake()
    {
        stats = GetComponent<EnemyStatsManager>();
        enemyMove = GetComponent<EnemyMove>();
        anim = GetComponent<Animation>();

        if (!anim) Debug.LogWarning($"{name}: Animation 컴포넌트가 없습니다!");
    }

    /// <summary>
    /// 플레이어 사망 이벤트를 구독한다.
    /// </summary>
    private void OnEnable()
    {
        PlayerStatsManager.OnPlayerDied += InterruptAttackOnPlayerDeath;
    }

    /// <summary>
    /// 플레이어 사망 이벤트 구독을 해제한다.
    /// </summary>
    private void OnDisable()
    {
        PlayerStatsManager.OnPlayerDied -= InterruptAttackOnPlayerDeath;
    }

    /// <summary>
    /// 플레이어가 사망했을 때 공격을 중단하고 상태를 초기화한다.
    /// </summary>
    private void InterruptAttackOnPlayerDeath()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (anim && anim.IsPlaying("AttackUnarmed (ID 16 variation 0)"))
            anim.Stop();

        isAttacking = false;
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// 매 프레임 타겟을 갱신하고 공격을 시도한다.
    /// </summary>
    private void Update()
    {
        UpdateTarget();
        TryAttack();
    }

    /// <summary>
    /// EnemyMove에서 찾은 플레이어를 타겟으로 설정한다.
    /// </summary>
    private void UpdateTarget()
    {
        if (enemyMove?.TargetPlayer == null)
        {
            targetPlayer = null;
            return;
        }

        if (targetPlayer == null || targetPlayer.gameObject != enemyMove.TargetPlayer)
            targetPlayer = enemyMove.TargetPlayer.GetComponent<PlayerStatsManager>();
    }

    /// <summary>
    /// 공격 가능 여부를 확인하고 코루틴을 시작한다.
    /// </summary>
    private void TryAttack()
    {
        if (isAttacking) return;
        if (!CanAttackTarget()) return;

        lastAttackTime = Time.time + GetAttackCooldown();
        attackRoutine = StartCoroutine(AttackSequence());
    }

    /// <summary>
    /// 타겟의 상태와 거리를 검사해 공격 가능 여부를 반환한다.
    /// </summary>
    private bool CanAttackTarget()
    {
        if (!targetPlayer || targetPlayer.Data.CurrentHP <= 0)
            return false;

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        return distance <= attackRange && Time.time >= lastAttackTime;
    }

    /// <summary>
    /// 공격 애니메이션을 재생하고 데미지를 적용하는 전체 흐름을 처리한다.
    /// </summary>
    private IEnumerator AttackSequence()
    {
        isAttacking = true;

        string attackAnimName = "AttackUnarmed (ID 16 variation 0)";
        float speed = Mathf.Max(stats.Data.As, 0.1f);

        // 애니메이션을 재생하면서 속도와 반복 방식을 설정한다.
        if (anim && anim.GetClip(attackAnimName))
        {
            anim.Stop();
            anim[attackAnimName].speed = speed;
            anim[attackAnimName].wrapMode = WrapMode.Once;
            anim.Play(attackAnimName);
        }
        else
        {
            Debug.LogWarning($"{name}: {attackAnimName} 클립을 찾을 수 없습니다!");
        }

        // 데미지 타이밍까지 기다리는 동안 범위와 타겟 유효성을 검사한다.
        float impactWait = damageDelay / speed;
        float t = 0f;
        while (t < impactWait)
        {
            if (OutOfRangeOrInvalid())
            {
                StopAttackAnimIfPlaying(attackAnimName);
                isAttacking = false;
                attackRoutine = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // 실제 데미지를 적용한다.
        if (!OutOfRangeOrInvalid())
        {
            float damage = Mathf.Max(stats.Data.atk - targetPlayer.Data.Def, 1f);
            targetPlayer.TakeDamage(damage);

            DamageTextManager.Instance.ShowDamage(
                targetPlayer.transform,
                Mathf.RoundToInt(damage),
                new Color(1f, 0.5f, 0f),
                DamageTextManager.DamageTextTarget.Player);
        }

        // 남은 애니메이션이 끝날 때까지 상태를 유지하며 확인한다.
        float totalDur = GetAnimDuration(attackAnimName, speed);
        float remain = Mathf.Max(0f, totalDur - impactWait);
        t = 0f;
        while (t < remain)
        {
            if (OutOfRangeOrInvalid())
            {
                StopAttackAnimIfPlaying(attackAnimName);
                isAttacking = false;
                attackRoutine = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        attackRoutine = null;
    }

    /// <summary>
    /// 타겟이 사라졌거나 범위를 벗어났는지 확인한다.
    /// </summary>
    private bool OutOfRangeOrInvalid()
    {
        return targetPlayer == null ||
               targetPlayer.Data.CurrentHP <= 0 ||
               Vector3.Distance(transform.position, targetPlayer.transform.position) > attackRange;
    }

    /// <summary>
    /// 공격 애니메이션이 재생 중이면 중지한다.
    /// </summary>
    private void StopAttackAnimIfPlaying(string attackAnimName)
    {
        if (anim && anim.IsPlaying(attackAnimName))
            anim.Stop();
    }

    /// <summary>
    /// 공격 속도를 기반으로 한 쿨다운 시간을 계산한다.
    /// </summary>
    private float GetAttackCooldown()
    {
        return 1f / Mathf.Max(stats.Data.As, 0.1f);
    }

    /// <summary>
    /// 애니메이션 길이와 재생 속도로 전체 재생 시간을 계산한다.
    /// </summary>
    private float GetAnimDuration(string clipName, float speed)
    {
        if (anim && anim.GetClip(clipName))
        {
            var st = anim[clipName];
            return st.length / Mathf.Max(speed, 0.0001f);
        }
        return 0.5f; // 기본값
    }
}
