using System.Collections;
using UnityEngine;
using static DamageTextManager;

/// <summary>
/// 단일 대상에게 즉시 피해를 주는 액티브 스킬 구현입니다.
/// </summary>
public class ActiveSkill : ISkill
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public float Cooldown { get; private set; }
    public float MpCost { get; private set; }
    public float Range { get; private set; }
    public float ImpactDelay { get; private set; }

    private float damage;
    private string animationName;

    /// <summary>
    /// 스킬 데이터로 액티브 스킬을 초기화합니다.
    /// </summary>
    public ActiveSkill(SkillData data)
    {
        Id = data.id;
        Name = data.name;
        Cooldown = data.cooldown;
        MpCost = data.mpCost;
        Range = data.range;
        damage = data.damage;
        ImpactDelay = data.impactDelay;
        animationName = data.animation;
    }

    /// <summary>
    /// 지정된 사용자와 능력치로 스킬을 시전합니다.
    /// </summary>
    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        var anim = user.GetComponent<Animation>();
        var attackComp = user.GetComponent<PlayerAttacks>();
        var moveComp = user.GetComponent<PlayerMove>();

        // 현재 타겟이 없으면 마우스 아래 적을 찾아서 설정합니다.
        EnemyStatsManager target = attackComp != null ? attackComp.targetEnemy : null;
        if (target == null || target.CurrentHP <= 0)
        {
            if (attackComp != null && attackComp.TryPickEnemyUnderMouse(out var picked))
            {
                target = picked;
            }
        }

        // 유효한 대상이 없으면 시전을 중단합니다.
        if (target == null || target.CurrentHP <= 0)
        {
            Debug.LogWarning($"{Name} 실패: 유효한 타겟이 없습니다.");
            return false;
        }

        // 사거리 안에 있는지 확인합니다.
        float dist = Vector3.Distance(user.transform.position, target.transform.position);
        if (dist > Range)
        {
            Debug.LogWarning($"{Name} 실패: 사거리({Range}m) 밖입니다. (현재 {dist:F2}m)");
            return false;
        }

        // 마나를 소모하고 부족하면 시전을 중단합니다.
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 실패: MP 부족");
            return false;
        }

        if (attackComp != null)
        {
            attackComp.ForceStopAttack();
            attackComp.isCastingSkill = true;
        }

        // 시전 시작 시 대상 방향으로 회전합니다.
        FaceTargetInstant(user.transform, target.transform.position);

        // 애니메이션을 재생하고 시전 시간을 계산합니다.
        float animDuration = 0.5f;
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);
            AnimationState state = anim[animationName];
            if (state != null)
                animDuration = state.length / Mathf.Max(0.0001f, state.speed);
        }

        if (attackComp != null) attackComp.isAttacking = true;
        if (moveComp != null) moveComp.SetMovementLocked(true);

        // 애니메이션 비율에 맞춰 피해 적용 지연 시간을 계산합니다.
        float impactDelay = animDuration * ImpactDelay;

        user.GetComponent<MonoBehaviour>()
            .StartCoroutine(DealDamageAfterDelay(user.transform, target, stats, impactDelay));

        // 시전이 끝나면 이동과 공격 잠금을 해제합니다.
        user.GetComponent<MonoBehaviour>()
            .StartCoroutine(UnlockAfterDelay(attackComp, moveComp, animDuration));

        return true;
    }

    /// <summary>
    /// 즉시 목표를 바라보도록 회전합니다.
    /// </summary>
    private void FaceTargetInstant(Transform self, Vector3 targetPos)
    {
        Vector3 dir = targetPos - self.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            self.rotation = Quaternion.LookRotation(dir);
    }

    /// <summary>
    /// 지연 후 대상에게 피해를 적용합니다.
    /// </summary>
    private IEnumerator DealDamageAfterDelay(Transform userTf, EnemyStatsManager target, PlayerStatsManager stats, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 임팩트 직전에 다시 대상 방향을 향하도록 보정합니다.
        if (target != null) FaceTargetInstant(userTf, target.transform.position);

        // 대상이 살아 있고 사거리 안에 있으면 피해를 가합니다.
        if (target != null && target.CurrentHP > 0)
        {
            float dist = Vector3.Distance(userTf.position, target.transform.position);
            if (dist <= Range)
            {
                bool isCrit;
                float baseDmg = stats.CalculateDamage(out isCrit);
                float finalDamage = baseDmg * damage;

                target.TakeDamage(finalDamage);

                DamageTextManager.Instance.ShowDamage(
                    target.transform,
                    Mathf.RoundToInt(finalDamage),
                    isCrit ? Color.red : Color.white,
                    DamageTextTarget.Enemy
                );

                Debug.Log($"{target.name}에게 {finalDamage} 피해! (ActiveSkill, Crit={isCrit})");
            }
        }
    }

    /// <summary>
    /// 지연 후 공격과 이동 잠금을 해제합니다.
    /// </summary>
    private IEnumerator UnlockAfterDelay(PlayerAttacks attack, PlayerMove move, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attack != null)
        {
            attack.isCastingSkill = false;
            attack.isAttacking = false;
            if (attack.targetEnemy != null && attack.targetEnemy.CurrentHP > 0)
                attack.ChangeState(new AttackingStates());
            else
                attack.ChangeState(new IdleStates());
        }

        if (move != null)
            move.SetMovementLocked(false);
    }
}
