using System.Collections;
using UnityEngine;
using static DamageTextManager;

/// <summary>
/// 대상에게 돌진한 뒤 피해를 주는 차지 스킬입니다.
/// </summary>
public class ChargeSkill : ISkill
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public float Cooldown { get; private set; }
    public float MpCost { get; private set; }
    public float Range { get; private set; }
    public float ImpactDelay { get; private set; }

    private float damageMul;
    private string animationName;

    private const float DashSpeed = 70f;             // 돌진 이동 속도입니다.
    private const float MinStopPadding = 0.25f;      // 대상과의 최소 여유 거리입니다.
    private const float MaxDashTimePerMeter = 0.12f; // 이동 거리당 최대 돌진 시간입니다.
    private const float fallbackHitWindup = 0.1f;    // 애니메이션 정보가 없을 때 대기 시간입니다.

    /// <summary>
    /// 스킬 데이터를 기반으로 차지 스킬을 초기화합니다.
    /// </summary>
    public ChargeSkill(SkillData data)
    {
        Id = data.id;
        Name = data.name;
        Cooldown = data.cooldown;
        MpCost = data.mpCost;
        Range = data.range;
        ImpactDelay = data.impactDelay;
        damageMul = data.damage;
        animationName = data.animation;
    }

    /// <summary>
    /// 지정된 대상에게 돌진하고 피해를 적용합니다.
    /// </summary>
    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        var anim = user.GetComponent<Animation>();
        var attack = user.GetComponent<PlayerAttacks>();
        var mover = user.GetComponent<PlayerMove>();
        var rb = user.GetComponent<Rigidbody>();

        // 현재 타겟이 없으면 마우스 아래 적을 선택합니다.
        EnemyStatsManager target = attack != null ? attack.targetEnemy : null;
        if (target == null || target.CurrentHP <= 0)
        {
            if (attack != null && attack.TryPickEnemyUnderMouse(out var picked)) target = picked;
        }

        // 유효한 대상이 없으면 시전을 중단합니다.
        if (target == null || target.CurrentHP <= 0)
        {
            Debug.LogWarning($"{Name} 실패: 대상이 없습니다.");
            return false;
        }

        // 사거리 안에 있는지 확인합니다.
        float dist = Vector3.Distance(user.transform.position, target.transform.position);
        if (dist > Range)
        {
            Debug.LogWarning($"{Name} 실패: 사거리({Range})를 초과했습니다. (현재 {dist:F2})");
            return false;
        }

        // 돌진 목표 지점을 계산하고 충돌 여부를 점검합니다.
        Vector3 startPos = user.transform.position;
        Vector3 targetPos = target.transform.position;

        Vector3 dir = targetPos - startPos; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = user.transform.forward; else dir.Normalize();

        float enemyR = EstimateRadius(target.GetComponent<Collider>());
        float selfR = EstimateRadius(user.GetComponent<Collider>());
        float stopDist = Mathf.Max(enemyR + selfR + MinStopPadding, 0.25f);
        Vector3 desired = targetPos - dir * stopDist;

        int wallMask = LayerMask.GetMask("Wall", "Obstacle");
        if (PathBlocked(startPos, desired, selfR * 0.9f, wallMask))
        {
            Debug.LogWarning($"{Name} 실패: 돌진 경로가 막혀 있습니다.");
            return false;
        }

        // 마나를 소모하고 부족하면 시전을 중단합니다.
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 실패: MP가 부족합니다.");
            return false;
        }

        // 공격과 이동을 잠시 잠급니다.
        if (attack != null)
        {
            attack.ForceStopAttack();
            attack.isCastingSkill = true;
            attack.isAttacking = true;
        }
        if (mover != null) mover.SetMovementLocked(true);

        // 코루틴 실행 주체를 찾습니다.
        MonoBehaviour runner = (MonoBehaviour)attack ?? (MonoBehaviour)mover ?? user.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            Debug.LogError($"{Name}: 코루틴 실행 주체를 찾을 수 없습니다.");
            if (attack != null) { attack.isCastingSkill = false; attack.isAttacking = false; }
            if (mover != null) mover.SetMovementLocked(false);
            return false;
        }

        runner.StartCoroutine(DashAndHit(user.transform, rb, stats, target, attack, mover, anim));
        return true;
    }

    /// <summary>
    /// 경로에 장애물이 있는지 구형 캐스트로 확인합니다.
    /// </summary>
    private static bool PathBlocked(Vector3 from, Vector3 to, float radius, int layerMask)
    {
        Vector3 start = from + Vector3.up * 0.2f;
        Vector3 dir = to - from; dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;
        dir /= dist;

        return Physics.SphereCast(start, Mathf.Max(0.05f, radius), dir, out _, dist, layerMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 목표 지점까지 돌진하고 피해를 적용합니다.
    /// </summary>
    private IEnumerator DashAndHit(Transform self, Rigidbody rb, PlayerStatsManager stats,
                                   EnemyStatsManager target, PlayerAttacks attack, PlayerMove mover,
                                   Animation anim)
    {
        Vector3 startPos = self.position;
        Vector3 targetPos = target.transform.position;

        Vector3 dir = targetPos - startPos;
        dir.y = 0f;
        float dirMag = dir.magnitude;
        if (dirMag < 0.0001f) dir = self.forward; else dir /= dirMag;

        float enemyR = EstimateRadius(target.GetComponent<Collider>());
        float selfR = EstimateRadius(self.GetComponent<Collider>());
        float stopDist = Mathf.Max(enemyR + selfR + MinStopPadding, 0.25f);

        Vector3 desired = targetPos - dir * stopDist;

        if (Physics.Raycast(startPos + Vector3.up * 0.2f, dir, out RaycastHit hit, Vector3.Distance(startPos, desired)))
        {
            desired = hit.point - dir * 0.2f;
        }

        float totalDist = Vector3.Distance(self.position, desired);
        float timeout = Mathf.Max(totalDist * MaxDashTimePerMeter, 0.25f);
        float t = 0f;

        bool lockFinalApproach = false;
        const float lockDistance = 0.35f;
        const float snapEpsilon = 0.12f;

        Face(self, target.transform.position);

        while (true)
        {
            t += Time.deltaTime;
            if (t > timeout) break;

            // 대상이 살아 있으면 목표 지점을 갱신합니다.
            if (!lockFinalApproach && target != null && target.CurrentHP > 0)
            {
                Vector3 curDir = (target.transform.position - self.position);
                curDir.y = 0f;
                float mag = curDir.magnitude;

                if (mag > 0.0001f) curDir /= mag; else curDir = self.forward;

                float curEnemyR = EstimateRadius(target.GetComponent<Collider>());
                float curSelfR = EstimateRadius(self.GetComponent<Collider>());
                float curStop = Mathf.Max(curEnemyR + curSelfR + MinStopPadding, 0.25f);

                Vector3 newDesired = target.transform.position - curDir * curStop;
                desired = newDesired;

                float distToDesiredNow = Vector3.Distance(self.position, desired);
                if (distToDesiredNow <= lockDistance)
                    lockFinalApproach = true;

                Face(self, target.transform.position);
            }

            // 목표 지점까지 이동합니다.
            Vector3 to = desired - self.position; to.y = 0f;
            float d = to.magnitude;

            if (d <= snapEpsilon)
            {
                if (rb != null && !rb.isKinematic) rb.MovePosition(desired);
                else self.position = desired;
                break;
            }

            Vector3 step = to.normalized * DashSpeed * Time.deltaTime;
            if (step.magnitude > d) step = to;

            if (rb != null && !rb.isKinematic)
                rb.MovePosition(self.position + step);
            else
                self.position += step;

            yield return null;
        }

        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;

        // 애니메이션을 재생하며 임팩트 타이밍을 기다립니다.
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);

            float waitTime = fallbackHitWindup;
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (target != null && target.CurrentHP > 0)
                    Face(self, target.transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 대상이 살아 있으면 피해를 적용합니다.
        if (target != null && target.CurrentHP > 0)
        {
            bool isCrit;
            float baseDmg = stats.CalculateDamage(out isCrit);
            float finalDamage = baseDmg * damageMul;

            target.TakeDamage(finalDamage);

            DamageTextManager.Instance.ShowDamage(
                target.transform,
                Mathf.RoundToInt(finalDamage),
                isCrit ? Color.red : Color.white,
                DamageTextTarget.Enemy
            );

            if (ImpactDelay > 0f)
                yield return new WaitForSeconds(ImpactDelay);
        }

        // 시전이 끝나면 상태를 해제합니다.
        if (attack != null)
        {
            attack.isCastingSkill = false;
            attack.isAttacking = false;
            if (attack.targetEnemy != null && attack.targetEnemy.CurrentHP > 0)
                attack.ChangeState(new AttackingStates());
            else
                attack.ChangeState(new IdleStates());
        }
        if (mover != null) mover.SetMovementLocked(false);
    }

    /// <summary>
    /// 지정된 방향을 바라보도록 회전합니다.
    /// </summary>
    private static void Face(Transform self, Vector3 worldTarget)
    {
        Vector3 d = worldTarget - self.position; d.y = 0f;
        if (d.sqrMagnitude > 0.0001f)
            self.rotation = Quaternion.LookRotation(d);
    }

    /// <summary>
    /// 콜라이더 크기를 기반으로 반지름을 추정합니다.
    /// </summary>
    private static float EstimateRadius(Collider col)
    {
        if (!col) return 0.4f;
        var b = col.bounds;
        return Mathf.Max(b.extents.x, b.extents.z);
    }
}
