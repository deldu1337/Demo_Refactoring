using UnityEngine;
using UnityEngine.EventSystems; // 추가 필요

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    enum RMBMode { None, Move, ChaseEnemy }   // 우클릭 의도
    private RMBMode rmbMode = RMBMode.None;   // 현재 의도
    private EnemyStatsManager chasedEnemy;     // 추적 대상(적)

    [SerializeField] private float baseRotationSpeed = 10f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private Rigidbody rb;
    private Animation animationComponent;
    private PlayerStatsManager stats;

    private LayerMask wallLayer;

    // 추가: 이동 잠금 플래그
    private bool movementLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        animationComponent = GetComponent<Animation>();
        stats = PlayerStatsManager.Instance; // ← 싱글톤

        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트가 없습니다!");
        if (stats == null)
            Debug.LogError("PlayerStatsManager 싱글톤이 없습니다!");

        wallLayer = LayerMask.GetMask("Wall");
    }

    void Update()
    {
        // 잠금 중에는 입력 자체를 받지 않음
        if (!movementLocked)
            HandleMovementInput();

        if (isMoving)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.green);
            Debug.DrawRay(targetPosition + Vector3.up * 0.1f, Vector3.up * 0.2f, Color.green);
        }
    }

    void FixedUpdate()
    {
        // 잠금 중에는 실제 이동도 수행하지 않음
        if (isMoving && !movementLocked)
            MovePlayer();
    }
    void HandleMovementInput()
    {
        if (movementLocked) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var attack = GetComponent<PlayerAttacks>();

        // =======================
        // 1) 우클릭 "한 번" 눌렀을 때 → 의도 결정
        // =======================
        if (Input.GetMouseButtonDown(1))
        {
            chasedEnemy = null;

            if (attack != null && attack.TryPickEnemyUnderMouse(out var clicked))
            {
                // 적을 눌렀음
                if (attack.IsInAttackRange(clicked))
                {
                    // 근접 사거리 → 즉시 공격, 이동 X
                    attack.SetTarget(clicked);
                    attack.ChangeState(new AttackingStates());
                    isMoving = false;
                    rmbMode = RMBMode.None;   // 드래그 동안 이동 갱신 안함
                }
                else
                {
                    // 사거리 밖 → 적에게 "추격 이동" 의도
                    attack.SetTarget(clicked);
                    chasedEnemy = clicked;
                    rmbMode = RMBMode.ChaseEnemy;
                    targetPosition = clicked.transform.position;
                    targetPosition.y = transform.position.y;
                    isMoving = true;
                }
            }
            else
            {
                // 땅을 눌렀음 → "이동" 의도
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (attack != null)
                    {
                        attack.ForceStopAttack();
                        attack.ClearTarget();
                        attack.ChangeState(new IdleStates());
                    }

                    rmbMode = RMBMode.Move;
                    targetPosition = hit.point;
                    targetPosition.y = transform.position.y;
                    isMoving = true;
                }
            }
        }

        // =======================
        // 2) 우클릭 "누르는 동안" → 의도에 따라 계속 갱신
        // =======================
        if (Input.GetMouseButton(1))
        {
            switch (rmbMode)
            {
                case RMBMode.Move:
                    {
                        // 커서가 적 위에 있어도 '이동' 의도이면 계속 갱신
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out RaycastHit holdHit))
                        {
                            targetPosition = holdHit.point;
                            targetPosition.y = transform.position.y;
                            isMoving = true;
                        }
                    }
                    break;

                case RMBMode.ChaseEnemy:
                    {
                        // 적 추격: 대상이 살아있으면 대상 위치로 이동을 갱신
                        if (chasedEnemy != null && chasedEnemy.CurrentHP > 0)
                        {
                            targetPosition = chasedEnemy.transform.position;
                            targetPosition.y = transform.position.y;
                            isMoving = true;

                            // 사거리 들어오면 상태 전환(공격 시작)
                            if (attack != null && attack.IsInAttackRange(chasedEnemy))
                            {
                                attack.ChangeState(new AttackingStates());
                                rmbMode = RMBMode.None; // 더 이상의 이동 갱신은 중단
                                isMoving = false;
                            }
                        }
                        else
                        {
                            // 추적 대상이 사라지면 일반 이동으로 폴백
                            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out RaycastHit holdHit))
                            {
                                rmbMode = RMBMode.Move;
                                targetPosition = holdHit.point;
                                targetPosition.y = transform.position.y;
                                isMoving = true;
                            }
                        }
                    }
                    break;

                case RMBMode.None:
                    // 의도가 '근접 즉시공격'이던 경우: 드래그 중 이동 갱신 없음
                    break;
            }
        }

        // =======================
        // 3) 우클릭 뗐을 때 → 의도 종료
        // =======================
        if (Input.GetMouseButtonUp(1))
        {
            rmbMode = RMBMode.None;
            chasedEnemy = null;
        }

        // =======================
        // 이동 애니메이션 처리
        // =======================
        if (isMoving && animationComponent != null)
        {
            if (!animationComponent.IsPlaying("Attack1H (ID 17 variation 0)") &&
                !animationComponent.IsPlaying("Run (ID 5 variation 0)"))
            {
                animationComponent.Play("Run (ID 5 variation 0)");
            }
        }
    }


    void MovePlayer()
    {
        float moveSpeed = stats.Data.Dex;
        float rotationSpeed = baseRotationSpeed + stats.Data.Dex * 0.5f;

        Vector3 direction = (targetPosition - rb.position).normalized;
        Vector3 moveDelta = direction * moveSpeed * Time.fixedDeltaTime;
        Vector3 nextPos = rb.position + moveDelta;

        if (Physics.SphereCast(rb.position, 0.1f, direction, out _, moveDelta.magnitude + 0.1f, wallLayer))
        {
            isMoving = false;
            if (animationComponent != null && !animationComponent.IsPlaying("Attack1H (ID 17 variation 0)"))
                animationComponent.Play("Stand (ID 0 variation 0)");
            return;
        }
        if (moveDelta.magnitude < 0.001f)
        {
            isMoving = false;
            if (animationComponent != null && !animationComponent.IsPlaying("Attack1H (ID 17 variation 0)"))
                animationComponent.Play("Stand (ID 0 variation 0)");
            return;
        }

        rb.MovePosition(nextPos);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        if (Vector3.Distance(rb.position, targetPosition) < 0.2f)
        {
            isMoving = false;
            if (animationComponent != null && !animationComponent.IsPlaying("Attack1H (ID 17 variation 0)"))
                animationComponent.Play("Stand (ID 0 variation 0)");
        }
    }

    // === 외부 접근용 ===
    public bool IsMoving() => isMoving;
    public Animation GetAnimation() => animationComponent;

    // 추가: 스킬 등이 호출하는 이동 잠금 토글
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            // 즉시 이동 정지 (물리 속도도 0으로)
            isMoving = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        // 잠금 해제 시엔 입력/이동 루틴이 자연스럽게 재개됩니다.
    }

    // (선택) 외부에서 상태 확인하고 싶다면:
    public bool IsMovementLocked() => movementLocked;
}