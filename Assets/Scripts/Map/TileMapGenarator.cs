using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// BSP 분할을 활용해 던전 타일 맵을 생성하고 배치합니다.
/// </summary>
public class TileMapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class StageTheme
    {
        public string name;
        public Material wallMaterial;
        public Material floorMaterial;
    }

    [SerializeField] private StageManager stageManager;

    public int width = 100;
    public int height = 100;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public int minRoomSize = 10;
    public int maxRoomSize = 24;

    [Range(0.1f, 0.9f)] public float minimumDevideRate = 0.45f;
    [Range(0.1f, 0.9f)] public float maximumDivideRate = 0.55f;
    [Range(1, 12)] public int maxDepth = 6;
    [Range(1, 9)] public int corridorWidth = 5;

    public GameObject portalPrefab;
    public float portalYOffset = 0f;
    public int bossRoomWidth = 28;
    public int bossRoomHeight = 28;

    private int[,] map;
    private List<RectInt> rooms;
    private RectInt playerRoom;
    private RectInt bossRoom;

    [Header("Stage Themes")]
    public int stagesPerTheme = 5;
    public StageTheme[] themes;
    public bool useBossOverrideTheme = true;
    public StageTheme bossOverrideTheme;

    public delegate void MapGeneratedHandler();
    public event MapGeneratedHandler OnMapGenerated;

    /// <summary>
    /// 보스 방이 존재하는지 여부를 반환합니다.
    /// </summary>
    private bool HasBossRoom => bossRoom.width > 0 && bossRoom.height > 0;

    /// <summary>
    /// 시작 시 맵을 생성하고 렌더링합니다.
    /// </summary>
    private void Start()
    {
        GenerateMap();
        RenderMap();
    }

    /// <summary>
    /// 현재 스테이지에 맞는 테마를 반환합니다.
    /// </summary>
    private StageTheme GetActiveTheme()
    {
        if (useBossOverrideTheme && stageManager != null && stageManager.IsBossStage() && bossOverrideTheme != null)
            return bossOverrideTheme;

        if (themes == null || themes.Length == 0) return null;

        int stage = (stageManager != null) ? stageManager.currentStage : 1;
        int idx = Mathf.FloorToInt((stage - 1) / Mathf.Max(1, stagesPerTheme));
        idx = Mathf.Clamp(idx, 0, themes.Length - 1);
        return themes[idx];
    }

    /// <summary>
    /// BSP 규칙에 따라 방과 복도를 생성하고 포탈을 배치합니다.
    /// </summary>
    public void GenerateMap()
    {
        InitMap();

        // 플레이어 시작 방을 먼저 준비합니다
        CarvePlayerRoom();

        // 분할 트리를 만들고 방 후보를 계산합니다
        Node root = new Node(new RectInt(1, 1, width - 2, height - 2));
        SplitRoom(root, 0);

        // 리프 노드에 실제 방을 생성합니다
        GenerateRooms(root);

        // 보스 스테이지라면 중앙에 보스 방을 만듭니다
        bool isBoss = stageManager != null && stageManager.IsBossStage();
        if (isBoss) CarveBossRoomCenter();

        // 플레이어 방과 가장 가까운 방을 연결합니다
        ConnectPlayerRoomToNearestRoom();

        // 트리를 따라 복도를 연결합니다
        GenerateTreeCorridors(root);

        // 보스 방 입구를 다른 방과 이어 줍니다
        if (isBoss)
        {
            var centers = rooms.Where(r => !r.Overlaps(playerRoom) && !r.Overlaps(bossRoom))
                               .Select(r => new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y)))
                               .ToList();
            ConnectBossRoomEntrances(centers);
        }

        // 가장 먼 방에 포탈을 배치합니다
        PlacePortal();

        OnMapGenerated?.Invoke();
    }

    /// <summary>
    /// 맵 데이터와 방 목록을 초기화합니다.
    /// </summary>
    private void InitMap()
    {
        map = new int[width, height];
        rooms = new List<RectInt>();
        bossRoom = new RectInt(0, 0, 0, 0);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = 1;

        playerRoom = new RectInt(2, 2, 25, 25);
    }

    /// <summary>
    /// 플레이어가 시작하는 공간을 비워 둡니다.
    /// </summary>
    private void CarvePlayerRoom()
    {
        for (int x = playerRoom.xMin + 1; x < playerRoom.xMax - 1; x++)
            for (int y = playerRoom.yMin + 1; y < playerRoom.yMax - 1; y++)
                map[x, y] = 0;
    }

    /// <summary>
    /// 주어진 영역을 추가로 분할할 수 있는지 확인합니다.
    /// </summary>
    private bool CanSplit(RectInt r)
    {
        const int minLeaf = 10;
        return (r.width >= minLeaf * 2) || (r.height >= minLeaf * 2);
    }

    /// <summary>
    /// BSP 트리를 따라 노드를 분할합니다.
    /// </summary>
    private void SplitRoom(Node tree, int depth)
    {
        if (depth >= maxDepth || !CanSplit(tree.nodeRect))
            return;

        bool splitHoriz = tree.nodeRect.width >= tree.nodeRect.height;
        int axisLen = splitHoriz ? tree.nodeRect.width : tree.nodeRect.height;

        int minSplit = Mathf.Clamp(Mathf.RoundToInt(axisLen * minimumDevideRate), 1, axisLen - 1);
        int maxSplit = Mathf.Clamp(Mathf.RoundToInt(axisLen * maximumDivideRate), minSplit, axisLen - 1);
        if (minSplit >= maxSplit) return;

        int split = Random.Range(minSplit, maxSplit + 1);

        if (splitHoriz)
        {
            tree.leftNode = new Node(new RectInt(tree.nodeRect.x, tree.nodeRect.y, split, tree.nodeRect.height));
            tree.rightNode = new Node(new RectInt(tree.nodeRect.x + split, tree.nodeRect.y, tree.nodeRect.width - split, tree.nodeRect.height));
        }
        else
        {
            tree.leftNode = new Node(new RectInt(tree.nodeRect.x, tree.nodeRect.y, tree.nodeRect.width, split));
            tree.rightNode = new Node(new RectInt(tree.nodeRect.x, tree.nodeRect.y + split, tree.nodeRect.width, tree.nodeRect.height - split));
        }

        tree.leftNode.parNode = tree;
        tree.rightNode.parNode = tree;

        SplitRoom(tree.leftNode, depth + 1);
        SplitRoom(tree.rightNode, depth + 1);
    }

    /// <summary>
    /// 리프 노드마다 방을 만들고 맵에 표시합니다.
    /// </summary>
    private RectInt GenerateRooms(Node node)
    {
        if (node.leftNode == null && node.rightNode == null)
        {
            RectInt r = node.nodeRect;

            int minW = Mathf.Clamp(r.width / 2, 4, Mathf.Max(4, r.width - 1));
            int minH = Mathf.Clamp(r.height / 2, 4, Mathf.Max(4, r.height - 1));
            int maxW = Mathf.Max(minW + 1, Mathf.Min(maxRoomSize, r.width - 1));
            int maxH = Mathf.Max(minH + 1, Mathf.Min(maxRoomSize, r.height - 1));

            if (minW >= maxW || minH >= maxH)
            {
                node.roomRect = r;
            }
            else
            {
                int w = Random.Range(minW, maxW);
                int h = Random.Range(minH, maxH);
                int x = r.x + Random.Range(1, Mathf.Max(1, r.width - w));
                int y = r.y + Random.Range(1, Mathf.Max(1, r.height - h));
                node.roomRect = new RectInt(x, y, w, h);
            }

            if (!node.roomRect.Overlaps(playerRoom))
            {
                rooms.Add(node.roomRect);
                for (int x = node.roomRect.xMin; x < node.roomRect.xMax; x++)
                    for (int y = node.roomRect.yMin; y < node.roomRect.yMax; y++)
                        map[x, y] = 0;
            }

            return node.roomRect;
        }

        RectInt left = (node.leftNode != null) ? GenerateRooms(node.leftNode) : new RectInt();
        RectInt right = (node.rightNode != null) ? GenerateRooms(node.rightNode) : new RectInt();
        node.roomRect = (left.width > 0) ? left : right;
        return node.roomRect;
    }

    /// <summary>
    /// 플레이어 방에서 가장 가까운 방까지 복도를 팝니다.
    /// </summary>
    private void ConnectPlayerRoomToNearestRoom()
    {
        if (rooms == null || rooms.Count == 0) return;

        Vector2Int p = new Vector2Int(Mathf.RoundToInt(playerRoom.center.x), Mathf.RoundToInt(playerRoom.center.y));

        RectInt nearest = default;
        float best = float.MaxValue;
        foreach (var r in rooms)
        {
            var c = new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y));
            float d = Vector2Int.Distance(p, c);
            if (d < best) { best = d; nearest = r; }
        }
        if (nearest.width == 0 || nearest.height == 0) return;

        Vector2Int doorway = new Vector2Int((playerRoom.xMin + playerRoom.xMax) / 2, playerRoom.yMax - 1);
        int half = Mathf.Max(1, corridorWidth / 2);
        for (int w = -half; w <= half; w++)
        {
            int dx = doorway.x + w;
            int dy = doorway.y + 1;
            if (Inside(dx, doorway.y)) map[dx, doorway.y] = 0;
            if (Inside(dx, dy)) map[dx, dy] = 0;
        }

        Vector2Int target = new Vector2Int(Mathf.RoundToInt(nearest.center.x), Mathf.RoundToInt(nearest.center.y));
        DigCorridor(doorway, target);
    }

    /// <summary>
    /// 분할 트리를 순회하며 방의 중심을 복도로 연결합니다.
    /// </summary>
    private void GenerateTreeCorridors(Node node)
    {
        if (node.leftNode == null || node.rightNode == null)
            return;

        Vector2Int a = node.leftNode.center;
        Vector2Int b = node.rightNode.center;
        DigCorridor(a, b);

        GenerateTreeCorridors(node.leftNode);
        GenerateTreeCorridors(node.rightNode);
    }

    /// <summary>
    /// 두 좌표를 직교 형태의 복도로 연결합니다.
    /// </summary>
    private void DigCorridor(Vector2Int a, Vector2Int b)
    {
        int half = Mathf.Max(1, corridorWidth / 2);

        int x0 = Mathf.Min(a.x, b.x);
        int x1 = Mathf.Max(a.x, b.x);
        for (int x = x0; x <= x1; x++)
            for (int w = -half; w <= half; w++)
            {
                int yy = a.y + w;
                if (Inside(x, yy)) map[x, yy] = 0;
            }

        int y0 = Mathf.Min(a.y, b.y);
        int y1 = Mathf.Max(a.y, b.y);
        for (int y = y0; y <= y1; y++)
            for (int w = -half; w <= half; w++)
            {
                int xx = b.x + w;
                if (Inside(xx, y)) map[xx, y] = 0;
            }
    }

    /// <summary>
    /// 좌표가 맵 범위 안에 있는지 확인합니다.
    /// </summary>
    private bool Inside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    /// <summary>
    /// 맵 중앙에 보스 방을 만들고 비웁니다.
    /// </summary>
    private void CarveBossRoomCenter()
    {
        int bw = Mathf.Clamp(bossRoomWidth, minRoomSize, width - 4);
        int bh = Mathf.Clamp(bossRoomHeight, minRoomSize, height - 4);
        int bx = (width - bw) / 2;
        int by = (height - bh) / 2;
        bossRoom = new RectInt(bx, by, bw, bh);

        for (int x = bossRoom.xMin; x < bossRoom.xMax; x++)
            for (int y = bossRoom.yMin; y < bossRoom.yMax; y++)
                map[x, y] = 0;
    }

    /// <summary>
    /// 보스 방의 변에 입구를 만들고 가장 가까운 방과 연결합니다.
    /// </summary>
    private void ConnectBossRoomEntrances(List<Vector2Int> normalCenters)
    {
        if (normalCenters == null || normalCenters.Count == 0 || !HasBossRoom) return;

        var edgePoints = new List<Vector2Int>
        {
            new Vector2Int(bossRoom.xMin, (bossRoom.yMin + bossRoom.yMax) / 2),
            new Vector2Int(bossRoom.xMax - 1, (bossRoom.yMin + bossRoom.yMax) / 2),
            new Vector2Int((bossRoom.xMin + bossRoom.xMax) / 2, bossRoom.yMin),
            new Vector2Int((bossRoom.xMin + bossRoom.xMax) / 2, bossRoom.yMax - 1)
        };

        int half = Mathf.Max(1, corridorWidth / 2);
        int made = 0;

        foreach (var ep in edgePoints)
        {
            var nearest = normalCenters.OrderBy(c => Vector2Int.Distance(c, ep)).FirstOrDefault();

            Vector2Int dir = Vector2Int.zero;
            if (ep.x == bossRoom.xMin) dir = Vector2Int.left;
            else if (ep.x == bossRoom.xMax - 1) dir = Vector2Int.right;
            else if (ep.y == bossRoom.yMin) dir = Vector2Int.down;
            else if (ep.y == bossRoom.yMax - 1) dir = Vector2Int.up;

            for (int w = -half; w <= half; w++)
            {
                int x = ep.x + (dir.x == 0 ? w : 0);
                int y = ep.y + (dir.y == 0 ? w : 0);
                int dx = ep.x + dir.x;
                int dy = ep.y + dir.y;

                if (Inside(x, y)) map[x, y] = 0;
                if (Inside(dx, dy)) map[dx, dy] = 0;
            }

            DigCorridor(ep, nearest);
            made++;
        }

        if (made == 0)
        {
            var c = new Vector2Int(Mathf.RoundToInt(bossRoom.center.x), Mathf.RoundToInt(bossRoom.center.y));
            var nearest = normalCenters.OrderBy(v => Vector2Int.Distance(v, c)).First();
            DigCorridor(c, nearest);
        }
    }

    /// <summary>
    /// 플레이어와 가장 먼 방에 포탈을 배치합니다.
    /// </summary>
    private void PlacePortal()
    {
        if (portalPrefab == null || rooms == null || rooms.Count == 0) return;

        Vector2Int pc = new Vector2Int(Mathf.RoundToInt(playerRoom.center.x), Mathf.RoundToInt(playerRoom.center.y));

        RectInt farthestRoom = rooms
            .Where(r => !r.Overlaps(playerRoom))
            .OrderByDescending(r =>
            {
                var c = new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y));
                return Vector2Int.Distance(pc, c);
            })
            .FirstOrDefault();

        if (farthestRoom.width == 0 || farthestRoom.height == 0) return;

        Vector3 pos = new Vector3(farthestRoom.center.x, portalYOffset, farthestRoom.center.y);
        GameObject portal = Instantiate(portalPrefab, pos, Quaternion.identity, transform);

        Collider col = portal.GetComponent<Collider>();
        if (col == null) col = portal.AddComponent<BoxCollider>();
        col.isTrigger = true;

        PortalTrigger trigger = portal.GetComponent<PortalTrigger>();
        if (trigger == null) trigger = portal.AddComponent<PortalTrigger>();
        trigger.Setup(this);
    }

    /// <summary>
    /// 생성된 맵 데이터를 바탕으로 프리팹을 배치합니다.
    /// </summary>
    private void RenderMap()
    {
        float floorStep = 10f;
        var theme = GetActiveTheme();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (floorPrefab != null && x % (int)floorStep == 0 && y % (int)floorStep == 0)
                {
                    var floorPos = new Vector3(x + 5, 0f, y + 5);
                    var floor = Instantiate(floorPrefab, floorPos, Quaternion.identity, transform);
                    if (theme != null && theme.floorMaterial != null)
                    {
                        var rend = floor.GetComponentInChildren<Renderer>();
                        if (rend != null) rend.sharedMaterial = theme.floorMaterial;
                    }
                }

                if (map[x, y] == 1 && wallPrefab != null)
                {
                    float wallH = wallPrefab.transform.localScale.y;
                    var wallPos = new Vector3(x, wallH / 5f, y);
                    var wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                    if (theme != null && theme.wallMaterial != null)
                    {
                        var rend = wall.GetComponentInChildren<Renderer>();
                        if (rend != null) rend.sharedMaterial = theme.wallMaterial;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 주어진 좌표가 바닥인지 확인합니다.
    /// </summary>
    public bool IsFloor(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return map[x, y] == 0;
    }

    /// <summary>
    /// 생성된 일반 방 목록을 반환합니다.
    /// </summary>
    public List<RectInt> GetRooms()
    {
        if (HasBossRoom)
            return rooms.Where(r => !r.Overlaps(bossRoom)).ToList();
        return new List<RectInt>(rooms);
    }

    /// <summary>
    /// 생성된 보스 방 정보를 반환합니다.
    /// </summary>
    public RectInt GetBossRoom()
    {
        return bossRoom;
    }

    /// <summary>
    /// 플레이어 시작 방 정보를 반환합니다.
    /// </summary>
    public RectInt GetPlayerRoom()
    {
        return playerRoom;
    }

    /// <summary>
    /// 자식 객체를 정리하고 맵을 다시 생성합니다.
    /// </summary>
    public void ReloadMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        GenerateMap();
        RenderMap();
    }
}
