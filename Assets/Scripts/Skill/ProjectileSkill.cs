using System.Collections;
using UnityEngine;
using static DamageTextManager;
using static UnityEngine.GraphicsBuffer;

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

    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 사용 실패: MP 부족");
            return false;
        }

        Animation anim = user.GetComponent<Animation>();
        PlayerAttacks attackComp = user.GetComponent<PlayerAttacks>();
        PlayerMove moveComp = user.GetComponent<PlayerMove>();

        if (attackComp != null)
        {
            attackComp.ForceStopAttack(); // 일반 공격 즉시 중단
            attackComp.isCastingSkill = true; // 스킬 우선 모드
        }

        float animDuration = 0.5f; // 기본값 (애니 없음 대비)
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);
            AnimationState state = anim[animationName];
            if (state != null)
                animDuration = state.length / Mathf.Max(state.speed, 0.0001f);
        }

        // 이동/공격 잠금
        if (attackComp != null) attackComp.isAttacking = true;
        if (moveComp != null) moveComp.SetMovementLocked(true);

        // 임팩트 타이밍: ActiveSkill과 동일하게 고정값 사용
        float impactDelay = animDuration * 0.5f;
        // 참고) 애니 기준 비율로 쓰고 싶다면 아래로 교체:

        // 임팩트 시점에 AoE 적용
        var host = user.GetComponent<MonoBehaviour>();
        if (host != null)
        {
            host.StartCoroutine(ApplyAoEAfterDelay(user.transform, stats, impactDelay));
            host.StartCoroutine(UnlockAfterDelay(attackComp, moveComp, animDuration));
        }
        return true;
    }

    private IEnumerator ApplyAoEAfterDelay(Transform userTf, PlayerStatsManager stats, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 임팩트 순간의 현재 위치를 기준으로 범위 내 적 탐지
        Collider[] hits = Physics.OverlapSphere(userTf.position, Range, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            EnemyStatsManager enemy = hit.GetComponent<EnemyStatsManager>();
            if (enemy != null && enemy.CurrentHP > 0)
            {
                // 각 적마다 독립적으로 치명타 판정
                bool isCrit;
                float baseDmg = stats.CalculateDamage(out isCrit);
                float finalDamage = baseDmg * damage;

                enemy.TakeDamage(finalDamage);

                // 적 Transform에 고정 + 색상(치명타=빨강, 평타=흰색)
                DamageTextManager.Instance.ShowDamage(
                    enemy.transform,
                    Mathf.RoundToInt(finalDamage),
                    isCrit ? Color.red : Color.white,
                    DamageTextTarget.Enemy
                );

                Debug.Log($"{enemy.name}에게 {finalDamage} 피해! (광역, Crit={isCrit})");
            }
        }
    }

    private IEnumerator UnlockAfterDelay(PlayerAttacks attack, PlayerMove move, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attack != null)
        {
            attack.isCastingSkill = false;  // 스킬 종료
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