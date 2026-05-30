using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [SerializeField] private int defaultPrewarmCount = 0;
    [SerializeField] private bool createPoolRootUnderManager = true;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();
    private readonly Dictionary<GameObject, IPoolable[]> instancePoolables = new();
    private Transform poolRoot;

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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static ObjectPoolManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        var go = new GameObject(nameof(ObjectPoolManager));
        return go.AddComponent<ObjectPoolManager>();
    }

    public void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;

        EnsurePool(prefab);
        for (int i = 0; i < count; i++)
        {
            var instance = CreateInstance(prefab, parent);
            instance.SetActive(false);
            ReturnToPool(instance);
        }
    }

    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        EnsurePool(prefab);

        var queue = pools[prefab];
        var instance = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab, parent);

        if (parent != null) instance.transform.SetParent(parent, false);

        foreach (var poolable in GetPoolables(instance))
            poolable.OnSpawnedFromPool();

        instanceToPrefab[instance] = prefab;
        instance.SetActive(true);

        return instance;
    }

    public T Get<T>(T prefab, Transform parent = null) where T : Component
    {
        if (prefab == null) return null;

        var instance = Get(prefab.gameObject, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var instance = Get(prefab, parent);
        if (instance == null) return null;

        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Destroy(instance);
            return;
        }

        foreach (var poolable in GetPoolables(instance))
            poolable.OnReturnedToPool();

        instance.SetActive(false);
        ReturnToPool(instance);
    }

    public int GetInactiveCount(GameObject prefab)
    {
        if (prefab == null) return 0;
        return pools.TryGetValue(prefab, out var queue) ? queue.Count : 0;
    }

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

    public int GetActiveCount(GameObject prefab)
    {
        return Mathf.Max(0, GetTrackedCount(prefab) - GetInactiveCount(prefab));
    }

    private void EnsurePool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();
    }

    private GameObject CreateInstance(GameObject prefab, Transform parent)
    {
        var instance = Instantiate(prefab, parent);
        instanceToPrefab[instance] = prefab;
        instancePoolables[instance] = instance.GetComponentsInChildren<IPoolable>(true);
        return instance;
    }

    private IPoolable[] GetPoolables(GameObject instance)
    {
        if (!instancePoolables.TryGetValue(instance, out var poolables) || poolables == null)
        {
            poolables = instance.GetComponentsInChildren<IPoolable>(true);
            instancePoolables[instance] = poolables;
        }

        return poolables;
    }

    private void ReturnToPool(GameObject instance)
    {
        if (!instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Destroy(instance);
            return;
        }

        EnsurePoolRoot();
        if (poolRoot != null) instance.transform.SetParent(poolRoot, false);
        pools[prefab].Enqueue(instance);
    }

    private void EnsurePoolRoot()
    {
        if (!createPoolRootUnderManager || poolRoot != null) return;

        var root = new GameObject("Pooled Objects");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;
    }
}
