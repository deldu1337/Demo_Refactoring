using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;              // ★ min/maxStage 반영을 위한 리플렉션
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField] private BossProximityWatcher bossWatcher;

    [Header("Refs")]
    public TileMapGenerator mapGenerator;
    public StageManager stageManager;

    [Header("스폰 수 조절")]
    public float spawnFactor = 25f; // 방 면적 / spawnFactor = 적 수(최소 1)
    public int bossCount = 1;
    public int triesPerEnemy = 10;

    [Header("스폰 높이/충돌")]
    public float spawnY = 1f;
    public LayerMask obstacleMask;

    [Header("프리팹 매핑")]
    public List<EnemyPrefabPair> prefabPairs = new(); // 인스펙터에서 id ↔ prefab 연결
    private Dictionary<string, GameObject> prefabMap;

    // DB 캐시
    private EnemyDatabase db;

    [System.Serializable]
    public struct EnemyPrefabPair
    {
        public string id;
        public GameObject prefab;
    }

    void Awake()
    {
        prefabMap = prefabPairs
            .Where(p => !string.IsNullOrEmpty(p.id) && p.prefab != null)
            .GroupBy(p => p.id)
            .ToDictionary(g => g.Key, g => g.First().prefab);
    }

    void OnEnable()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("TileMapGenerator를 연결해주세요!");
            return;
        }
        mapGenerator.OnMapGenerated += GenerateEnemies;
    }

    void OnDisable()
    {
        if (mapGenerator != null)
            mapGenerator.OnMapGenerated -= GenerateEnemies;
    }

    // DB 로드 (한 번)
    private void EnsureDbLoaded()
    {
        if (db != null) return;
        TextAsset json = Resources.Load<TextAsset>("Datas/enemyData");
        if (json == null)
        {
            Debug.LogError("Resources/Datas/enemyData.json이 필요합니다!");
            db = new EnemyDatabase { enemies = new EnemyData[0] };
            return;
        }
        db = JsonUtility.FromJson<EnemyDatabase>(json.text);
        if (db.enemies == null) db.enemies = new EnemyData[0];
    }

    public void GenerateEnemies()
    {
        // 이전 스폰 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        EnsureDbLoaded();

        if (stageManager == null)
        {
            Debug.LogWarning("[EnemySpawn] StageManager 없음 → 스테이지 기반 다양성 미적용");
            return;
        }

        if (stageManager.IsBossStage()) SpawnBossStage();
        else SpawnNormalStage();
    }

    // ====== ★★ 핵심: 스테이지 범위 유틸 (JSON에 없으면 기본값 유도) ★★ ======
    private static readonly FieldInfo _fiMinStage = typeof(EnemyData).GetField("minStage");
    private static readonly FieldInfo _fiMaxStage = typeof(EnemyData).GetField("maxStage");

    private int GetMinStage(EnemyData e)
    {
        // JSON에 minStage 필드가 있으면 사용, 없으면 unlockStage 기반으로 유도
        if (_fiMinStage != null)
        {
            int v = (int)_fiMinStage.GetValue(e);
            if (v > 0) return v;
        }
        return Math.Max(1, e.unlockStage);
    }

    private int GetMaxStage(EnemyData e)
    {
        if (_fiMaxStage != null)
        {
            int v = (int)_fiMaxStage.GetValue(e);
            if (v > 0) return v;
        }
        return int.MaxValue;
    }

    private bool IsAvailableOnStage(EnemyData e, int stage, bool isBossStage)
    {
        if (e.isBoss != isBossStage) return false;
        // unlockStage는 여전히 하한으로 존중
        if (stage < Math.Max(1, e.unlockStage)) return false;
        int minS = GetMinStage(e);
        int maxS = GetMaxStage(e);
        return stage >= minS && stage <= maxS;
    }
    // ====================================================================

    void SpawnNormalStage()
    {
        int stage = stageManager.currentStage;

        // ★ unlockStage + (min/maxStage 범위) + 프리팹 매핑 체크
        var pool = db.enemies
            .Where(e => IsAvailableOnStage(e, stage, isBossStage: false) && prefabMap.ContainsKey(e.id))
            .ToList();

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawn] Stage {stage}에서 스폰 가능한 일반 몹이 없습니다.");
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

                // 가중치 랜덤 픽
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

    void SpawnBossStage()
    {
        int stage = stageManager.currentStage;

        // ★ 보스도 범위 체크
        var bosses = db.enemies
            .Where(e => IsAvailableOnStage(e, stage, isBossStage: true) && prefabMap.ContainsKey(e.id))
            .ToList();

        if (bosses.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawn] Stage {stage} 보스 풀 없음 → 일반 스폰으로 대체");
            SpawnNormalStage();
            return;
        }

        var br = mapGenerator.GetBossRoom();
        if (br.width <= 0 || br.height <= 0)
        {
            Debug.LogWarning("[EnemySpawn] 보스방 미정의 → 일반 스폰으로 대체");
            SpawnNormalStage();
            return;
        }

        Vector2Int c = new Vector2Int(Mathf.RoundToInt(br.center.x), Mathf.RoundToInt(br.center.y));
        Vector3 pos;
        if (mapGenerator.IsFloor(c.x, c.y)) pos = new Vector3(c.x, spawnY, c.y);
        else if (!TryPickPointInRoom(br, out pos)) pos = new Vector3(br.xMin + br.width / 2f, spawnY, br.yMin + br.height / 2f);

        //var boss = bosses[UnityEngine.Random.Range(0, bosses.Count)];
        var boss = bosses[UnityEngine.Random.Range(0, bosses.Count)];

        // Y축 180도 회전 후 스폰
        Quaternion bossRot = Quaternion.Euler(0f, 180f, 0f);
        GameObject go = SpawnById(boss.id, pos, markAsBoss: true, rotation: bossRot);

        // 보스 태그 부여
        //GameObject go = SpawnById(boss.id, pos, markAsBoss: true);
        if (go == null) { Debug.LogError("[EnemySpawn] 보스 스폰 실패"); return; }

        var esm = go.GetComponent<EnemyStatsManager>();
        if (esm == null) { Debug.LogError("[EnemySpawn] EnemyStatsManager 누락"); return; }

        if (bossWatcher == null) bossWatcher = FindAnyObjectByType<BossProximityWatcher>();
        if (bossWatcher != null) bossWatcher.SetBoss(esm);
    }

    // 반환형 GameObject 권장
    GameObject SpawnById(string enemyId, Vector3 position, bool markAsBoss = false, Quaternion? rotation = null)
    {
        if (!prefabMap.TryGetValue(enemyId, out var prefab))
        {
            Debug.LogWarning($"[EnemySpawn] '{enemyId}' 프리팹 매핑이 없습니다.");
            return null;
        }

        Quaternion rot = rotation ?? Quaternion.identity;   // 기본은 회전 없음
        var go = Instantiate(prefab, position, rot, transform);

        var esm = go.GetComponent<EnemyStatsManager>();
        if (esm != null) esm.enemyId = enemyId;

        var move = go.GetComponent<EnemyMove>();
        if (move != null) move.SetSpawnPosition(position);

        if (markAsBoss) go.tag = "Boss";

        return go;
    }


    bool TryPickPointInRoom(RectInt room, out Vector3 pos)
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
