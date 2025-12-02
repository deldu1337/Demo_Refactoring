using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    [Header("UI")]
    [SerializeField] private GameObject tooltipPanel;      // 표시할 말풍선 패널입니다.
    [SerializeField] private Text tooltipText;

    [Header("Optional")]
    [SerializeField] private Canvas uiCanvas;              // 필요 시 사용할 UI 캔버스입니다.
    [SerializeField] private GameObject tooltipPanelPrefab;// 캔버스에 붙일 패널 프리팹입니다.

    [Header("Layout")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

    [Header("Colors")]
    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 225);
    [SerializeField] private Color32 hoverColor = new Color32(3, 62, 113, 255);

    private Transform followTarget;
    private Action onClick;
    private Button panelButton;
    private UIHoverColor hover;

    /// <summary>
    /// 인스턴스를 지정하고 말풍선 패널을 준비합니다.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        EnsureCanvas();
        EnsurePanel();
        Hide();
    }

    /// <summary>
    /// 파괴 시 싱글턴 인스턴스를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 대상 위치를 따라가며 말풍선 패널의 화면 좌표를 갱신합니다.
    /// </summary>
    private void Update()
    {
        if (!tooltipPanel || !tooltipPanel.activeSelf || followTarget == null) return;

        var cam = Camera.main;
        if (!cam) return;

        Vector3 screenPos = cam.WorldToScreenPoint(followTarget.position + worldOffset);
        tooltipPanel.transform.position = screenPos;
    }

    /// <summary>
    /// 지정한 대상 위에 말풍선을 표시하고 클릭 시 호출할 동작을 설정합니다.
    /// </summary>
    /// <param name="target">말풍선을 표시할 대상 트랜스폼입니다.</param>
    /// <param name="info">말풍선에 보여줄 문자열입니다.</param>
    /// <param name="onClick">클릭 시 호출할 콜백입니다.</param>
    public void ShowFor(Transform target, string info, Action onClick)
    {
        if (!EnsurePanel())
        {
            Debug.LogWarning("[ItemTooltip] 말풍선 패널을 준비하지 못했습니다");
            return;
        }

        followTarget = target;
        tooltipText.text = info;
        this.onClick = onClick;

        var img = tooltipPanel.GetComponent<Image>();
        if (img) img.color = normalColor;

        tooltipPanel.SetActive(true);
        if (Camera.main)
            tooltipPanel.transform.position = Camera.main.WorldToScreenPoint(followTarget.position + worldOffset);
    }

    /// <summary>
    /// 말풍선을 숨기고 대상 및 콜백을 초기화합니다.
    /// </summary>
    public void Hide()
    {
        if (tooltipPanel) tooltipPanel.SetActive(false);
        followTarget = null;
        onClick = null;
    }

    /// <summary>
    /// 말풍선 패널이 존재하는지 확인하고 필요하면 생성합니다.
    /// </summary>
    /// <returns>패널이 준비되었을 때 참을 반환합니다.</returns>
    private bool EnsurePanel()
    {
        if (tooltipPanel) return true;

        var found = GameObject.Find("TooltipPanel");
        if (found) tooltipPanel = found;

        if (!tooltipPanel && tooltipPanelPrefab && EnsureCanvas())
        {
            tooltipPanel = Instantiate(tooltipPanelPrefab, uiCanvas.transform);
            tooltipPanel.name = "TooltipPanel";
        }

        if (!tooltipPanel) return false;

        hover = tooltipPanel.GetComponent<UIHoverColor>();
        if (!hover) hover = tooltipPanel.AddComponent<UIHoverColor>();
        hover.SetColors(normalColor, hoverColor);

        panelButton = tooltipPanel.GetComponent<Button>();
        if (!panelButton) panelButton = tooltipPanel.AddComponent<Button>();
        panelButton.onClick.RemoveAllListeners();
        panelButton.onClick.AddListener(() => onClick?.Invoke());

        if (!tooltipText) tooltipText = tooltipPanel.GetComponentInChildren<Text>(true);

        return true;
    }

    /// <summary>
    /// 사용할 UI 캔버스를 찾거나 설정합니다.
    /// </summary>
    /// <returns>캔버스가 준비되면 참을 반환합니다.</returns>
    private bool EnsureCanvas()
    {
        if (uiCanvas) return true;

        uiCanvas = FindAnyObjectByType<Canvas>();
        if (!uiCanvas)
        {
            Debug.LogWarning("[ItemTooltip] Canvas를 찾지 못했습니다. 툴팁을 표시할 수 없습니다");
            return false;
        }
        return true;
    }
}
