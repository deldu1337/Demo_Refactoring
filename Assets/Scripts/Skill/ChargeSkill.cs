using System.Collections;
using UnityEngine;
using static DamageTextManager;

public class ChargeSkill : ISkill
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public float Cooldown { get; private set; }
    public float MpCost { get; private set; }
    public float Range { get; private set; }
    public float ImpactDelay { get; private set; } // 도착 후 임팩트 연출용(원하면 사용)
    private float damageMul;
    private string animationName;

    // 튜닝 파라미터
    private const float DashSpeed = 70f;             // 돌진 속도(유닛/초)
    private const float MinStopPadding = 0.25f;      // 적 앞에 멈출 여유 거리
    private const float MaxDashTimePerMeter = 0.12f; // 안전 타임아웃(장애물 등)
    private const float fallbackHitWindup = 0.1f;   // 애니 길이를 알 수 없을 때 대기

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

    public bool Execute(GameObject user, PlayerStatsManager stats)
    {
        var anim = user.GetComponent<Animation>();
        var attack = user.GetComponent<PlayerAttacks>();
        var mover = user.GetComponent<PlayerMove>();
        var rb = user.GetComponent<Rigidbody>();

        // 1) 타겟 확보
        EnemyStatsManager target = attack != null ? attack.targetEnemy : null;
        if (target == null || target.CurrentHP <= 0)
            if (attack != null && attack.TryPickEnemyUnderMouse(out var picked)) target = picked;

        if (target == null || target.CurrentHP <= 0)
        {
            Debug.LogWarning($"{Name} 실패: 유효한 타겟 없음");
            return false;
        }

        // 2) 사거리 체크
        float dist = Vector3.Distance(user.transform.position, target.transform.position);
        if (dist > Range)
        {
            Debug.LogWarning($"{Name} 실패: 사거리({Range}) 밖 (현재 {dist:F2})");
            return false;
        }

        // 2.5) 시전 전 경로 장애물 체크
        // 적 앞에 설 목표 지점(desired) 미리 계산해서, 그 지점까지 구체 캐스트로 막힘 확인
        Vector3 startPos = user.transform.position;
        Vector3 targetPos = target.transform.position;

        Vector3 dir = targetPos - startPos; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = user.transform.forward; else dir.Normalize();

        float enemyR = EstimateRadius(target.GetComponent<Collider>());
        float selfR = EstimateRadius(user.GetComponent<Collider>());
        float stopDist = Mathf.Max(enemyR + selfR + MinStopPadding, 0.25f);
        Vector3 desired = targetPos - dir * stopDist;

        // 벽 레이어가 있다면 여기에 지정 (없으면 Default로도 동작)
        int wallMask = LayerMask.GetMask("Wall", "Obstacle"); // 프로젝트에 맞게
        if (PathBlocked(startPos, desired, selfR * 0.9f, wallMask))
        {
            Debug.LogWarning($"{Name} 실패: 전방에 장애물로 인해 돌진 불가");
            return false;
        }

        // 3) MP 차감 (장애물 통과했을 때만)
        if (!stats.UseMana(MpCost))
        {
            Debug.LogWarning($"{Name} 실패: MP 부족");
            return false;
        }

        // 4) 락/상태 설정
        if (attack != null)
        {
            attack.ForceStopAttack();
            attack.isCastingSkill = true;
            attack.isAttacking = true;
        }
        if (mover != null) mover.SetMovementLocked(true);

        // 5) 애니메이션은 도착 후 재생

        // 6) 코루틴 시작
        MonoBehaviour runner = (MonoBehaviour)attack ?? (MonoBehaviour)mover ?? user.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            Debug.LogError($"{Name}: 코루틴 실행 주체 없음");
            if (attack != null) { attack.isCastingSkill = false; attack.isAttacking = false; }
            if (mover != null) mover.SetMovementLocked(false);
            return false;
        }

        runner.StartCoroutine(DashAndHit(user.transform, rb, stats, target, attack, mover, anim));
        return true;
    }

    private static bool PathBlocked(Vector3 from, Vector3 to, float radius, int layerMask)
    {
        Vector3 start = from + Vector3.up * 0.2f;   // 바닥 튕김 방지
        Vector3 dir = to - from; dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false; // 제자리면 막힘 아님
        dir /= dist;

        // 스피어캐스트로 경로 상 충돌 체크
        // 히트가 있으면 막힘
        return Physics.SphereCast(start, Mathf.Max(0.05f, radius), dir, out _, dist, layerMask, QueryTriggerInteraction.Ignore);
    }



    private IEnumerator DashAndHit(Transform self, Rigidbody rb, PlayerStatsManager stats,
                                   EnemyStatsManager target, PlayerAttacks attack, PlayerMove mover,
                                   Animation anim)
    {
        // 돌진 목표 지점 계산(적 바로 앞)
        Vector3 startPos = self.position;
        Vector3 targetPos = target.transform.position;

        // 수평 방향
        Vector3 dir = targetPos - startPos;
        dir.y = 0f;
        float dirMag = dir.magnitude;
        if (dirMag < 0.0001f) dir = self.forward; else dir /= dirMag;

        // 적/플레이어의 반경 추정 → 너무 붙지 말고 앞에 멈추기
        float enemyR = EstimateRadius(target.GetComponent<Collider>());
        float selfR = EstimateRadius(self.GetComponent<Collider>());
        float stopDist = Mathf.Max(enemyR + selfR + MinStopPadding, 0.25f);

        Vector3 desired = targetPos - dir * stopDist;

        // 장애물에 막히면 그 앞에서 멈추도록 레이캐스트
        if (Physics.Raycast(startPos + Vector3.up * 0.2f, dir, out RaycastHit hit, Vector3.Distance(startPos, desired)))
        {
            // 벽/장애물 충돌 시 약간 뒤에서 멈추기
            desired = hit.point - dir * 0.2f;
        }

        // ===== 돌진 루프 =====
        float totalDist = Vector3.Distance(self.position, desired);
        float timeout = Mathf.Max(totalDist * MaxDashTimePerMeter, 0.25f);
        float t = 0f;

        // 목표 보정 관련 플래그/임계값
        bool lockFinalApproach = false;          // 마지막 구간에서는 desired 고정
        const float lockDistance = 0.35f;        // 이 거리 이내로 들어오면 desired 고정
        const float snapEpsilon = 0.12f;       // 충분히 가깝다면 바로 스냅

        Face(self, target.transform.position);

        while (true)
        {
            t += Time.deltaTime;
            if (t > timeout) break;

            // 1) 마지막 구간 전까지만 desired를 타겟에 맞춰 갱신
            if (!lockFinalApproach && target != null && target.CurrentHP > 0)
            {
                Vector3 curDir = (target.transform.position - self.position);
                curDir.y = 0f;
                float mag = curDir.magnitude;

                // 너무 가까우면 현재 forward 사용
                if (mag > 0.0001f) curDir /= mag; else curDir = self.forward;

                float curEnemyR = EstimateRadius(target.GetComponent<Collider>());
                float curSelfR = EstimateRadius(self.GetComponent<Collider>());
                float curStop = Mathf.Max(curEnemyR + curSelfR + MinStopPadding, 0.25f);

                Vector3 newDesired = target.transform.position - curDir * curStop;
                desired = newDesired;

                // 마지막 구간 진입: desired를 더 이상 갱신하지 않음(서성임 방지)
                float distToDesiredNow = Vector3.Distance(self.position, desired);
                if (distToDesiredNow <= lockDistance)
                    lockFinalApproach = true;

                Face(self, target.transform.position);
            }

            // 2) 이동
            Vector3 to = desired - self.position; to.y = 0f;
            float d = to.magnitude;

            // 충분히 가까우면 바로 스냅해서 종료
            if (d <= snapEpsilon)
            {
                // rb 사용 중이면 MovePosition으로 스냅
                if (rb != null && !rb.isKinematic) rb.MovePosition(desired);
                else self.position = desired;
                break;
            }

            Vector3 step = to.normalized * DashSpeed * Time.deltaTime;
            if (step.magnitude > d) step = to; // 오버슈팅 방지

            if (rb != null && !rb.isKinematic)
                rb.MovePosition(self.position + step);
            else
                self.position += step;

            yield return null;
        }

        // 도착 직후 잔속 제거(옵션)
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;


        // ===== 도착 지점 =====
        // 1) 도착 후 애니메이션 실행 & 종료까지 대기
        if (anim && !string.IsNullOrEmpty(animationName))
        {
            anim.CrossFade(animationName, 0.1f);

            float waitTime = fallbackHitWindup;
            // 클립 길이 알 수 있으면 그것만큼 대기
            //if (anim[animationName] != null && anim[animationName].length > 0f)
            //    waitTime = anim[animationName].length;

            // 대기 중에도 타겟 바라보기 유지(선택)
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (target != null && target.CurrentHP > 0)
                    Face(self, target.transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 2) 애니 종료 후 데미지 적용
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

        // 3) 상태/락 해제
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

    private static void Face(Transform self, Vector3 worldTarget)
    {
        Vector3 d = worldTarget - self.position; d.y = 0f;
        if (d.sqrMagnitude > 0.0001f)
            self.rotation = Quaternion.LookRotation(d);
    }

    private static float EstimateRadius(Collider col)
    {
        if (!col) return 0.4f; // 대충 기본값
        var b = col.bounds;
        // X/Z 중 큰 값의 절반을 반경으로 가정
        return Mathf.Max(b.extents.x, b.extents.z);
    }
}
