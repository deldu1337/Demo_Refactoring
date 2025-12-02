using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    private enum RMBMode { None, Move, ChaseEnemy }
    private RMBMode rmbMode = RMBMode.None;
    private EnemyStatsManager chasedEnemy;

    [SerializeField] private float baseRotationSpeed = 10f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private Rigidbody rb;
    private Animation animationComponent;
    private PlayerStatsManager stats;

    private LayerMask wallLayer;

    private bool movementLocked = false;

    /// <summary>
    /// 이동에 필요한 구성 요소를 초기화합니다.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        animationComponent = GetComponent<Animation>();
        stats = PlayerStatsManager.Instance;

        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트를 찾지 못했습니다.");
        if (stats == null)
            Debug.LogError("PlayerStatsManager를 찾지 못했습니다.");

        wallLayer = LayerMask.GetMask("Wall");
    }

    /// <summary>
    /// 매 프레임마다 이동 입력을 처리합니다.
    /// </summary>
    void Update()
    {
        if (!movementLocked)
            HandleMovementInput();

        if (isMoving)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.green);
            Debug.DrawRay(targetPosition + Vector3.up * 0.1f, Vector3.up * 0.2f, Color.green);
        }
    }

    /// <summary>
    /// 물리 업데이트에서 실제 이동을 처리합니다.
    /// </summary>
    void FixedUpdate()
    {
        if (isMoving && !movementLocked)
            MovePlayer();
    }

    /// <summary>
    /// 우클릭 입력을 감지하여 이동 또는 추적 동작을 설정합니다.
    /// </summary>
    void HandleMovementInput()
    {
        if (movementLocked) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var attack = GetComponent<PlayerAttacks>();

        // 1) 우클릭을 눌렀을 때 이동 또는 추적을 결정합니다.
        if (Input.GetMouseButtonDown(1))
        {
            chasedEnemy = null;

            if (attack != null && attack.TryPickEnemyUnderMouse(out var clicked))
            {
                if (attack.IsInAttackRange(clicked))
                {
                    // 사거리 안에서는 즉시 공격 상태로 전환합니다.
                    attack.SetTarget(clicked);
                    attack.ChangeState(new AttackingStates());
                    isMoving = false;
                    rmbMode = RMBMode.None;
                }
                else
                {
                    // 사거리 밖에서는 추적 모드로 전환합니다.
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
                // 적이 아닌 지점을 클릭하면 해당 위치로 이동합니다.
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

        // 2) 우클릭을 유지하는 동안 목표를 갱신합니다.
        if (Input.GetMouseButton(1))
        {
            switch (rmbMode)
            {
                case RMBMode.Move:
                {
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
                    if (chasedEnemy != null && chasedEnemy.CurrentHP > 0)
                    {
                        targetPosition = chasedEnemy.transform.position;
                        targetPosition.y = transform.position.y;
                        isMoving = true;

                        if (attack != null && attack.IsInAttackRange(chasedEnemy))
                        {
                            attack.ChangeState(new AttackingStates());
                            rmbMode = RMBMode.None;
                            isMoving = false;
                        }
                    }
                    else
                    {
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
                    break;
            }
        }

        // 3) 우클릭을 떼면 추적 상태를 초기화합니다.
        if (Input.GetMouseButtonUp(1))
        {
            rmbMode = RMBMode.None;
            chasedEnemy = null;
        }

        // 이동 중에는 달리기 애니메이션을 재생합니다.
        if (isMoving && animationComponent != null)
        {
            if (!animationComponent.IsPlaying("Attack1H (ID 17 variation 0)") &&
                !animationComponent.IsPlaying("Run (ID 5 variation 0)"))
            {
                animationComponent.Play("Run (ID 5 variation 0)");
            }
        }
    }

    /// <summary>
    /// 목표 위치를 향해 이동하고 회전합니다.
    /// </summary>
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

    /// <summary>
    /// 이동 여부를 반환합니다.
    /// </summary>
    public bool IsMoving() => isMoving;
    /// <summary>
    /// 애니메이션 컴포넌트를 반환합니다.
    /// </summary>
    public Animation GetAnimation() => animationComponent;

    /// <summary>
    /// 외부에서 이동을 잠그거나 해제하도록 설정합니다.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            isMoving = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 이동 잠금 여부를 확인합니다.
    /// </summary>
    public bool IsMovementLocked() => movementLocked;
}
