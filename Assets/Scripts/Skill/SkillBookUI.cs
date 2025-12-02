using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 북 UI를 표시하고 해제 조건에 따라 잠금을 관리합니다.
/// </summary>
public class SkillBookUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform contentParent;
    [SerializeField] private SkillBookItemDraggable itemPrefab;

    private readonly Dictionary<string, SkillBookItemDraggable> items = new();
    private PlayerStatsManager stats;

    public bool IsOpen => panel != null && panel.activeSelf;

    private const string ESC_KEY = "skillbook";

    /// <summary>
    /// 참조를 초기화하고 기본적으로 패널을 닫습니다.
    /// </summary>
    void Awake()
    {
        stats = PlayerStatsManager.Instance;
        if (!panel) panel = gameObject;
        if (closeButton) closeButton.onClick.AddListener(() => Show(false));
        Show(false);
    }

    /// <summary>
    /// 활성화 시 레벨 업 이벤트를 등록합니다.
    /// </summary>
    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnLevelUp -= OnLevelUp;
            stats.OnLevelUp += OnLevelUp;
        }
    }

    /// <summary>
    /// 비활성화 시 이벤트를 해제하고 ESC 스택에서 제거합니다.
    /// </summary>
    void OnDisable()
    {
        if (stats != null) stats.OnLevelUp -= OnLevelUp;
        if (IsOpen) UIEscapeStack.GetOrCreate().Remove(ESC_KEY);
    }

    /// <summary>
    /// 스킬 북의 열림 상태를 토글합니다.
    /// </summary>
    public void Toggle() => Show(!IsOpen);

    /// <summary>
    /// 패널을 열거나 닫고 ESC 입력 스택을 관리합니다.
    /// </summary>
    public void Show(bool visible)
    {
        if (!panel) return;
        if (visible == IsOpen) return;

        panel.SetActive(visible);

        var esc = UIEscapeStack.GetOrCreate();
        if (visible)
        {
            esc.Push(
                key: ESC_KEY,
                close: () => Show(false),
                isOpen: () => IsOpen
            );
        }
        else
        {
            esc.Remove(ESC_KEY);
        }
    }

    /// <summary>
    /// 스킬 정보를 기반으로 UI 항목을 생성합니다.
    /// </summary>
    public void Build(List<SkillUnlockDef> defs, System.Func<string, Sprite> iconResolver)
    {
        if (!contentParent || !itemPrefab) return;

        foreach (Transform t in contentParent) Destroy(t.gameObject);
        items.Clear();

        foreach (var def in defs)
        {
            var item = Instantiate(itemPrefab, contentParent);
            var sp = iconResolver?.Invoke(def.skillId);
            item.Setup(def.skillId, sp, def.unlockLevel, false);
            items[def.skillId] = item;
        }
        RefreshLocks(stats?.Data?.Level ?? 1);
    }

    /// <summary>
    /// 레벨 업 시 잠금 상태를 새로고침합니다.
    /// </summary>
    private void OnLevelUp(int level) => RefreshLocks(level);

    /// <summary>
    /// 현재 레벨에 따라 잠금 상태를 업데이트합니다.
    /// </summary>
    public void RefreshLocks(int level)
    {
        foreach (var kv in items)
            kv.Value.SetUnlocked(level >= kv.Value.UnlockLevel);
    }

    /// <summary>
    /// 특정 스킬을 해제 표시로 갱신합니다.
    /// </summary>
    public void MarkUnlocked(string skillId)
    {
        if (items.TryGetValue(skillId, out var it))
            it.SetUnlocked(true);
    }
}
