using System.Collections;
using UnityEngine;
using static DamageTextManager;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 범위 피해를 주는 투사체형 스킬입니다.
/// </summary>
public class ProjectileSkill : ISkill
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
    /// 스킬 데이터로 투사체 스킬을 초기화합니다.
    /// </summary>
    public ProjectileSkill(SkillData data)
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
    /// 시전자 주변에 범위 피해를 발생시킵니다.
    /// </summary>
    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 실패: MP가 부족합니다.");
            return false;
        }

        Animation anim = user.GetComponent<Animation>();
        PlayerAttacks attackComp = user.GetComponent<PlayerAttacks>();
        PlayerMove moveComp = user.GetComponent<PlayerMove>();

        if (attackComp != null)
        {
            attackComp.ForceStopAttack();
            attackComp.isCastingSkill = true;
        }

        float animDuration = 0.5f;
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);
            AnimationState state = anim[animationName];
            if (state != null)
                animDuration = state.length / Mathf.Max(state.speed, 0.0001f);
        }

        if (attackComp != null) attackComp.isAttacking = true;
        if (moveComp != null) moveComp.SetMovementLocked(true);

        float impactDelay = animDuration * 0.5f;

        var host = user.GetComponent<MonoBehaviour>();
        if (host != null)
        {
            host.StartCoroutine(ApplyAoEAfterDelay(user.transform, stats, impactDelay));
            host.StartCoroutine(UnlockAfterDelay(attackComp, moveComp, animDuration));
        }
        return true;
    }

    /// <summary>
    /// 지연 후 범위 내 적에게 피해를 적용합니다.
    /// </summary>
    private IEnumerator ApplyAoEAfterDelay(Transform userTf, PlayerStatsManager stats, float delay)
    {
        yield return new WaitForSeconds(delay);

        Collider[] hits = Physics.OverlapSphere(userTf.position, Range, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            EnemyStatsManager enemy = hit.GetComponent<EnemyStatsManager>();
            if (enemy != null && enemy.CurrentHP > 0)
            {
                bool isCrit;
                float baseDmg = stats.CalculateDamage(out isCrit);
                float finalDamage = baseDmg * damage;

                enemy.TakeDamage(finalDamage);

                DamageTextManager.Instance.ShowDamage(
                    enemy.transform,
                    Mathf.RoundToInt(finalDamage),
                    isCrit ? Color.red : Color.white,
                    DamageTextTarget.Enemy
                );

                Debug.Log($"{enemy.name}에게 {finalDamage} 피해를 주었습니다. (Crit={isCrit})");
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
