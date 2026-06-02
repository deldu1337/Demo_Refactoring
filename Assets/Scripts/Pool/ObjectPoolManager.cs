using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    /// <summary>
    /// 풀에서 꺼내져 다시 사용되기 직전에 호출됩니다.
    /// </summary>
    void OnSpawnedFromPool();

    /// <summary>
    /// 사용이 끝나 풀로 반환되기 직전에 호출됩니다.
    /// </summary>
    void OnReturnedToPool();
}

/// <summary>
/// 프리팹별 오브젝트 풀을 관리하는 공용 매니저입니다.
/// 반복 생성/파괴 비용을 줄이기 위해 비활성 오브젝트를 보관하고 재사용합니다.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [SerializeField] private int defaultPrewarmCount = 0;
    [SerializeField] private bool createPoolRootUnderManager = true;

    // 프리팹을 기준으로 반환된 오브젝트들을 보관합니다.
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    // 반환 시 어떤 풀로 돌려보낼지 알 수 있도록 인스턴스와 원본 프리팹을 매핑합니다.
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    // IPoolable 탐색 비용을 줄이기 위해 인스턴스 생성 시 컴포넌트 목록을 캐싱합니다.
    private readonly Dictionary<GameObject, IPoolable[]> instancePoolables = new();
    private Transform poolRoot;

    /// <summary>
    /// 싱글턴 인스턴스를 설정하고, 비활성 오브젝트를 모아둘 루트를 준비합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsurePoolRoot();
    }

    /// <summary>
    /// 매니저가 파괴될 때 싱글턴 참조를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 씬에 풀 매니저가 없으면 런타임에 생성하고, 있으면 기존 인스턴스를 반환합니다.
    /// </summary>
    public static ObjectPoolManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        var go = new GameObject(nameof(ObjectPoolManager));
        return go.AddComponent<ObjectPoolManager>();
    }

    /// <summary>
    /// 지정한 프리팹을 미리 생성해 풀에 넣어 둡니다.
    /// 전투 시작 후 첫 생성 스파이크를 줄이기 위한 준비 단계입니다.
    /// </summary>
    public void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;

        EnsurePool(prefab);
        for (int i = 0; i < count; i++)
        {
            var instance = CreateInstance(prefab, parent);
            instance.SetActive(false);

            // 생성 직후 바로 풀 루트로 이동시켜 비활성 대기 상태로 만듭니다.
            ReturnToPool(instance);
        }
    }

    /// <summary>
    /// 프리팹에 해당하는 오브젝트를 풀에서 꺼내거나, 풀이 비어 있으면 새로 생성합니다.
    /// </summary>
    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        EnsurePool(prefab);

        var queue = pools[prefab];

        // 풀에 남은 오브젝트가 있으면 재사용하고, 부족할 때만 새로 생성합니다.
        var instance = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab, parent);

        if (parent != null) instance.transform.SetParent(parent, false);

        // 재사용 전 대상 오브젝트가 이전 상태를 초기화할 수 있도록 알립니다.
        foreach (var poolable in GetPoolables(instance))
            poolable.OnSpawnedFromPool();

        instanceToPrefab[instance] = prefab;
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// 컴포넌트 프리팹을 받아 같은 타입의 컴포넌트를 반환하는 편의 메서드입니다.
    /// </summary>
    public T Get<T>(T prefab, Transform parent = null) where T : Component
    {
        if (prefab == null) return null;

        var instance = Get(prefab.gameObject, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    /// <summary>
    /// 오브젝트를 풀에서 꺼낸 뒤 위치와 회전을 함께 설정합니다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var instance = Get(prefab, parent);
        if (instance == null) return null;

        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 비활성화하고 원래 프리팹 풀로 반환합니다.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            // 풀에서 만든 오브젝트가 아니면 안전하게 파괴합니다.
            Destroy(instance);
            return;
        }

        // 반환 전 대상 오브젝트가 참조나 상태를 정리할 수 있도록 알립니다.
        foreach (var poolable in GetPoolables(instance))
            poolable.OnReturnedToPool();

        instance.SetActive(false);
        ReturnToPool(instance);
    }

    /// <summary>
    /// 해당 프리팹 풀에 대기 중인 비활성 오브젝트 수를 반환합니다.
    /// 커스텀 에디터 테스트 툴에서 풀 상태를 확인할 때 사용합니다.
    /// </summary>
    public int GetInactiveCount(GameObject prefab)
    {
        if (prefab == null) return 0;
        return pools.TryGetValue(prefab, out var queue) ? queue.Count : 0;
    }

    /// <summary>
    /// 해당 프리팹으로 생성되어 매니저가 추적 중인 전체 오브젝트 수를 반환합니다.
    /// </summary>
    public int GetTrackedCount(GameObject prefab)
    {
        if (prefab == null) return 0;

        int count = 0;
        foreach (var trackedPrefab in instanceToPrefab.Values)
        {
            if (trackedPrefab == prefab)
                count++;
        }

        return count;
    }

    /// <summary>
    /// 추적 중인 전체 수에서 비활성 대기 수를 뺀 활성 오브젝트 수를 반환합니다.
    /// </summary>
    public int GetActiveCount(GameObject prefab)
    {
        return Mathf.Max(0, GetTrackedCount(prefab) - GetInactiveCount(prefab));
    }

    /// <summary>
    /// 프리팹에 해당하는 큐가 없으면 새로 생성합니다.
    /// </summary>
    private void EnsurePool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();
    }

    /// <summary>
    /// 원본 프리팹으로 새 인스턴스를 생성하고 프리팹 매핑 정보와 IPoolable 캐시를 등록합니다.
    /// </summary>
    private GameObject CreateInstance(GameObject prefab, Transform parent)
    {
        var instance = Instantiate(prefab, parent);
        instanceToPrefab[instance] = prefab;

        // Get/Release 때마다 GetComponentsInChildren을 반복 호출하지 않도록 한 번만 캐싱합니다.
        instancePoolables[instance] = instance.GetComponentsInChildren<IPoolable>(true);
        return instance;
    }

    /// <summary>
    /// 캐싱된 IPoolable 목록을 반환하고, 캐시가 없으면 한 번만 다시 검색합니다.
    /// </summary>
    private IPoolable[] GetPoolables(GameObject instance)
    {
        if (!instancePoolables.TryGetValue(instance, out var poolables) || poolables == null)
        {
            poolables = instance.GetComponentsInChildren<IPoolable>(true);
            instancePoolables[instance] = poolables;
        }

        return poolables;
    }

    /// <summary>
    /// 비활성 오브젝트를 풀 루트 하위로 이동시킨 뒤 해당 프리팹 큐에 넣습니다.
    /// </summary>
    private void ReturnToPool(GameObject instance)
    {
        if (!instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Destroy(instance);
            return;
        }

        EnsurePoolRoot();

        // Hierarchy에서 사용 중인 오브젝트와 대기 중인 오브젝트를 구분하기 위해 루트를 분리합니다.
        if (poolRoot != null) instance.transform.SetParent(poolRoot, false);
        pools[prefab].Enqueue(instance);
    }

    /// <summary>
    /// 풀에 반환된 오브젝트를 모아둘 "Pooled Objects" 루트를 생성합니다.
    /// </summary>
    private void EnsurePoolRoot()
    {
        if (!createPoolRootUnderManager || poolRoot != null) return;

        var root = new GameObject("Pooled Objects");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;
    }
}
