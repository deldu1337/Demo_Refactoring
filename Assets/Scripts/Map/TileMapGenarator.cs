//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class TileMapGenerator : MonoBehaviour
//{
//    [System.Serializable]
//    public class StageTheme
//    {
//        // 테마 이름. 디버깅과 구분용
//        public string name;
//        // 벽에 적용할 머티리얼
//        public Material wallMaterial;
//        // 바닥에 적용할 머티리얼
//        public Material floorMaterial;
//    }

//    // 현재 스테이지 번호와 보스 스테이지 여부를 알려주는 매니저
//    [SerializeField] private StageManager stageManager;

//    // 전체 맵 크기와 프리팹 및 생성 파라미터
//    public int width = 100;          // 맵 가로 셀 수
//    public int height = 100;         // 맵 세로 셀 수
//    public GameObject wallPrefab;    // 벽 프리팹. 1 x 1 크기 기준으로 배치
//    public GameObject floorPrefab;   // 바닥 프리팹. 10 x 10 크기 세그먼트로 배치
//    public int minRoomSize = 10;     // 방 최소 크기. BSP 분할 시 사용
//    public int maxRoomSize = 24;     // 방 최대 크기. BSP 분할 시 사용
//    public int maxDepth = 20;        // BSP 재귀 분할 최대 깊이
//    public int corridorWidth = 5;    // 통로 폭. 홀수 권장
//    public GameObject portalPrefab;  // 포탈 프리팹. 보통 스테이지 클리어 지점
//    public float portalYOffset = 0f; // 포탈의 Y 위치 보정값
//    public int bossRoomWidth = 28;   // 보스방 가로 크기. 보스 스테이지에서만 사용
//    public int bossRoomHeight = 28;  // 보스방 세로 크기. 보스 스테이지에서만 사용

//    // 내부 상태값들
//    private RectInt bossRoom;        // 보스 전용 방의 영역. 없으면 너비와 높이가 0
//    private int[,] map;              // 셀 기반 맵 데이터. 0은 바닥. 1은 벽
//    private List<RectInt> rooms;     // BSP로 생성된 일반 방 목록
//    private RectInt playerRoom;      // 플레이어 전용 시작 방

//    // 스테이지 테마 관련 설정
//    [Header("Stage Themes")]
//    public int stagesPerTheme = 5;           // 지정한 수의 스테이지마다 같은 테마를 사용
//    public StageTheme[] themes;              // 테마 배열
//    public bool useBossOverrideTheme = true; // 보스 스테이지에서 전용 테마 사용 여부
//    public StageTheme bossOverrideTheme;     // 보스 스테이지 전용 테마

//    // 현재 스테이지에 적용할 테마를 반환
//    private StageTheme GetActiveTheme()
//    {
//        // 보스 스테이지이고 전용 테마가 있다면 전용 테마를 우선 사용
//        if (useBossOverrideTheme && stageManager != null && stageManager.IsBossStage() && bossOverrideTheme != null)
//            return bossOverrideTheme;

//        // 일반 테마가 하나도 없다면 null 반환
//        if (themes == null || themes.Length == 0) return null;

//        // 현재 스테이지를 바탕으로 테마 인덱스를 계산
//        int stage = (stageManager != null) ? stageManager.currentStage : 1;
//        int idx = Mathf.FloorToInt((stage - 1) / Mathf.Max(1, stagesPerTheme));
//        idx = Mathf.Clamp(idx, 0, themes.Length - 1);
//        return themes[idx];
//    }

//    // 맵 생성 완료시 외부에 알리기 위한 이벤트. 스폰 등 후처리를 연결할 수 있다
//    public delegate void MapGeneratedHandler();
//    public event MapGeneratedHandler OnMapGenerated;

//    // 보스방이 유효하게 생성되었는지 판단하는 편의 속성
//    private bool HasBossRoom => bossRoom.width > 0 && bossRoom.height > 0;

//    // 유니티 라이프사이클. 시작 시 맵을 생성하고 렌더한다
//    void Start()
//    {
//        GenerateMap(); // 데이터 생성 단계
//        RenderMap();   // 프리팹 배치 단계
//    }

//    // 맵 데이터 생성 절차. 방 생성. 통로 생성. 보스방과 포탈 배치까지 담당
//    public void GenerateMap()
//    {
//        // 맵 배열과 컬렉션 초기화
//        map = new int[width, height];
//        rooms = new List<RectInt>();
//        bossRoom = new RectInt(0, 0, 0, 0);

//        // 전체를 벽으로 채운다
//        for (int x = 0; x < width; x++)
//            for (int y = 0; y < height; y++)
//                map[x, y] = 1;

//        // 플레이어 시작 방을 먼저 고정 생성한다
//        playerRoom = new RectInt(2, 2, 25, 25);

//        // BSP 분할로 일반 방 후보들을 만든다. 경계에서 한 칸 여유를 둔다
//        RectInt root = new RectInt(1, 1, width - 2, height - 2);
//        SplitRoom(root, maxDepth, rooms);

//        // 플레이어 방을 벽 테두리와 바닥 내부로 캐브한다
//        for (int x = playerRoom.xMin; x < playerRoom.xMax; x++)
//            for (int y = playerRoom.yMin; y < playerRoom.yMax; y++)
//                map[x, y] = 1; // 테두리 포함 우선 벽 처리
//        for (int x = playerRoom.xMin + 1; x < playerRoom.xMax - 1; x++)
//            for (int y = playerRoom.yMin + 1; y < playerRoom.yMax - 1; y++)
//                map[x, y] = 0; // 내부는 바닥 처리

//        // 일반 방들을 캐브한다. 플레이어 방과 겹치는 경우는 제외한다
//        foreach (var room in rooms)
//        {
//            if (!room.Overlaps(playerRoom))
//            {
//                for (int x = room.xMin; x < room.xMax; x++)
//                    for (int y = room.yMin; y < room.yMax; y++)
//                        map[x, y] = 0;
//            }
//        }

//        // 현재 스테이지가 보스 스테이지인지 확인한다
//        bool makeBossRoom = (stageManager != null && stageManager.IsBossStage());

//        // 보스 스테이지라면 맵 중앙에 보스방을 만들고 내부를 바닥으로 만든다
//        if (makeBossRoom)
//        {
//            int bw = Mathf.Clamp(bossRoomWidth, minRoomSize, width - 4);
//            int bh = Mathf.Clamp(bossRoomHeight, minRoomSize, height - 4);
//            int bx = (width - bw) / 2;
//            int by = (height - bh) / 2;
//            bossRoom = new RectInt(bx, by, bw, bh);

//            for (int x = bossRoom.xMin; x < bossRoom.xMax; x++)
//                for (int y = bossRoom.yMin; y < bossRoom.yMax; y++)
//                    map[x, y] = 0;
//        }

//        // 일반 방들의 중심 좌표 목록을 만든다. 플레이어 방과 보스방은 제외한다
//        List<Vector2Int> centers = rooms
//            .Where(r => !r.Overlaps(playerRoom) && !r.Overlaps(bossRoom))
//            .Select(r => new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y)))
//            .ToList();

//        // 최소 신장 트리 방식으로 일반 방들을 복도로 연결한다
//        List<Vector2Int> connected = new List<Vector2Int>();
//        List<Vector2Int> remaining = new List<Vector2Int>(centers);
//        if (remaining.Count > 0) { connected.Add(remaining[0]); remaining.RemoveAt(0); }

//        while (remaining.Count > 0)
//        {
//            float minDist = float.MaxValue; Vector2Int a = Vector2Int.zero, b = Vector2Int.zero;
//            foreach (var c in connected)
//                foreach (var r in remaining)
//                {
//                    float dist = Vector2Int.Distance(c, r);
//                    if (dist < minDist) { minDist = dist; a = c; b = r; }
//                }
//            CreateCorridor(a, b);
//            connected.Add(b);
//            remaining.Remove(b);
//        }

//        // 플레이어 방에서 가장 가까운 일반 방까지 전용 복도를 만든다
//        Vector2Int playerCenter = new Vector2Int(
//            Mathf.RoundToInt(playerRoom.center.x),
//            Mathf.RoundToInt(playerRoom.center.y)
//        );
//        if (centers.Count > 0)
//        {
//            Vector2Int nearestRoomCenter = centers.OrderBy(c => Vector2Int.Distance(c, playerCenter)).First();
//            Vector2Int corridorStart = new Vector2Int((playerRoom.xMin + playerRoom.xMax) / 2, playerRoom.yMax);

//            int offset = Mathf.Max(corridorWidth / 2, 1);
//            for (int w = -offset; w <= offset; w++)
//            {
//                int x = corridorStart.x + w, y = corridorStart.y;
//                if (x >= playerRoom.xMin && x < playerRoom.xMax && y < height)
//                {
//                    map[x, y] = 0; map[x, y - 1] = 0;
//                }
//            }
//            CreatePlayerCorridor(corridorStart, nearestRoomCenter);
//        }

//        // 보스 스테이지라면 보스방과 일반 방들을 연결하는 입구를 만든다
//        if (makeBossRoom)
//            ConnectBossRoomEntrances(centers);

//        // 포탈을 배치한다. 플레이어 방에서 가장 먼 일반 방의 중심에 둔다
//        PlacePortal();

//        // 맵 생성이 끝났음을 이벤트로 알린다
//        OnMapGenerated?.Invoke();
//    }

//    // 보스방 가장자리에서 가까운 일반 방으로 이어지는 입구와 복도를 만든다
//    void ConnectBossRoomEntrances(List<Vector2Int> normalCenters)
//    {
//        if (normalCenters == null || normalCenters.Count == 0) return;

//        // 보스방 네 변의 중앙 지점들을 후보로 잡는다
//        var edgePoints = new List<Vector2Int>
//        {
//            new Vector2Int(bossRoom.xMin, (bossRoom.yMin + bossRoom.yMax) / 2),
//            new Vector2Int(bossRoom.xMax - 1, (bossRoom.yMin + bossRoom.yMax) / 2),
//            new Vector2Int((bossRoom.xMin + bossRoom.xMax) / 2, bossRoom.yMin),
//            new Vector2Int((bossRoom.xMin + bossRoom.xMax) / 2, bossRoom.yMax - 1)
//        };

//        int made = 0;
//        foreach (var ep in edgePoints)
//        {
//            // 가장 가까운 일반 방 중심을 찾는다
//            var nearest = normalCenters.OrderBy(c => Vector2Int.Distance(c, ep)).FirstOrDefault();

//            // 입구 폭을 corridorWidth에 맞춰 넓힌다
//            int offset = Mathf.Max(corridorWidth / 2, 1);

//            // 보스방 밖으로 나가는 방향을 계산한다
//            Vector2Int dir = Vector2Int.zero;
//            if (ep.x == bossRoom.xMin) dir = Vector2Int.left;
//            else if (ep.x == bossRoom.xMax - 1) dir = Vector2Int.right;
//            else if (ep.y == bossRoom.yMin) dir = Vector2Int.down;
//            else if (ep.y == bossRoom.yMax - 1) dir = Vector2Int.up;

//            // 문을 캐브한다. 입구와 그 바깥 한 칸을 바닥으로 만든다
//            for (int w = -offset; w <= offset; w++)
//            {
//                int x = ep.x + (dir.x == 0 ? w : 0);
//                int y = ep.y + (dir.y == 0 ? w : 0);
//                int dx = ep.x + dir.x;
//                int dy = ep.y + dir.y;

//                if (x >= 0 && x < width && y >= 0 && y < height) map[x, y] = 0;
//                if (dx >= 0 && dx < width && dy >= 0 && dy < height) map[dx, dy] = 0;
//            }

//            // 입구에서 일반 방 중심까지 복도를 만든다
//            CreatePlayerCorridor(ep, nearest);
//            made++;
//        }

//        // 모든 변에서 실패한 경우를 대비하여 보스방 중앙에서 한번 더 연결한다
//        if (made == 0)
//        {
//            var bossC = new Vector2Int(Mathf.RoundToInt(bossRoom.center.x), Mathf.RoundToInt(bossRoom.center.y));
//            var nearest = normalCenters.OrderBy(c => Vector2Int.Distance(c, bossC)).First();
//            CreatePlayerCorridor(bossC, nearest);
//        }
//    }

//    // 포탈을 생성한다. 플레이어 방과 가장 멀리 떨어진 일반 방 중심에 둔다
//    void PlacePortal()
//    {
//        if (portalPrefab == null || rooms == null || rooms.Count == 0) return;

//        Vector2Int playerCenter = new Vector2Int(
//            Mathf.RoundToInt(playerRoom.center.x),
//            Mathf.RoundToInt(playerRoom.center.y)
//        );

//        RectInt farthestRoom = rooms
//            .Where(r => !r.Overlaps(playerRoom))
//            .OrderByDescending(r =>
//            {
//                var c = new Vector2Int(Mathf.RoundToInt(r.center.x), Mathf.RoundToInt(r.center.y));
//                return Vector2Int.Distance(playerCenter, c);
//            })
//            .FirstOrDefault();

//        if (farthestRoom.width == 0 || farthestRoom.height == 0) return;

//        Vector3 portalPos = new Vector3(farthestRoom.center.x, portalYOffset, farthestRoom.center.y);

//        GameObject portal = Instantiate(portalPrefab, portalPos, Quaternion.identity, transform);

//        Collider col = portal.GetComponent<Collider>();
//        if (col == null) col = portal.AddComponent<BoxCollider>();
//        col.isTrigger = true;

//        PortalTrigger trigger = portal.GetComponent<PortalTrigger>();
//        if (trigger == null) trigger = portal.AddComponent<PortalTrigger>();
//        trigger.Setup(this);
//    }

//    // BSP 분할. 공간을 쪼개며 방을 만든다
//    void SplitRoom(RectInt space, int depth, List<RectInt> rooms)
//    {
//        // 더 쪼갤 수 없거나 공간이 너무 작다면 이 공간 안에서 하나의 방을 만든다
//        if (depth == 0 || space.width < minRoomSize * 2 || space.height < minRoomSize * 2)
//        {
//            int roomWidth = Random.Range(minRoomSize, Mathf.Min(space.width, maxRoomSize));
//            int roomHeight = Random.Range(minRoomSize, Mathf.Min(space.height, maxRoomSize));
//            int roomX = Random.Range(space.xMin + 1, space.xMax - roomWidth - 1);
//            int roomY = Random.Range(space.yMin + 1, space.yMax - roomHeight - 1);
//            RectInt newRoom = new RectInt(roomX, roomY, roomWidth, roomHeight);

//            float minDistanceFromPlayer = 8f; // 플레이어 방과의 최소 거리
//            Vector2Int newCenter = new Vector2Int(Mathf.RoundToInt(newRoom.center.x), Mathf.RoundToInt(newRoom.center.y));
//            Vector2Int playerCenter = new Vector2Int(Mathf.RoundToInt(playerRoom.center.x), Mathf.RoundToInt(playerRoom.center.y));

//            if (!newRoom.Overlaps(playerRoom) && Vector2Int.Distance(newCenter, playerCenter) > minDistanceFromPlayer)
//            {
//                rooms.Add(newRoom);
//            }
//            return;
//        }

//        // 수평 또는 수직으로 랜덤 분할한다
//        bool splitHorizontally = Random.value > 0.5f;
//        if (splitHorizontally)
//        {
//            int splitY = Random.Range(space.yMin + minRoomSize, space.yMax - minRoomSize);
//            SplitRoom(new RectInt(space.xMin, space.yMin, space.width, splitY - space.yMin), depth - 1, rooms);
//            SplitRoom(new RectInt(space.xMin, splitY, space.width, space.yMax - splitY), depth - 1, rooms);
//        }
//        else
//        {
//            int splitX = Random.Range(space.xMin + minRoomSize, space.xMax - minRoomSize);
//            SplitRoom(new RectInt(space.xMin, space.yMin, splitX - space.xMin, space.height), depth - 1, rooms);
//            SplitRoom(new RectInt(splitX, space.yMin, space.xMax - splitX, space.height), depth - 1, rooms);
//        }
//    }

//    // 두 방 중심을 축을 나눠 지그재그로 연결하는 복도를 만든다
//    void CreateCorridor(Vector2Int a, Vector2Int b)
//    {
//        if (Random.value > 0.5f)
//        {
//            DrawHorizontalCorridor(a.x, b.x, a.y);
//            DrawVerticalCorridor(a.y, b.y, b.x);
//        }
//        else
//        {
//            DrawVerticalCorridor(a.y, b.y, a.x);
//            DrawHorizontalCorridor(a.x, b.x, b.y);
//        }
//    }

//    // 플레이어 방에서 특정 방까지 비교적 자연스러운 경로를 만든다. 중간 지점을 약간 랜덤하게 둔다
//    void CreatePlayerCorridor(Vector2Int start, Vector2Int end)
//    {
//        int offset = Mathf.Max(corridorWidth / 2, 1);

//        Vector2Int mid = new Vector2Int(
//            (start.x + end.x) / 2 + Random.Range(-2, 3),
//            (start.y + end.y) / 2 + Random.Range(-2, 3)
//        );

//        for (int w = -offset; w <= offset; w++)
//            DigCorridor(new Vector2Int(start.x + w, start.y), new Vector2Int(mid.x + w, mid.y));

//        for (int w = -offset; w <= offset; w++)
//            DigCorridor(new Vector2Int(mid.x + w, mid.y), new Vector2Int(end.x + w, end.y));
//    }

//    // 두 점을 직선으로 잇는 복도를 단위 셀로 파낸다
//    void DigCorridor(Vector2Int start, Vector2Int end)
//    {
//        Vector2Int current = start;

//        int xDir = (end.x > current.x) ? 1 : -1;
//        while (current.x != end.x)
//        {
//            current.x += xDir;
//            for (int w = -1; w <= 1; w++)
//            {
//                int yy = current.y + w;
//                if (!playerRoom.Contains(new Vector2Int(current.x, yy)))
//                    map[current.x, yy] = 0;
//            }
//        }

//        int yDir = (end.y > current.y) ? 1 : -1;
//        while (current.y != end.y)
//        {
//            current.y += yDir;
//            for (int w = -1; w <= 1; w++)
//            {
//                int xx = current.x + w;
//                if (!playerRoom.Contains(new Vector2Int(xx, current.y)))
//                    map[xx, current.y] = 0;
//            }
//        }
//    }

//    // 수평 복도를 일정 폭으로 파낸다
//    void DrawHorizontalCorridor(int xStart, int xEnd, int y)
//    {
//        int offset = corridorWidth / 2;
//        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
//            for (int w = -offset; w <= offset; w++)
//            {
//                int yy = y + w;
//                if (yy >= 0 && yy < height && !playerRoom.Contains(new Vector2Int(x, yy)))
//                    map[x, yy] = 0;
//            }
//    }

//    // 수직 복도를 일정 폭으로 파낸다
//    void DrawVerticalCorridor(int yStart, int yEnd, int x)
//    {
//        int offset = corridorWidth / 2;
//        for (int y = Mathf.Min(yStart, yEnd); y <= Mathf.Max(yStart, yEnd); y++)
//            for (int w = -offset; w <= offset; w++)
//            {
//                int xx = x + w;
//                if (xx >= 0 && xx < width && !playerRoom.Contains(new Vector2Int(xx, y)))
//                    map[xx, y] = 0;
//            }
//    }

//    // 맵 데이터를 실제 게임 오브젝트로 렌더링한다. 바닥 타일과 벽 타일을 배치하고 테마 머티리얼을 적용한다
//    void RenderMap()
//    {
//        float floorSize = 10f;
//        var theme = GetActiveTheme();

//        for (int x = 0; x < width; x++)
//        {
//            for (int y = 0; y < height; y++)
//            {
//                // 바닥 타일은 10 간격으로 배치하여 과도한 인스턴스를 줄인다
//                if (x % (int)floorSize == 0 && y % (int)floorSize == 0)
//                {
//                    Vector3 floorPos = new Vector3(x + 5, 0, y + 5);
//                    var floor = Instantiate(floorPrefab, floorPos, Quaternion.identity, transform);

//                    // 테마 바닥 머티리얼 적용
//                    if (theme != null && theme.floorMaterial != null)
//                    {
//                        var rend = floor.GetComponentInChildren<Renderer>();
//                        if (rend != null) rend.sharedMaterial = theme.floorMaterial;
//                    }
//                }

//                // 맵 데이터가 벽인 셀에는 벽 프리팹을 1 x 1로 배치한다
//                if (map[x, y] == 1)
//                {
//                    float wallHeight = wallPrefab.transform.localScale.y;
//                    Vector3 wallPos = new Vector3(x, wallHeight / 5f, y);
//                    var wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);

//                    // 테마 벽 머티리얼 적용
//                    if (theme != null && theme.wallMaterial != null)
//                    {
//                        var rend = wall.GetComponentInChildren<Renderer>();
//                        if (rend != null) rend.sharedMaterial = theme.wallMaterial;
//                    }
//                }
//            }
//        }
//    }

//    // 지정 좌표가 맵 내부이고 바닥인지 확인한다
//    public bool IsFloor(int x, int y)
//    {
//        if (x < 0 || x >= width || y < 0 || y >= height) return false;
//        return map[x, y] == 0;
//    }

//    // 일반 방 목록을 반환한다. 보스방이 있을 경우 겹치는 방은 제외한다
//    public List<RectInt> GetRooms()
//    {
//        if (HasBossRoom)
//            return rooms.Where(r => r != playerRoom && !r.Overlaps(bossRoom)).ToList();
//        else
//            return rooms.Where(r => r != playerRoom).ToList();
//    }

//    // 보스방 영역 반환
//    public RectInt GetBossRoom() => bossRoom;

//    // 플레이어 방 영역 반환
//    public RectInt GetPlayerRoom() => playerRoom;

//    // 맵을 다시 만든다. 기존 자식 오브젝트를 제거하고 데이터 생성과 렌더를 다시 수행한다
//    public void ReloadMap()
//    {
//        for (int i = transform.childCount - 1; i >= 0; i--)
//            Destroy(transform.GetChild(i).gameObject);

//        GenerateMap();
//        RenderMap();
//    }
//}
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

    //void SplitRoom(Node node, int depth)
    //{
    //    if (depth >= maxDepth || !CanSplit(node.nodeRect))
    //        return;

    //    bool splitHoriz = node.nodeRect.width >= node.nodeRect.height;
    //    if (splitHoriz)
    //    {
    //        int minY = node.nodeRect.yMin + minRoomSize;
    //        int maxY = node.nodeRect.yMax - minRoomSize;
    //        if (minY >= maxY) return;

    //        int splitY = Random.Range(minY, maxY);
    //        node.leftNode = new Node(new RectInt(node.nodeRect.xMin, node.nodeRect.yMin, node.nodeRect.width, splitY - node.nodeRect.yMin));
    //        node.rightNode = new Node(new RectInt(node.nodeRect.xMin, splitY, node.nodeRect.width, node.nodeRect.yMax - splitY));
    //    }
    //    else
    //    {
    //        int minX = node.nodeRect.xMin + minRoomSize;
    //        int maxX = node.nodeRect.xMax - minRoomSize;
    //        if (minX >= maxX) return;

    //        int splitX = Random.Range(minX, maxX);
    //        node.leftNode = new Node(new RectInt(node.nodeRect.xMin, node.nodeRect.yMin, splitX - node.nodeRect.xMin, node.nodeRect.height));
    //        node.rightNode = new Node(new RectInt(splitX, node.nodeRect.yMin, node.nodeRect.xMax - splitX, node.nodeRect.height));
    //    }

    //    node.leftNode.parNode = node;
    //    node.rightNode.parNode = node;

    //    SplitRoom(node.leftNode, depth + 1);
    //    SplitRoom(node.rightNode, depth + 1);
    //}

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
