using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    // 인스펙터에서 조절 가능
    [Range(0.1f, 0.9f)] public float minimumDevideRate = 0.45f;
    [Range(0.1f, 0.9f)] public float maximumDivideRate = 0.55f;
    [Range(1, 12)] public int maxDepth = 6;
    [Range(1, 9)] public int corridorWidth = 5;

    public GameObject portalPrefab;
    public float portalYOffset = 0f;
    public int bossRoomWidth = 28;
    public int bossRoomHeight = 28;

    private int[,] map;                 // 0 바닥 1 벽
    private List<RectInt> rooms;        // 리프에서 만든 방
    private RectInt playerRoom;         // 시작 방
    private RectInt bossRoom;           // 보스 방

    [Header("Stage Themes")]
    public int stagesPerTheme = 5;
    public StageTheme[] themes;
    public bool useBossOverrideTheme = true;
    public StageTheme bossOverrideTheme;

    public delegate void MapGeneratedHandler();
    public event MapGeneratedHandler OnMapGenerated;

    private bool HasBossRoom => bossRoom.width > 0 && bossRoom.height > 0;

    void Start()
    {
        GenerateMap();
        RenderMap();
    }

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

    public void GenerateMap()
    {
        InitMap();

        // 1 플레이어 방 먼저 캐브
        CarvePlayerRoom();

        // 2 BSP 분할
        Node root = new Node(new RectInt(1, 1, width - 2, height - 2));
        SplitRoom(root, 0);

        // 3 리프에서 방 생성 캐브
        GenerateRooms(root);

        // 4 보스 스테이지면 중앙에 보스 방 캐브
        bool isBoss = stageManager != null && stageManager.IsBossStage();
        if (isBoss) CarveBossRoomCenter();

        // 5 플레이어 방과 가장 가까운 일반 방을 반드시 연결
        ConnectPlayerRoomToNearestRoom();

        // 6 BSP 트리를 따라 형제 리프 센터 간 복도 생성
        GenerateTreeCorridors(root);

        // 7 보스방 입구 연결
        if (isBoss)
        {
            var centers = rooms.Where(r => !r.Overlaps(playerRoom) && !r.Overlaps(bossRoom))
                               .Select(r => new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y)))
                               .ToList();
            ConnectBossRoomEntrances(centers);
        }

        // 8 포탈 배치
        PlacePortal();

        OnMapGenerated?.Invoke();
    }

    void InitMap()
    {
        map = new int[width, height];
        rooms = new List<RectInt>();
        bossRoom = new RectInt(0, 0, 0, 0);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = 1;

        playerRoom = new RectInt(2, 2, 25, 25);
    }

    void CarvePlayerRoom()
    {
        // 테두리는 벽 유지 내부만 바닥
        for (int x = playerRoom.xMin + 1; x < playerRoom.xMax - 1; x++)
            for (int y = playerRoom.yMin + 1; y < playerRoom.yMax - 1; y++)
                map[x, y] = 0;
    }

    //bool CanSplit(RectInt r)
    //{
    //    return (r.width >= minRoomSize * 2) || (r.height >= minRoomSize * 2);
    //}

    // 더 분할 가능한지
    bool CanSplit(RectInt r)
    {
        const int minLeaf = 10;  // 리프 최소 폭/높이
        return (r.width >= minLeaf * 2) || (r.height >= minLeaf * 2);
    }

    // 분할만 수행, map은 건드리지 않음
    void SplitRoom(Node tree, int depth)
    {
        if (depth >= maxDepth || !CanSplit(tree.nodeRect))
            return;

        bool splitHoriz = tree.nodeRect.width >= tree.nodeRect.height;
        int axisLen = splitHoriz ? tree.nodeRect.width : tree.nodeRect.height;

        // 분할 구간을 1..axisLen-1로 강제
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

    RectInt GenerateRooms(Node node)
    {
        // 리프
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

            // 플레이어 방과 겹치면 제외
            if (!node.roomRect.Overlaps(playerRoom))
            {
                rooms.Add(node.roomRect);
                for (int x = node.roomRect.xMin; x < node.roomRect.xMax; x++)
                    for (int y = node.roomRect.yMin; y < node.roomRect.yMax; y++)
                        map[x, y] = 0;
            }

            return node.roomRect;
        }

        // 내부 노드면 자식 처리
        RectInt left = (node.leftNode != null) ? GenerateRooms(node.leftNode) : new RectInt();
        RectInt right = (node.rightNode != null) ? GenerateRooms(node.rightNode) : new RectInt();
        node.roomRect = (left.width > 0) ? left : right;
        return node.roomRect;
    }

    void ConnectPlayerRoomToNearestRoom()
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

        // 플레이어 방 상단 중앙에 문 뚫기
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

    void GenerateTreeCorridors(Node node)
    {
        if (node.leftNode == null || node.rightNode == null)
            return;

        Vector2Int a = node.leftNode.center;
        Vector2Int b = node.rightNode.center;
        DigCorridor(a, b);

        GenerateTreeCorridors(node.leftNode);
        GenerateTreeCorridors(node.rightNode);
    }

    void DigCorridor(Vector2Int a, Vector2Int b)
    {
        int half = Mathf.Max(1, corridorWidth / 2);

        // 수평
        int x0 = Mathf.Min(a.x, b.x);
        int x1 = Mathf.Max(a.x, b.x);
        for (int x = x0; x <= x1; x++)
            for (int w = -half; w <= half; w++)
            {
                int yy = a.y + w;
                if (Inside(x, yy)) map[x, yy] = 0;
            }

        // 수직
        int y0 = Mathf.Min(a.y, b.y);
        int y1 = Mathf.Max(a.y, b.y);
        for (int y = y0; y <= y1; y++)
            for (int w = -half; w <= half; w++)
            {
                int xx = b.x + w;
                if (Inside(xx, y)) map[xx, y] = 0;
            }
    }

    bool Inside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    void CarveBossRoomCenter()
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

    void ConnectBossRoomEntrances(List<Vector2Int> normalCenters)
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

    void PlacePortal()
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

    void RenderMap()
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

    public bool IsFloor(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return map[x, y] == 0;
    }

    public List<RectInt> GetRooms()
    {
        if (HasBossRoom)
            return rooms.Where(r => !r.Overlaps(bossRoom)).ToList();
        return new List<RectInt>(rooms);
    }

    public RectInt GetBossRoom() => bossRoom;
    public RectInt GetPlayerRoom() => playerRoom;

    public void ReloadMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        GenerateMap();
        RenderMap();
    }
}
