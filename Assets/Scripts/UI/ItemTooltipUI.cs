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

    [Header("Compare Tooltip (optional)")]
    [SerializeField] private RectTransform compareRoot;
    [SerializeField] private float compareGap = 10f;

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

    private static readonly Color32 MaxRollColor = new Color32(73, 221, 223, 255);

    /// <summary>
    /// 참조를 초기화하고 텍스트 색상을 설정합니다.
    /// </summary>
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
            if (!compareNameText) compareNameText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Name"));
            if (!compareLevelText) compareLevelText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Level"));
            if (!compareTierText) compareTierText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Tier"));
            if (!compareTypeText) compareTypeText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Type"));
            if (!compareStatsText) compareStatsText = compareRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name.Contains("Stats"));

            if (compareNameText) compareNameText.color = Color.white;
            if (compareLevelText) compareLevelText.color = Color.white;
            if (compareTypeText) compareTypeText.color = Color.white;
            if (compareStatsText) { compareStatsText.color = new Color32(100, 96, 219, 255); compareStatsText.supportRichText = true; }

            compareRoot.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 비활성화 시 툴팁을 숨깁니다.
    /// </summary>
    private void OnDisable() => Hide();

    /// <summary>
    /// 지정된 화면 좌표에 단일 툴팁을 보여줍니다.
    /// </summary>
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

    /// <summary>
    /// 대상 UI 옆에 단일 툴팁을 표시합니다.
    /// </summary>
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

    /// <summary>
    /// 비교용 툴팁을 포함하여 대상 옆에 표시합니다.
    /// </summary>
    public void ShowNextToWithCompare(InventoryItem invItem, InventoryItem eqItem, RectTransform target, ItemHoverTooltip owner)
    {
        ShowNextTo(invItem, target, owner);

        if (eqItem == null || compareRoot == null)
        {
            if (compareRoot) compareRoot.gameObject.SetActive(false);
            return;
        }

        if (compareNameText)
        {
            compareNameText.text = $"{eqItem.data.name} <size=11><color=#A1A1A6>(장착중)</color></size>";
            compareNameText.color = GetTierColor(eqItem.data.tier);
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
        if (compareTypeText) compareTypeText.text = $"종류: {eqItem.data.type}";

        if (compareStatsText)
            compareStatsText.text = ItemStatCompare.BuildCompareLines(invItem, eqItem, showEquippedValues: true);

        ForceRebuild(compareRoot);

        var parentRect = compareRoot.parent as RectTransform;
        if (parentRect == null) parentRect = root.parent as RectTransform;

        Vector2 mainPos = root.anchoredPosition;
        Vector2 mainSize = root.rect.size;
        float mainLeftX = mainPos.x - (root.pivot.x * mainSize.x);

        compareRoot.pivot = new Vector2(1f, 0.5f);
        Vector2 desired;
        desired.x = mainLeftX - compareGap;
        desired.y = mainPos.y;

        compareRoot.anchoredPosition = ClampInsideParentByPivot(desired, compareRoot, parentRect);

        compareRoot.gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// 요청자가 맞을 때 툴팁을 숨깁니다.
    /// </summary>
    public void Hide(ItemHoverTooltip requester = null)
    {
        if (requester != null && requester != currentOwner) return;
        gameObject.SetActive(false);
        currentOwner = null;
        if (compareRoot) compareRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// 비교 툴팁 레이아웃을 강제로 재계산합니다.
    /// </summary>
    private void ForceRebuild(RectTransform rt)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    /// <summary>
    /// 피벗을 고려하여 부모 내부에 위치를 고정합니다.
    /// </summary>
    private Vector2 ClampInsideParentByPivot(Vector2 desired, RectTransform self, RectTransform parentRect)
    {
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

    /// <summary>
    /// 화면 좌표를 입력받아 툴팁 위치를 갱신합니다.
    /// </summary>
    public void UpdatePosition(Vector2 screenPos)
    {
        if (canvas == null) return;
        Vector2 sp = screenPos + screenOffset;
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, sp, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);

        root.anchoredPosition = ClampInsideCanvas(local, canvasRect);
    }

    /// <summary>
    /// 헤더 텍스트와 색상을 설정합니다.
    /// </summary>
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

        if (typeText) typeText.text = $"종류: {item.data.type}";
    }

    /// <summary>
    /// 아이템 능력치를 문자열로 구성합니다.
    /// </summary>
    private string BuildStats(InventoryItem item)
    {
        var sb = new StringBuilder();
        var d = item.data;
        var r = item.rolled;

        bool isPotion = string.Equals(d.type, "potion", System.StringComparison.OrdinalIgnoreCase);

        void AddLine(string label, string key, float baseVal)
        {
            float v = baseVal;

            bool hasRolled = !isPotion && r != null && r.TryGet(key, out v);

            bool isZero = Mathf.Abs(v) <= 0.0001f;
            if (isZero) return;

            bool isMax = hasRolled && ItemRoller.IsMaxRoll(item.id, key, v);

            string valueStr = key == "cc" ? $"+{v * 100f}%" :
                              key == "cd" ? $"x{v}" :
                              $"+{v}";

            if (!isPotion && isMax && d.type != "potion")
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGBA(MaxRollColor)}>{label} {valueStr}</color>");
            else
                sb.AppendLine($"{label} {valueStr}");
        }

        AddLine("HP", "hp", d.hp);
        AddLine("MP", "mp", d.mp);
        AddLine("공격력", "atk", d.atk);
        AddLine("방어력", "def", d.def);
        AddLine("민첩", "dex", d.dex);
        AddLine("공속", "As", d.As);
        AddLine("치명타 확률", "cc", d.cc);
        AddLine("치명타 피해", "cd", d.cd);

        if (sb.Length == 0) sb.Append("표시할 능력치가 없습니다.");
        return sb.ToString();
    }

    /// <summary>
    /// 내용에 맞춰 툴팁 높이를 재조정합니다.
    /// </summary>
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

    /// <summary>
    /// 캔버스 범위 안으로 위치를 제한합니다.
    /// </summary>
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

    /// <summary>
    /// 부모 RectTransform 내부로 위치를 조정합니다.
    /// </summary>
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

    /// <summary>
    /// 좌측에 배치할 수 있는지 판단합니다.
    /// </summary>
    private bool HasRoomOnLeft(Vector2 parentLocalLeft, RectTransform parentRect)
    {
        Vector2 size = root.rect.size;
        Rect r = parentRect.rect;
        float predictedLeftEdge = parentLocalLeft.x - gapFromIcon - size.x;
        return predictedLeftEdge >= r.xMin;
    }

    /// <summary>
    /// 우측에 배치할 수 있는지 판단합니다.
    /// </summary>
    private bool HasRoomOnRight(Vector2 parentLocalRight, RectTransform parentRect)
    {
        Vector2 size = root.rect.size;
        Rect r = parentRect.rect;
        float predictedRightEdge = parentLocalRight.x + gapFromIcon + size.x;
        return predictedRightEdge <= r.xMax;
    }

    /// <summary>
    /// 좌측 여유 공간을 계산합니다.
    /// </summary>
    private float AvailableSpaceLeft(Vector2 parentLocalLeft, RectTransform parentRect)
    {
        Rect r = parentRect.rect;
        return parentLocalLeft.x - gapFromIcon - r.xMin;
    }

    /// <summary>
    /// 우측 여유 공간을 계산합니다.
    /// </summary>
    private float AvailableSpaceRight(Vector2 parentLocalRight, RectTransform parentRect)
    {
        Rect r = parentRect.rect;
        return r.xMax - (parentLocalRight.x + gapFromIcon);
    }

    /// <summary>
    /// 아이템 등급에 맞는 색상을 반환합니다.
    /// </summary>
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
