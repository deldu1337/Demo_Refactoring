using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    [Header("UI")]
    [SerializeField] private GameObject tooltipPanel;      // 씬 객체 or 프리팹
    [SerializeField] private Text tooltipText;

    [Header("Optional")]
    [SerializeField] private Canvas uiCanvas;              // 없으면 자동 탐색
    [SerializeField] private GameObject tooltipPanelPrefab;// 패널이 없다면 인스턴스 생성용(선택)

    [Header("Layout")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

    [Header("Colors")]
    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 225);
    [SerializeField] private Color32 hoverColor = new Color32(3, 62, 113, 255);

    private Transform followTarget;
    private Action onClick;
    private Button panelButton;
    private UIHoverColor hover;

    void Awake()
    {
        Instance = this;
        EnsureCanvas();
        EnsurePanel();        // 필요한 컴포넌트 바인딩까지
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // tooltipPanel이 파괴되었거나(null 같음) 아직 준비 안 됐으면 패스
        if (!tooltipPanel || !tooltipPanel.activeSelf || followTarget == null) return;

        var cam = Camera.main;
        if (!cam) return;

        Vector3 screenPos = cam.WorldToScreenPoint(followTarget.position + worldOffset);
        tooltipPanel.transform.position = screenPos;
    }

    public void ShowFor(Transform target, string info, Action onClick)
    {
        if (!EnsurePanel())
        {
            Debug.LogWarning("[ItemTooltip] tooltipPanel이 없어 표시할 수 없습니다.");
            return;
        }

        followTarget = target;
        tooltipText.text = info;
        this.onClick = onClick;

        // 배경색 초기화
        var img = tooltipPanel.GetComponent<Image>();
        if (img) img.color = normalColor;

        tooltipPanel.SetActive(true);
        // 초기 위치도 한 번 계산
        if (Camera.main)
            tooltipPanel.transform.position = Camera.main.WorldToScreenPoint(followTarget.position + worldOffset);
    }

    public void Hide()
    {
        if (tooltipPanel) tooltipPanel.SetActive(false);
        followTarget = null;
        onClick = null;
    }

    // ================== helpers ==================

    // 파괴/미할당 모두 커버하는 지연 보장
    private bool EnsurePanel()
    {
        // 이미 유효하면 OK
        if (tooltipPanel) return true;

        // 1) 씬에서 이름으로 찾아보기(옵션: 필요에 맞게 수정)
        var found = GameObject.Find("TooltipPanel");
        if (found) tooltipPanel = found;

        // 2) 프리팹이 있으면 캔버스 아래에 인스턴스 생성
        if (!tooltipPanel && tooltipPanelPrefab && EnsureCanvas())
        {
            tooltipPanel = Instantiate(tooltipPanelPrefab, uiCanvas.transform);
            tooltipPanel.name = "TooltipPanel";
        }

        if (!tooltipPanel) return false;

        // 컴포넌트 재바인딩
        hover = tooltipPanel.GetComponent<UIHoverColor>();
        if (!hover) hover = tooltipPanel.AddComponent<UIHoverColor>();
        hover.SetColors(normalColor, hoverColor);

        panelButton = tooltipPanel.GetComponent<Button>();
        if (!panelButton) panelButton = tooltipPanel.AddComponent<Button>();
        panelButton.onClick.RemoveAllListeners();
        panelButton.onClick.AddListener(() => onClick?.Invoke());

        // 텍스트 자동 바인딩(필요하면 직접 Drag&Drop)
        if (!tooltipText) tooltipText = tooltipPanel.GetComponentInChildren<Text>(true);

        return true;
    }

    private bool EnsureCanvas()
    {
        if (uiCanvas) return true;

        uiCanvas = FindAnyObjectByType<Canvas>();
        if (!uiCanvas)
        {
            Debug.LogWarning("[ItemTooltip] Canvas가 없어 tooltip을 생성/표시할 수 없습니다.");
            return false;
        }
        return true;
    }
}
