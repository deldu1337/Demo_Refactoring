using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    public GameObject damageTextPrefab;
    public Canvas canvas;

    /// <summary>
    /// 싱글턴 인스턴스를 설정합니다.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

    public enum DamageTextTarget
    {
        Enemy,
        Player
    }

    /// <summary>
    /// 대상 Transform 기준으로 데미지 텍스트를 표시합니다.
    /// </summary>
    public void ShowDamage(Transform target, int damage, Color color, DamageTextTarget type)
    {
        if (!target || damageTextPrefab == null || canvas == null) return;

        Vector3 worldOffset = Vector3.up * 1.5f;

        GameObject go = Instantiate(damageTextPrefab, canvas.transform);

        var dt = go.GetComponent<DamageText>();
        if (dt != null)
            dt.Setup(damage, color, target, worldOffset, Camera.main);
    }

    /// <summary>
    /// 월드 좌표 기준으로 데미지 텍스트를 표시합니다.
    /// </summary>
    public void ShowDamage(Vector3 worldPos, int damage, Color color, DamageTextTarget type)
    {
        if (damageTextPrefab == null || canvas == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 1.5f);
        GameObject go = Instantiate(damageTextPrefab, canvas.transform);
        go.transform.position = screenPos;

        var dt = go.GetComponent<DamageText>();
        if (dt != null)
        {
            dt.Setup(damage, color, null, Vector3.zero, Camera.main);
        }
    }
}
