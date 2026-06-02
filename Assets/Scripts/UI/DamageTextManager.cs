using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    public GameObject damageTextPrefab;
    public Canvas canvas;
    [SerializeField] private int prewarmCount = 20;

    private ObjectPoolManager poolManager;

    /// <summary>
    /// 싱글턴 인스턴스를 설정하고 데미지 텍스트 풀을 미리 준비합니다.
    /// </summary>
    void Awake()
    {
        Instance = this;
        poolManager = ObjectPoolManager.GetOrCreate();

        // 전투 중 첫 데미지 표시 시점에 대량 생성이 몰리지 않도록 미리 생성합니다.
        if (damageTextPrefab != null)
            poolManager.Prewarm(damageTextPrefab, prewarmCount, canvas != null ? canvas.transform : null);
    }

    public enum DamageTextTarget
    {
        Enemy,
        Player
    }

    /// <summary>
    /// 대상 Transform을 따라가는 데미지 텍스트를 표시합니다.
    /// </summary>
    public void ShowDamage(Transform target, int damage, Color color, DamageTextTarget type)
    {
        if (!target || damageTextPrefab == null || canvas == null) return;

        Vector3 worldOffset = Vector3.up * 1.5f;

        // Instantiate 대신 풀에서 꺼내 DamageCanvas 하위에 배치합니다.
        GameObject go = poolManager.Get(damageTextPrefab, canvas.transform);

        var dt = go.GetComponent<DamageText>();
        if (dt != null)
            dt.Setup(damage, color, target, worldOffset, Camera.main);
    }

    /// <summary>
    /// 고정된 월드 좌표 기준으로 데미지 텍스트를 표시합니다.
    /// </summary>
    public void ShowDamage(Vector3 worldPos, int damage, Color color, DamageTextTarget type)
    {
        if (damageTextPrefab == null || canvas == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 1.5f);

        // 월드 좌표 표시도 같은 풀을 사용해 생성/파괴 비용을 줄입니다.
        GameObject go = poolManager.Get(damageTextPrefab, canvas.transform);
        go.transform.position = screenPos;

        var dt = go.GetComponent<DamageText>();
        if (dt != null)
        {
            dt.Setup(damage, color, null, Vector3.zero, Camera.main);
        }
    }
}
