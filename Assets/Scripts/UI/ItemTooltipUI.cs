using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text tierText;
    [SerializeField] private Text typeText;
    [SerializeField] private Text statsText;

    // ItemTooltipUI.cs (필드 추가)
    [Header("Compare Tooltip (optional)")]
    [SerializeField] private RectTransform compareRoot;
    [SerializeField] private float compareGap = 10f;

    // ★ 메인과 동일한 구조의 텍스트들
    [SerializeField] private Text compareNameText;
    [SerializeField] private Text compareLevelText;
    [SerializeField] private Text compareTierText;
    [SerializeField] private Text compareTypeText;
    [SerializeField] private Text compareStatsText;

    [Header("Layout")]
    [SerializeField] private Vector2 screenOffset = new Vector2(16, -16);
    [SerializeField] private float gapFromIcon = 10f;
    [SerializeField] private float minHeight = 0f;

    private Transform originalParent;
    private ItemHoverTooltip currentOwner;

    private static readonly Color32 MaxRollColor = new Color32(73, 221, 223, 255); // 73,221,223,255

    void Awake()
    {
        Instance = this;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (root == null) root = transform as RectTransform;

        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 0.5f);

        if (nameText) nameText.color = Color.white;
        if (levelText) levelText.color = Color.white;
        if (typeText) typeText.color = Color.white;

        if (statsText)
        {
            statsText.color = new Color32(100, 96, 219, 255);
            statsText.supportRichText = true;
        }

        if (compareRoot)
        {
            // 연결이 비어 있으면 자식에서 찾아오기 (이름/순서에 맞춰 정리돼 있다면 생략 가능)
            if (!compareNameText) compareNameText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Name"));
            if (!compareLevelText) compareLevelText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Level"));
            if (!compareTierText) compareTierText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Tier"));
            if (!compareTypeText) compareTypeText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Type"));
            if (!compareStatsText) compareStatsText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Stats"));

            // 리치텍스트/기본색
            if (compareNameText) compareNameText.color = Color.white;
            if (compareLevelText) compareLevelText.color = Color.white;
            if (compareTypeText) compareTypeText.color = Color.white;
            if (compareStatsText) { compareStatsText.color = new Color32(100, 96, 219, 255); compareStatsText.supportRichText = true; }

            compareRoot.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void OnDisable() => Hide();

    public void Show(InventoryItem item, Vector2 screenPos)
    {
        if (item == null || item.data == null) return;

        SetupHeader(item);
        statsText.text = BuildStats(item);

        ForceResizeToContent();
        UpdatePosition(screenPos);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void ShowNextTo(InventoryItem item, RectTransform target, ItemHoverTooltip owner)
    {
        if (item == null || item.data == null || target == null) return;

        SetupHeader(item);
        statsText.text = BuildStats(item);
        ForceResizeToContent();

        var targetCanvas = target.GetComponentInParent<Canvas>();
        if (targetCanvas == null) return;

        if (root.parent != targetCanvas.transform)
        {
            originalParent = root.parent;
            root.SetParent(targetCanvas.transform, false);
        }

        var cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        var parentRect = root.parent as RectTransform;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 worldRightMid = (corners[2] + corners[3]) * 0.5f;
        Vector3 worldLeftMid = (corners[0] + corners[1]) * 0.5f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, RectTransformUtility.WorldToScreenPoint(cam, worldRightMid), cam, out var parentLocalRight);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, RectTransformUtility.WorldToScreenPoint(cam, worldLeftMid), cam, out var parentLocalLeft);

        bool canLeft = HasRoomOnLeft(parentLocalLeft, parentRect);
        bool canRight = HasRoomOnRight(parentLocalRight, parentRect);

        bool placeLeft = canLeft || (!canRight && AvailableSpaceLeft(parentLocalLeft, parentRect) >= AvailableSpaceRight(parentLocalRight, parentRect));

        root.pivot = placeLeft ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        Vector2 desired = placeLeft
            ? parentLocalLeft - new Vector2(gapFromIcon, 0f)
            : parentLocalRight + new Vector2(gapFromIcon, 0f);

        root.anchoredPosition = ClampInsideParent(desired, parentRect);

        currentOwner = owner;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (compareRoot) compareRoot.gameObject.SetActive(false);
    }

    // 기존 단일 ShowNextTo는 그대로 두고, 아래 "비교 포함" 오버로드 추가
    public void ShowNextToWithCompare(InventoryItem invItem, InventoryItem eqItem, RectTransform target, ItemHoverTooltip owner)
    {
        // 1) 메인(인벤) 툴팁은 기존대로
        ShowNextTo(invItem, target, owner);

        // 2) 비교 대상 없으면 종료
        if (eqItem == null || compareRoot == null) { if (compareRoot) compareRoot.gameObject.SetActive(false); return; }

        // 3) 비교 헤더: 메인과 동일 포맷
        if (compareNameText)
        {
            compareNameText.text = $"{eqItem.data.name} <size=11><color=#A1A1A6>(현재 장착)</color></size>";
            compareNameText.color = GetTierColor(eqItem.data.tier); // ★ 등급색으로
        }
        if (compareTierText)
        {
            compareTierText.text = $"등급: {eqItem.data.tier}";
            compareTierText.color = GetTierColor(eqItem.data.tier);
        }
        if (compareLevelText)
        {
            int req = Mathf.Max(1, eqItem.data.level);
            compareLevelText.text = $"요구 레벨: {req}";
            var ps = PlayerStatsManager.Instance;
            compareLevelText.color = (ps != null && ps.Data != null && ps.Data.Level < req) ? Color.red : Color.white;
        }
        if (compareTypeText) compareTypeText.text = $"분류: {eqItem.data.type}";

        // 4) 비교 본문: inv vs eq (± 컬러)
        if (compareStatsText)
            compareStatsText.text = ItemStatCompare.BuildCompareLines(invItem, eqItem, showEquippedValues: true);

        // 5) 크기 리빌드 + 위치는 메인 왼쪽에
        ForceRebuild(compareRoot);

        var parentRect = compareRoot.parent as RectTransform;
        if (parentRect == null) parentRect = root.parent as RectTransform;

        Vector2 mainPos = root.anchoredPosition;
        Vector2 mainSize = root.rect.size;
        float mainLeftX = mainPos.x - (root.pivot.x * mainSize.x); // 메인 왼쪽 엣지

        compareRoot.pivot = new Vector2(1f, 0.5f);
        Vector2 desired;
        desired.x = mainLeftX - compareGap; // 비교패널 오른쪽 엣지를 메인 왼쪽-g
        desired.y = mainPos.y;

        compareRoot.anchoredPosition = ClampInsideParentByPivot(desired, compareRoot, parentRect);

        compareRoot.gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide(ItemHoverTooltip requester = null)
    {
        if (requester != null && requester != currentOwner) return;
        // 메인
        gameObject.SetActive(false);
        currentOwner = null;
        // 비교
        if (compareRoot) compareRoot.gameObject.SetActive(false);
    }

    private void ForceRebuild(RectTransform rt)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        // VerticalLayoutGroup 있는 경우 높이 재산정 (root와 동일 로직을 재사용하고 싶으면 공용화해도 됨)
        // 여기서는 간단히 강제 리빌드만
    }

    private Vector2 ClampInsideParentByPivot(Vector2 desired, RectTransform self, RectTransform parentRect)
    {
        // self.pivot을 고려해서 parentRect 안으로 클램프
        Rect r = parentRect.rect;
        Vector2 size = self.rect.size;
        Vector2 pvt = self.pivot;

        float left = r.xMin + size.x * (1f - pvt.x);
        float right = r.xMax - size.x * pvt.x;
        float bottom = r.yMin + size.y * (1f - pvt.y);
        float top = r.yMax - size.y * pvt.y;

        return new Vector2(
            Mathf.Clamp(desired.x, left, right),
            Mathf.Clamp(desired.y, bottom, top)
        );
    }

    public void UpdatePosition(Vector2 screenPos)
    {
        if (canvas == null) return;
        Vector2 sp = screenPos + screenOffset;
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, sp, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);

        root.anchoredPosition = ClampInsideCanvas(local, canvasRect);
    }

    private void SetupHeader(InventoryItem item)
    {
        nameText.text = item.data.name;
        nameText.color = GetTierColor(item.data.tier);

        tierText.text = $"등급: {item.data.tier}";
        tierText.color = GetTierColor(item.data.tier);

        int required = Mathf.Max(1, item.data.level);
        levelText.text = $"요구 레벨: {required}";
        var ps = PlayerStatsManager.Instance;
        if (ps != null && ps.Data != null && ps.Data.Level < required)
            levelText.color = Color.red;
        else
            levelText.color = Color.white;

        if (typeText) typeText.text = $"분류: {item.data.type}";
    }

    private string BuildStats(InventoryItem item)
    {
        var sb = new StringBuilder();
        var d = item.data;
        var r = item.rolled;

        bool isPotion = string.Equals(d.type, "potion", System.StringComparison.OrdinalIgnoreCase);

        // 라인 추가 헬퍼 (포션이면 rolled 절대 적용 X)
        void AddLine(string label, string key, float baseVal)
        {
            float v = baseVal;

            // ★ 포션이 아닐 때만 rolled 적용
            bool hasRolled = !isPotion && r != null && r.TryGet(key, out v);

            bool isZero = Mathf.Abs(v) <= 0.0001f;
            if (isZero) return;

            // 하이라이트(최대치)는 포션 제외
            bool isMax = hasRolled && ItemRoller.IsMaxRoll(item.id, key, v);

            string valueStr = key == "cc" ? $"+{v * 100f}%" :
                              key == "cd" ? $"x{v}" :
                              $"+{v}";

            if (!isPotion && isMax && d.type != "potion")
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGBA(MaxRollColor)}>{label}  {valueStr}</color>");
            else
                sb.AppendLine($"{label}  {valueStr}");
        }

        AddLine("HP", "hp", d.hp);
        AddLine("MP", "mp", d.mp);
        AddLine("데미지", "atk", d.atk);
        AddLine("방어력", "def", d.def);
        AddLine("민첩성", "dex", d.dex);
        AddLine("공격 속도", "As", d.As);
        AddLine("치명타 확률", "cc", d.cc);
        AddLine("치명타 데미지", "cd", d.cd);

        if (sb.Length == 0) sb.Append("추가 능력치 없음");
        return sb.ToString();
    }

    private void ForceResizeToContent()
    {
        var vlg = root.GetComponent<VerticalLayoutGroup>();
        float spacing = vlg ? vlg.spacing : 0f;
        var padding = vlg ? vlg.padding : new RectOffset();

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        int enabledCount = 0;
        float totalHeights = 0f;

        if (nameText && nameText.gameObject.activeInHierarchy)
        { totalHeights += LayoutUtility.GetPreferredHeight(nameText.rectTransform); enabledCount++; }
        if (levelText && levelText.gameObject.activeInHierarchy)
        { totalHeights += LayoutUtility.GetPreferredHeight(levelText.rectTransform); enabledCount++; }
        if (typeText && typeText.gameObject.activeInHierarchy)
        { totalHeights += LayoutUtility.GetPreferredHeight(typeText.rectTransform); enabledCount++; }
        if (statsText && statsText.gameObject.activeInHierarchy)
        { totalHeights += LayoutUtility.GetPreferredHeight(statsText.rectTransform); enabledCount++; }

        float totalSpacing = Mathf.Max(0, enabledCount - 1) * spacing;
        float totalPadding = padding.top + padding.bottom;

        float newHeight = totalHeights + totalSpacing + totalPadding;
        if (minHeight > 0f) newHeight = Mathf.Max(newHeight, minHeight);

        root.sizeDelta = new Vector2(root.sizeDelta.x, newHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private Vector2 ClampInsideCanvas(Vector2 local, RectTransform canvasRect)
    {
        Vector2 size = root.sizeDelta;
        Vector2 canvasSize = canvasRect.rect.size;

        float leftLimit = -canvasSize.x * 0.5f + size.x * (1f - root.pivot.x);
        float rightLimit = canvasSize.x * 0.5f - size.x * (root.pivot.x);
        float bottomLimit = -canvasSize.y * 0.5f + size.y * (1f - root.pivot.y);
        float topLimit = canvasSize.y * 0.5f - size.y * (root.pivot.y);

        return new Vector2(
            Mathf.Clamp(local.x, leftLimit, rightLimit),
            Mathf.Clamp(local.y, bottomLimit, topLimit)
        );
    }

    private Vector2 ClampInsideParent(Vector2 local, RectTransform parentRect)
    {
        Vector2 size = root.rect.size;
        Rect r = parentRect.rect;

        float leftLimit = r.xMin + size.x * (1f - root.pivot.x);
        float rightLimit = r.xMax - size.x * (root.pivot.x);
        float bottomLimit = r.yMin + size.y * (1f - root.pivot.y);
        float topLimit = r.yMax - size.y * (root.pivot.y);

        return new Vector2(
            Mathf.Clamp(local.x, leftLimit, rightLimit),
            Mathf.Clamp(local.y, bottomLimit, topLimit)
        );
    }

    private bool HasRoomOnLeft(Vector2 parentLocalLeft, RectTransform parentRect)
    {
        Vector2 size = root.rect.size;
        Rect r = parentRect.rect;
        float predictedLeftEdge = parentLocalLeft.x - gapFromIcon - size.x; // pivot.x=1
        return predictedLeftEdge >= r.xMin;
    }

    private bool HasRoomOnRight(Vector2 parentLocalRight, RectTransform parentRect)
    {
        Vector2 size = root.rect.size;
        Rect r = parentRect.rect;
        float predictedRightEdge = parentLocalRight.x + gapFromIcon + size.x; // pivot.x=0
        return predictedRightEdge <= r.xMax;
    }

    private float AvailableSpaceLeft(Vector2 parentLocalLeft, RectTransform parentRect)
    {
        Rect r = parentRect.rect;
        return parentLocalLeft.x - gapFromIcon - r.xMin;
    }

    private float AvailableSpaceRight(Vector2 parentLocalRight, RectTransform parentRect)
    {
        Rect r = parentRect.rect;
        return r.xMax - (parentLocalRight.x + gapFromIcon);
    }

    private static Color GetTierColor(string tier)
    {
        if (string.IsNullOrEmpty(tier)) return Color.white;

        switch (tier.Trim().ToLower())
        {
            case "normal": return Color.white;
            case "magic": return new Color32(50, 205, 50, 255);
            case "rare": return new Color32(255, 128, 0, 255);
            case "unique": return new Color32(255, 0, 144, 255);
            case "legendary": return new Color32(255, 215, 0, 255);
            default: return Color.white;
        }
    }
}
