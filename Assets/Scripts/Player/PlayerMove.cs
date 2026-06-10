using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    private enum RMBMode { None, Move, ChaseEnemy }
    private RMBMode rmbMode = RMBMode.None;
    private EnemyStatsManager chasedEnemy;

    [SerializeField] private float baseRotationSpeed = 10f;
    [SerializeField] private float waypointReachDistance = 0.45f;
    [SerializeField] private float pathRefreshDistance = 0.75f;
    [SerializeField] private float chasePathRefreshInterval = 0.25f;
    [SerializeField] private float movementCollisionRadius = 0.15f;
    [SerializeField] private float pathProbeRadius = 0.25f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private Rigidbody rb;
    private Animation animationComponent;
    private PlayerStatsManager stats;
    private TileMapGenerator mapGenerator;

    private LayerMask wallLayer;

    private bool movementLocked = false;
    private readonly List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex;
    private Vector3 requestedDestination;
    private float nextPathRefreshTime;
    private PlayerAttacks attack;

    public event System.Action<IReadOnlyList<Vector3>> PathUpdated;

    /// <summary>
    /// 이동에 필요한 구성 요소를 초기화합니다.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        animationComponent = GetComponent<Animation>();
        stats = PlayerStatsManager.Instance;
        mapGenerator = FindAnyObjectByType<TileMapGenerator>();
        attack = GetComponent<PlayerAttacks>();

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
                    ClearPath();
                }
                else
                {
                    // 사거리 밖에서는 추적 모드로 전환합니다.
                    attack.SetTarget(clicked);
                    chasedEnemy = clicked;
                    rmbMode = RMBMode.ChaseEnemy;
                    SetMoveDestination(clicked.transform.position, true);
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
                    SetMoveDestination(hit.point, true);
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
                        SetMoveDestination(holdHit.point, false);
                    }
                }
                break;

                case RMBMode.ChaseEnemy:
                {
                    if (chasedEnemy != null && chasedEnemy.CurrentHP > 0)
                    {
                        if (Time.time >= nextPathRefreshTime)
                        {
                            SetMoveDestination(chasedEnemy.transform.position, false);
                            nextPathRefreshTime = Time.time + chasePathRefreshInterval;
                        }

                        if (attack != null && attack.IsInAttackRange(chasedEnemy))
                        {
                            attack.ChangeState(new AttackingStates());
                            rmbMode = RMBMode.None;
                            isMoving = false;
                            ClearPath();
                        }
                    }
                    else
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out RaycastHit holdHit))
                        {
                            rmbMode = RMBMode.Move;
                            SetMoveDestination(holdHit.point, true);
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
            if (!animationComponent.IsPlaying(PlayerAttacks.AttackAnimation) &&
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

        UpdatePathWaypoint();

        Vector3 toTarget = targetPosition - rb.position;
        float remainingDistance = toTarget.magnitude;
        Vector3 direction = remainingDistance > 0.001f ? toTarget / remainingDistance : Vector3.zero;
        Vector3 moveDelta = direction * Mathf.Min(moveSpeed * Time.fixedDeltaTime, remainingDistance);
        Vector3 nextPos = rb.position + moveDelta;

        if (moveDelta.magnitude < 0.001f)
        {
            isMoving = false;
            ClearPath();
            if (attack == null || !attack.isCastingSkill)
                attack?.ChangeState(new IdleStates());
            if (animationComponent != null && !animationComponent.IsPlaying(PlayerAttacks.AttackAnimation) &&
                (attack == null || !attack.isCastingSkill))
                animationComponent.Play(PlayerAttacks.IdleAnimation);
            return;
        }
        if (Physics.SphereCast(rb.position, movementCollisionRadius, direction, out _, moveDelta.magnitude + movementCollisionRadius, wallLayer))
        {
            if (TryBuildPath(requestedDestination, false))
            {
                isMoving = true;
                return;
            }

            StopMovement();
            return;
        }

        rb.MovePosition(nextPos);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        if (Vector3.Distance(rb.position, targetPosition) < waypointReachDistance)
        {
            if (!AdvancePath())
            {
                isMoving = false;
                ClearPath();
                if (attack == null || !attack.isCastingSkill)
                    attack?.ChangeState(new IdleStates());
                if (animationComponent != null && !animationComponent.IsPlaying(PlayerAttacks.AttackAnimation) &&
                    (attack == null || !attack.isCastingSkill))
                    animationComponent.Play(PlayerAttacks.IdleAnimation);
            }
        }
    }

    private void SetMoveDestination(Vector3 destination, bool forceRefresh)
    {
        destination.y = transform.position.y;

        if (!forceRefresh && isMoving && Vector3.Distance(requestedDestination, destination) < pathRefreshDistance)
            return;

        requestedDestination = destination;

        if (TryBuildPath(destination))
        {
            isMoving = true;
            attack?.ChangeState(new MovingStates());
            return;
        }

        if (mapGenerator != null)
        {
            StopMovement();
            return;
        }

        targetPosition = destination;
        isMoving = true;
        attack?.ChangeState(new MovingStates());
    }

    private bool TryBuildPath(Vector3 destination, bool allowSmoothing = true)
    {
        ClearPath();

        if (mapGenerator == null)
            mapGenerator = FindAnyObjectByType<TileMapGenerator>();
        if (mapGenerator == null)
            return false;

        Vector2Int start = WorldToCell(rb.position);
        Vector2Int goal = WorldToCell(destination);

        if (!FindNearestFloor(start, out start) || !FindNearestFloor(goal, out goal))
            return false;

        List<Vector2Int> cells = FindPath(start, goal);
        if (cells == null || cells.Count == 0)
            return false;

        if (allowSmoothing)
            cells = SmoothPath(cells);

        for (int i = 0; i < cells.Count; i++)
            currentPath.Add(CellToWorld(cells[i]));

        currentPathIndex = Mathf.Min(1, currentPath.Count - 1);
        targetPosition = currentPath[currentPathIndex];
        PathUpdated?.Invoke(currentPath);
        return true;
    }

    private void UpdatePathWaypoint()
    {
        if (currentPath.Count == 0)
            return;

        while (currentPathIndex < currentPath.Count - 1 &&
               Vector3.Distance(rb.position, targetPosition) <= waypointReachDistance)
        {
            currentPathIndex++;
            targetPosition = currentPath[currentPathIndex];
        }
    }

    private bool AdvancePath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count - 1)
            return false;

        currentPathIndex++;
        targetPosition = currentPath[currentPathIndex];
        return true;
    }

    private void ClearPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
    }

    private void StopMovement()
    {
        isMoving = false;
        ClearPath();
        if (attack == null || !attack.isCastingSkill)
            attack?.ChangeState(new IdleStates());
        if (animationComponent != null && !animationComponent.IsPlaying(PlayerAttacks.AttackAnimation) &&
            (attack == null || !attack.isCastingSkill))
            animationComponent.Play(PlayerAttacks.IdleAnimation);
    }

    public void CancelMovementForCombat()
    {
        isMoving = false;
        rmbMode = RMBMode.None;
        chasedEnemy = null;
        ClearPath();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x, transform.position.y, cell.y);
    }

    private bool FindNearestFloor(Vector2Int origin, out Vector2Int floor)
    {
        if (IsWalkable(origin))
        {
            floor = origin;
            return true;
        }

        int maxRadius = Mathf.Max(mapGenerator.width, mapGenerator.height);
        for (int radius = 1; radius < maxRadius; radius++)
        {
            for (int x = origin.x - radius; x <= origin.x + radius; x++)
            {
                if (TryNearestFloorCell(new Vector2Int(x, origin.y - radius), out floor) ||
                    TryNearestFloorCell(new Vector2Int(x, origin.y + radius), out floor))
                    return true;
            }

            for (int y = origin.y - radius + 1; y <= origin.y + radius - 1; y++)
            {
                if (TryNearestFloorCell(new Vector2Int(origin.x - radius, y), out floor) ||
                    TryNearestFloorCell(new Vector2Int(origin.x + radius, y), out floor))
                    return true;
            }
        }

        floor = origin;
        return false;
    }

    private bool TryNearestFloorCell(Vector2Int cell, out Vector2Int floor)
    {
        if (IsWalkable(cell))
        {
            floor = cell;
            return true;
        }

        floor = cell;
        return false;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        return mapGenerator != null && mapGenerator.IsFloor(cell.x, cell.y);
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal)
            return new List<Vector2Int> { start };

        var startNode = new PathNode(start, 0, Heuristic(start, goal), null);
        var open = new List<PathNode> { startNode };
        var bestNodes = new Dictionary<Vector2Int, PathNode> { [start] = startNode };
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].FCost < open[bestIndex].FCost ||
                    (open[i].FCost == open[bestIndex].FCost && open[i].HCost < open[bestIndex].HCost))
                {
                    bestIndex = i;
                }
            }

            PathNode current = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (current.Cell == goal)
                return ReconstructPath(current);

            closed.Add(current.Cell);

            foreach (Vector2Int neighbor in GetNeighbors(current.Cell))
            {
                if (closed.Contains(neighbor) || !IsWalkable(neighbor))
                    continue;

                int moveCost = (neighbor.x == current.Cell.x || neighbor.y == current.Cell.y) ? 10 : 14;
                int tentativeG = current.GCost + moveCost;

                PathNode known;
                if (bestNodes.TryGetValue(neighbor, out known) && tentativeG >= known.GCost)
                    continue;

                PathNode next = new PathNode(neighbor, tentativeG, Heuristic(neighbor, goal), current);
                bestNodes[neighbor] = next;
                if (known != null)
                    open.Remove(known);
                open.Add(next);
            }
        }

        return null;
    }

    private List<Vector2Int> SmoothPath(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count <= 2)
            return cells;

        var smoothed = new List<Vector2Int> { cells[0] };
        int anchorIndex = 0;

        while (anchorIndex < cells.Count - 1)
        {
            int nextIndex = cells.Count - 1;
            while (nextIndex > anchorIndex + 1)
            {
                if (HasWalkableLine(cells[anchorIndex], cells[nextIndex]))
                    break;

                nextIndex--;
            }

            smoothed.Add(cells[nextIndex]);
            anchorIndex = nextIndex;
        }

        return smoothed;
    }

    private bool HasWalkableLine(Vector2Int from, Vector2Int to)
    {
        Vector2 start = new Vector2(from.x, from.y);
        Vector2 end = new Vector2(to.x, to.y);
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance * 4f));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            Vector2Int cell = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));

            if (!IsWalkable(cell))
                return false;
        }

        return IsPhysicsPathClear(CellToWorld(from), CellToWorld(to));
    }

    private bool IsPhysicsPathClear(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        Vector3 direction = delta / distance;
        return !Physics.SphereCast(from, pathProbeRadius, direction, out _, distance, wallLayer);
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                Vector2Int next = new Vector2Int(cell.x + dx, cell.y + dy);
                if (dx != 0 && dy != 0)
                {
                    if (!IsWalkable(new Vector2Int(cell.x + dx, cell.y)) ||
                        !IsWalkable(new Vector2Int(cell.x, cell.y + dy)))
                        continue;
                }

                yield return next;
            }
        }
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 14 * Mathf.Min(dx, dy) + 10 * Mathf.Abs(dx - dy);
    }

    private List<Vector2Int> ReconstructPath(PathNode node)
    {
        var path = new List<Vector2Int>();
        for (PathNode current = node; current != null; current = current.Parent)
            path.Add(current.Cell);

        path.Reverse();
        return path;
    }

    private class PathNode
    {
        public readonly Vector2Int Cell;
        public readonly int GCost;
        public readonly int HCost;
        public readonly PathNode Parent;
        public int FCost => GCost + HCost;

        public PathNode(Vector2Int cell, int gCost, int hCost, PathNode parent)
        {
            Cell = cell;
            GCost = gCost;
            HCost = hCost;
            Parent = parent;
        }
    }

    /// <summary>
    /// 이동 여부를 반환합니다.
    /// </summary>
    public bool IsMoving() => isMoving;

    public IReadOnlyList<Vector3> GetCurrentPathSnapshot()
    {
        return currentPath;
    }
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
            ClearPath();
            if (attack == null || !attack.isCastingSkill)
                attack?.ChangeState(new IdleStates());
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
