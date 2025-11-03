using System.Collections;
using UnityEngine;
using static DamageTextManager;

[RequireComponent(typeof(EnemyMove))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : MonoBehaviour
{
    private EnemyStatsManager stats;
    private EnemyMove enemyMove;
    private PlayerStatsManager targetPlayer;
    private Animation anim;
    private float lastAttackTime;

    [Header("공격 설정")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float damageDelay = 0.3f; // 타격 타이밍(초)

    // 추가: 상태/코루틴 핸들
    private bool isAttacking = false;
    private Coroutine attackRoutine;

    private void Awake()
    {
        stats = GetComponent<EnemyStatsManager>();
        enemyMove = GetComponent<EnemyMove>();
        anim = GetComponent<Animation>();

        if (!anim) Debug.LogWarning($"{name}: Animation 컴포넌트가 없습니다!");
    }

    private void OnEnable()
    {
        PlayerStatsManager.OnPlayerDied += InterruptAttackOnPlayerDeath;
    }

    private void OnDisable()
    {
        PlayerStatsManager.OnPlayerDied -= InterruptAttackOnPlayerDeath;
    }

    private void InterruptAttackOnPlayerDeath()
    {
        // 공격 중이면 즉시 중단
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        // 공격 애니 중이면 멈춤
        if (anim && anim.IsPlaying("AttackUnarmed (ID 16 variation 0)"))
            anim.Stop();

        isAttacking = false;

        // 다음 공격 딜레이 초기화 (즉시 재개하지 않도록 약간의 쿨을 줄 수도 있음)
        lastAttackTime = Time.time;
    }

    private void Update()
    {
        UpdateTarget();
        TryAttack();
    }

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

    private void TryAttack()
    {
        // 공격 중엔 재시작 금지
        if (isAttacking) return;
        if (!CanAttackTarget()) return;

        lastAttackTime = Time.time + GetAttackCooldown();
        attackRoutine = StartCoroutine(AttackSequence());
    }

    private bool CanAttackTarget()
    {
        if (!targetPlayer || targetPlayer.Data.CurrentHP <= 0)
            return false;

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        return distance <= attackRange && Time.time >= lastAttackTime;
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;

        string attackAnimName = "AttackUnarmed (ID 16 variation 0)";
        float speed = Mathf.Max(stats.Data.As, 0.1f);

        // 공격 애니메이션 재생
        if (anim && anim.GetClip(attackAnimName))
        {
            anim.Stop();
            anim[attackAnimName].speed = speed;
            anim[attackAnimName].wrapMode = WrapMode.Once;
            anim.Play(attackAnimName);
            // Debug.Log($"{name} : {attackAnimName} 재생");
        }
        else
        {
            Debug.LogWarning($"{name}: {attackAnimName} 클립을 찾을 수 없음!");
        }

        // 임팩트까지 매 프레임 거리/유효성 체크
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

        // 타격: 여전히 범위 안/유효할 때만
        if (!OutOfRangeOrInvalid())
        {
            float damage = Mathf.Max(stats.Data.atk - targetPlayer.Data.Def, 1f);
            targetPlayer.TakeDamage(damage);

            // 플레이어 피격 텍스트
            DamageTextManager.Instance.ShowDamage(
                targetPlayer.transform,
                Mathf.RoundToInt(damage),
                new Color(1f, 0.5f, 0f),
                DamageTextManager.DamageTextTarget.Player);
        }

        // 남은 모션 동안도 이탈 즉시 중단
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

        // 종료
        isAttacking = false;
        attackRoutine = null;
    }

    private bool OutOfRangeOrInvalid()
    {
        return targetPlayer == null ||
               targetPlayer.Data.CurrentHP <= 0 ||
               Vector3.Distance(transform.position, targetPlayer.transform.position) > attackRange;
    }

    private void StopAttackAnimIfPlaying(string attackAnimName)
    {
        if (anim && anim.IsPlaying(attackAnimName))
            anim.Stop();
        // EnemyMove는 공격 애니가 아닐 때만 Run/Stand를 재생하므로,
        // 여기서 Stop()하면 다음 FixedUpdate에서 자동으로 Run/Stand로 전환됩니다.
    }

    private float GetAttackCooldown()
    {
        return 1f / Mathf.Max(stats.Data.As, 0.1f);
    }

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
