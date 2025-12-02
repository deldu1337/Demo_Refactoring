using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 스테이지 정보에 따라 적 프리팹을 선택하고 맵에 배치한다.
/// </summary>
public class EnemySpawn : MonoBehaviour
{
    [SerializeField] private BossProximityWatcher bossWatcher;

    [Header("참조 설정")]
    public TileMapGenerator mapGenerator;
    public StageManager stageManager;

    [Header("스폰 설정")]
    public float spawnFactor = 25f; // 방 면적을 나눈 값으로 적 수를 결정
    public int bossCount = 1;
    public int triesPerEnemy = 10;

    [Header("배치 옵션")]
    public float spawnY = 1f;
    public LayerMask obstacleMask;

    [Header("프리팹 매핑")]
    public List<EnemyPrefabPair> prefabPairs = new();
    private Dictionary<string, GameObject> prefabMap;

    private EnemyDatabase db;

    [System.Serializable]
    public struct EnemyPrefabPair
    {
        public string id;
        public GameObject prefab;
    }

    /// <summary>
    /// 유효한 프리팹 목록을 정리해 빠르게 조회할 수 있도록 매핑한다.
    /// </summary>
    private void Awake()
    {
        prefabMap = prefabPairs
            .Where(p => !string.IsNullOrEmpty(p.id) && p.prefab != null)
            .GroupBy(p => p.id)
            .ToDictionary(g => g.Key, g => g.First().prefab);
    }

    /// <summary>
    /// 맵 생성 이벤트를 구독하여 맵 완성 시 적을 생성한다.
    /// </summary>
    private void OnEnable()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("TileMapGenerator가 설정되지 않았습니다!");
            return;
        }
        mapGenerator.OnMapGenerated += GenerateEnemies;
    }

    /// <summary>
    /// 맵 생성 이벤트 구독을 해제한다.
    /// </summary>
    private void OnDisable()
    {
        if (mapGenerator != null)
            mapGenerator.OnMapGenerated -= GenerateEnemies;
    }

    /// <summary>
    /// 적 데이터베이스를 로드해 메모리에 보관한다.
    /// </summary>
    private void EnsureDbLoaded()
    {
        if (db != null) return;
        TextAsset json = Resources.Load<TextAsset>("Datas/enemyData");
        if (json == null)
        {
            Debug.LogError("Resources/Datas/enemyData.json을 찾을 수 없습니다!");
            db = new EnemyDatabase { enemies = new EnemyData[0] };
            return;
        }
        db = JsonUtility.FromJson<EnemyDatabase>(json.text);
        if (db.enemies == null) db.enemies = new EnemyData[0];
    }

    /// <summary>
    /// 기존 스폰을 정리한 뒤 현재 스테이지에 맞는 적을 배치한다.
    /// </summary>
    public void GenerateEnemies()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        EnsureDbLoaded();

        if (stageManager == null)
        {
            Debug.LogWarning("[EnemySpawn] StageManager가 설정되지 않아 적을 스폰하지 않습니다.");
            return;
        }

        if (stageManager.IsBossStage()) SpawnBossStage();
        else SpawnNormalStage();
    }

    private static readonly FieldInfo _fiMinStage = typeof(EnemyData).GetField("minStage");
    private static readonly FieldInfo _fiMaxStage = typeof(EnemyData).GetField("maxStage");

    /// <summary>
    /// 데이터의 최소 출현 스테이지를 가져온다.
    /// </summary>
    private int GetMinStage(EnemyData e)
    {
        if (_fiMinStage != null)
        {
            int v = (int)_fiMinStage.GetValue(e);
            if (v > 0) return v;
        }
        return Math.Max(1, e.unlockStage);
    }

    /// <summary>
    /// 데이터의 최대 출현 스테이지를 가져온다.
    /// </summary>
    private int GetMaxStage(EnemyData e)
    {
        if (_fiMaxStage != null)
        {
            int v = (int)_fiMaxStage.GetValue(e);
            if (v > 0) return v;
        }
        return int.MaxValue;
    }

    /// <summary>
    /// 현재 스테이지와 보스 여부에 맞는 적인지 판별한다.
    /// </summary>
    private bool IsAvailableOnStage(EnemyData e, int stage, bool isBossStage)
    {
        if (e.isBoss != isBossStage) return false;
        if (stage < Math.Max(1, e.unlockStage)) return false;
        int minS = GetMinStage(e);
        int maxS = GetMaxStage(e);
        return stage >= minS && stage <= maxS;
    }

    /// <summary>
    /// 일반 스테이지에서 방 면적을 기준으로 적을 무작위 배치한다.
    /// </summary>
    private void SpawnNormalStage()
    {
        int stage = stageManager.currentStage;

        var pool = db.enemies
            .Where(e => IsAvailableOnStage(e, stage, isBossStage: false) && prefabMap.ContainsKey(e.id))
            .ToList();

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawn] Stage {stage}에 스폰 가능한 적이 없습니다.");
            return;
        }

        float totalWeight = pool.Sum(e => Mathf.Max(0.0001f, e.weight));

        foreach (var room in mapGenerator.GetRooms())
        {
            int roomArea = room.width * room.height;
            int enemyCount = Mathf.Max(1, Mathf.RoundToInt(roomArea / spawnFactor));

            int spawned = 0, tries = 0;
            while (spawned < enemyCount && tries < enemyCount * triesPerEnemy)
            {
                tries++;
                if (!TryPickPointInRoom(room, out Vector3 pos)) continue;

                float pick = UnityEngine.Random.value * totalWeight;
                EnemyData chosen = null;
                float acc = 0f;
                foreach (var e in pool)
                {
                    acc += Mathf.Max(0.0001f, e.weight);
                    if (pick <= acc) { chosen = e; break; }
                }
                if (chosen == null) chosen = pool[pool.Count - 1];

                SpawnById(chosen.id, pos);
                spawned++;
            }
        }
    }

    /// <summary>
    /// 보스 스테이지에서 보스 룸을 기준으로 적을 스폰한다.
    /// </summary>
    private void SpawnBossStage()
    {
        int stage = stageManager.currentStage;

        var bosses = db.enemies
            .Where(e => IsAvailableOnStage(e, stage, isBossStage: true) && prefabMap.ContainsKey(e.id))
            .ToList();

        if (bosses.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawn] Stage {stage}에 배치할 보스가 없어 일반 스폰을 진행합니다.");
            SpawnNormalStage();
            return;
        }

        var br = mapGenerator.GetBossRoom();
        if (br.width <= 0 || br.height <= 0)
        {
            Debug.LogWarning("[EnemySpawn] 보스 룸이 없어 일반 스폰을 진행합니다.");
            SpawnNormalStage();
            return;
        }

        Vector2Int c = new Vector2Int(Mathf.RoundToInt(br.center.x), Mathf.RoundToInt(br.center.y));
        Vector3 pos;
        if (mapGenerator.IsFloor(c.x, c.y)) pos = new Vector3(c.x, spawnY, c.y);
        else if (!TryPickPointInRoom(br, out pos)) pos = new Vector3(br.xMin + br.width / 2f, spawnY, br.yMin + br.height / 2f);

        var boss = bosses[UnityEngine.Random.Range(0, bosses.Count)];

        Quaternion bossRot = Quaternion.Euler(0f, 180f, 0f);
        GameObject go = SpawnById(boss.id, pos, markAsBoss: true, rotation: bossRot);

        if (go == null) { Debug.LogError("[EnemySpawn] 보스 오브젝트 생성에 실패했습니다."); return; }

        var esm = go.GetComponent<EnemyStatsManager>();
        if (esm == null) { Debug.LogError("[EnemySpawn] EnemyStatsManager를 찾을 수 없습니다."); return; }

        if (bossWatcher == null) bossWatcher = FindAnyObjectByType<BossProximityWatcher>();
        if (bossWatcher != null) bossWatcher.SetBoss(esm);
    }

    /// <summary>
    /// ID에 해당하는 프리팹을 생성하고 스폰 결과를 반환한다.
    /// </summary>
    private GameObject SpawnById(string enemyId, Vector3 position, bool markAsBoss = false, Quaternion? rotation = null)
    {
        if (!prefabMap.TryGetValue(enemyId, out var prefab))
        {
            Debug.LogWarning($"[EnemySpawn] '{enemyId}' 프리팹이 없습니다.");
            return null;
        }

        Quaternion rot = rotation ?? Quaternion.identity;
        var go = Instantiate(prefab, position, rot, transform);

        var esm = go.GetComponent<EnemyStatsManager>();
        if (esm != null) esm.enemyId = enemyId;

        var move = go.GetComponent<EnemyMove>();
        if (move != null) move.SetSpawnPosition(position);

        if (markAsBoss) go.tag = "Boss";

        return go;
    }

    /// <summary>
    /// 지정된 방 안에서 장애물이 없는 스폰 지점을 찾는다.
    /// </summary>
    private bool TryPickPointInRoom(RectInt room, out Vector3 pos)
    {
        for (int t = 0; t < triesPerEnemy; t++)
        {
            int x = UnityEngine.Random.Range(room.xMin + 1, room.xMax - 1);
            int z = UnityEngine.Random.Range(room.yMin + 1, room.yMax - 1);

            if (!mapGenerator.IsFloor(x, z)) continue;

            var pr = mapGenerator.GetPlayerRoom();
            if (pr.Contains(new Vector2Int(x, z))) continue;

            Vector3 candidate = new Vector3(x, spawnY, z);

            if (obstacleMask.value != 0 &&
                Physics.CheckSphere(candidate, 0.4f, obstacleMask))
                continue;

            pos = candidate;
            return true;
        }
        pos = default;
        return false;
    }
}
