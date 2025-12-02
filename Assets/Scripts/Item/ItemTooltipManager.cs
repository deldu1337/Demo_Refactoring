using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipManager : MonoBehaviour
{
    public static ItemTooltipManager Instance;

    [Header("Prefab & Layout")]
    [SerializeField] private GameObject tooltipPrefab;   // 텍스트와 버튼이 포함된 UI 프리팹입니다.
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

    // 대상 트랜스폼과 생성된 말풍선 정보를 매핑합니다.
    private readonly Dictionary<Transform, TooltipInstance> actives = new();

    /// <summary>
    /// 싱글턴 인스턴스를 설정하고 프리팹 준비 여부를 확인합니다.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        if (tooltipPrefab == null)
            Debug.LogError("[ItemTooltipManager] tooltipPrefab이 설정되지 않았습니다");
    }

    /// <summary>
    /// 활성화된 말풍선의 화면 위치를 갱신하고 유효하지 않은 항목을 정리합니다.
    /// </summary>
    private void LateUpdate()
    {
        var toRemove = new List<Transform>();
        foreach (var kv in actives)
        {
            var target = kv.Key;
            var inst = kv.Value;

            if (!target || !inst.Panel) { toRemove.Add(target); continue; }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + inst.Offset);
            inst.Panel.position = screenPos;
        }

        foreach (var dead in toRemove)
            HideFor(dead);
    }

    /// <summary>
    /// 대상 위에 말풍선을 보여 주고 텍스트, 티어 색상, 클릭 동작을 설정합니다.
    /// </summary>
    /// <param name="target">말풍선을 표시할 대상입니다.</param>
    /// <param name="text">표시할 내용입니다.</param>
    /// <param name="tier">색상을 결정할 티어 문자열입니다.</param>
    /// <param name="onClick">클릭 시 실행할 콜백입니다.</param>
    /// <param name="offset">말풍선 위치 오프셋입니다.</param>
    public void ShowFor(Transform target, string text, string tier = null, Action onClick = null, Vector3? offset = null)
    {
        if (!target || tooltipPrefab == null) return;
        if (target.GetComponentInParent<EquippedMarker>() != null) return;

        if (!actives.TryGetValue(target, out var inst) || !inst.Panel)
        {
            var go = Instantiate(tooltipPrefab, transform);
            var rect = go.GetComponent<RectTransform>();
            var txt = go.GetComponentInChildren<Text>(true);
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();

            inst = new TooltipInstance
            {
                RootGO = go,
                Panel = rect,
                Text = txt,
                Button = btn
            };
            actives[target] = inst;
        }

        inst.Text.text = text;
        inst.Text.color = GetTierColor(tier);
        inst.Button.onClick.RemoveAllListeners();
        if (onClick != null) inst.Button.onClick.AddListener(() => onClick());

        inst.Offset = offset ?? worldOffset;
        if (!inst.RootGO.activeSelf) inst.RootGO.SetActive(true);
    }

    /// <summary>
    /// 특정 대상과 연결된 말풍선을 제거합니다.
    /// </summary>
    /// <param name="target">숨길 대상 트랜스폼입니다.</param>
    public void HideFor(Transform target)
    {
        if (!actives.TryGetValue(target, out var inst)) return;

        if (inst.RootGO) Destroy(inst.RootGO);
        actives.Remove(target);
    }

    /// <summary>
    /// 모든 활성 말풍선을 제거합니다.
    /// </summary>
    public void HideAll()
    {
        foreach (var kv in actives)
        {
            if (kv.Value.RootGO) Destroy(kv.Value.RootGO);
        }
        actives.Clear();
    }

    /// <summary>
    /// 티어 문자열에 따라 텍스트 색상을 반환합니다.
    /// </summary>
    /// <param name="tier">확인할 티어 문자열입니다.</param>
    /// <returns>티어에 대응하는 색상을 반환합니다.</returns>
    private static Color GetTierColor(string tier)
    {
        if (string.IsNullOrEmpty(tier)) return Color.white;
        switch (tier.ToLower())
        {
            case "normal": return Color.white;
            case "magic": return new Color32(50, 205, 50, 255);
            case "rare": return new Color32(0, 128, 255, 255);
            case "unique": return new Color32(255, 0, 144, 255);
            case "legendary": return new Color32(255, 215, 0, 255);
            default: return Color.white;
        }
    }

    private class TooltipInstance
    {
        public GameObject RootGO;
        public RectTransform Panel;
        public Text Text;
        public Button Button;
        public Vector3 Offset;
    }
}
