using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    public GameObject damageTextPrefab;
    public Canvas canvas;

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
    /// 대상 Transform에 고정되는 데미지 텍스트 생성(권장)
    /// </summary>
    public void ShowDamage(Transform target, int damage, Color color, DamageTextTarget type)
    {
        if (!target || damageTextPrefab == null || canvas == null) return;

        // 대상 종류별 월드 오프셋(머리 위 등)
        Vector3 worldOffset = Vector3.up * 1.5f;

        // 캔버스 하위에 생성
        GameObject go = Instantiate(damageTextPrefab, canvas.transform);

        // 바로 위치 잡을 필요 없이, DamageText가 매 프레임 target 추적
        var dt = go.GetComponent<DamageText>();
        if (dt != null)
            dt.Setup(damage, color, target, worldOffset, Camera.main);
    }

    // [선택] 구버전 호환: worldPos로도 호출 가능하지만 '고정'은 안 됨 (기존 동작)
    public void ShowDamage(Vector3 worldPos, int damage, Color color, DamageTextTarget type)
    {
        if (damageTextPrefab == null || canvas == null || Camera.main == null) return;

        // worldPos를 스크린으로 변환하여 1프레임만 기준으로 사용(이전 방식)
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 1.5f);
        GameObject go = Instantiate(damageTextPrefab, canvas.transform);
        go.transform.position = screenPos;

        var dt = go.GetComponent<DamageText>();
        if (dt != null)
        {
            // 따라갈 대상이 없으니 null 전달(화면 기준으로만 애니메이션)
            dt.Setup(damage, color, null, Vector3.zero, Camera.main);
        }
    }
}
