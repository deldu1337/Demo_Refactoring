using System.Collections;
using UnityEngine;
using static DamageTextManager;

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

    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        var anim = user.GetComponent<Animation>();
        var attackComp = user.GetComponent<PlayerAttacks>();
        var moveComp = user.GetComponent<PlayerMove>();

        // === 1) 유효 타겟 확보 ===
        EnemyStatsManager target = attackComp != null ? attackComp.targetEnemy : null;

        // 타겟 없거나 죽었으면 마우스 아래 적 찾기 (근접 보정 포함)
        if (target == null || target.CurrentHP <= 0)
        {
            if (attackComp != null && attackComp.TryPickEnemyUnderMouse(out var picked))
            {
                target = picked;
            }
        }

        // === 2) 타겟 최종 검증 ===
        if (target == null || target.CurrentHP <= 0)
        {
            Debug.LogWarning($"{Name} 실패: 유효한 타겟이 없습니다.");
            return false;
        }

        // === 3) 사거리 체크 ===
        float dist = Vector3.Distance(user.transform.position, target.transform.position);
        if (dist > Range)
        {
            Debug.LogWarning($"{Name} 실패: 사거리({Range}m) 밖입니다. (현재 {dist:F2}m)");
            return false;
        }

        // === 4) 마나 차감 ===
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 실패: MP 부족");
            return false;
        }

        if (attackComp != null)
        {
            attackComp.ForceStopAttack(); // 일반 공격 즉시 중단
            attackComp.isCastingSkill = true; // 스킬 우선 모드
        }

        // === 5) 스킬 시작 시 타겟 방향으로 회전 ===
        FaceTargetInstant(user.transform, target.transform.position);

        // === 6) 애니메이션 재생 + 시전 잠금 ===
        float animDuration = 0.5f; // 기본값
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);
            AnimationState state = anim[animationName];
            if (state != null)
                animDuration = state.length / Mathf.Max(0.0001f, state.speed);
        }

        if (attackComp != null) attackComp.isAttacking = true;
        if (moveComp != null) moveComp.SetMovementLocked(true);

        // === 7) 임팩트 타이밍 (애니메이션 비율 기반) ===
        float impactDelay = animDuration * ImpactDelay;

        user.GetComponent<MonoBehaviour>()
            .StartCoroutine(DealDamageAfterDelay(user.transform, target, stats, impactDelay));

        // === 8) 시전 종료 후 잠금 해제 ===
        user.GetComponent<MonoBehaviour>()
            .StartCoroutine(UnlockAfterDelay(attackComp, moveComp, animDuration));

        return true;
    }

    // 즉시 목표를 바라보게 (y축 고정)
    private void FaceTargetInstant(Transform self, Vector3 targetPos)
    {
        Vector3 dir = targetPos - self.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            self.rotation = Quaternion.LookRotation(dir);
    }

    private IEnumerator DealDamageAfterDelay(Transform userTf, EnemyStatsManager target, PlayerStatsManager stats, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 임팩트 직전 회전 보정
        if (target != null) FaceTargetInstant(userTf, target.transform.position);

        // 타겟 생존 + 사거리 체크
        if (target != null && target.CurrentHP > 0)
        {
            float dist = Vector3.Distance(userTf.position, target.transform.position);
            if (dist <= Range)
            {
                // 치명타 여부 포함 계산
                bool isCrit;
                float baseDmg = stats.CalculateDamage(out isCrit);
                float finalDamage = baseDmg * damage;

                target.TakeDamage(finalDamage);

                // 적 Transform에 고정 + 색상(치명타=빨강, 평타=흰색)
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
