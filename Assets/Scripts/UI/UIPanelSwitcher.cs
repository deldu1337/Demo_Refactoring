using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIPanelSwitcher : MonoBehaviour
{
    [Header("(From) 패널 루트 - 기준 위치를 참조")]
    [SerializeField] private RectTransform fromPanelRoot;

    [Header("전환 대상 (To) 패널 루트")]
    [SerializeField] private RectTransform toPanelRoot;

    [Header("옵션")]
    [SerializeField] private bool copyScaleAndRotation = true;
    [SerializeField] private bool matchSiblingIndex = true;

    private Button btn;

    /// <summary>
    /// 버튼과 패널 참조를 설정하고 클릭 이벤트를 연결합니다.
    /// </summary>
    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(Switch);

        if (!fromPanelRoot)
            fromPanelRoot = FindRootPanelUnderCanvas(transform as RectTransform);

        if (!fromPanelRoot || !toPanelRoot)
            Debug.LogError("[UIPanelSwitcher] fromPanelRoot / toPanelRoot 설정을 확인해 주세요.");
    }

    /// <summary>
    /// 버튼을 누르면 대상 패널을 표시하고 기존 패널을 숨깁니다.
    /// </summary>
    public void Switch()
    {
        if (!fromPanelRoot || !toPanelRoot) return;

        if (toPanelRoot.parent != fromPanelRoot.parent)
            toPanelRoot.SetParent(fromPanelRoot.parent, worldPositionStays: false);

        CopyLayout(fromPanelRoot, toPanelRoot);

        if (copyScaleAndRotation)
        {
            toPanelRoot.localScale = fromPanelRoot.localScale;
            toPanelRoot.localRotation = fromPanelRoot.localRotation;
        }
        if (matchSiblingIndex)
            toPanelRoot.SetSiblingIndex(fromPanelRoot.GetSiblingIndex());

        toPanelRoot.gameObject.SetActive(true);
        fromPanelRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// 시작 노드에서 부모를 거슬러 올라가며 캔버스 바로 아래 패널을 찾습니다.
    /// </summary>
    private RectTransform FindRootPanelUnderCanvas(RectTransform start)
    {
        RectTransform cur = start;
        while (cur && cur.parent is RectTransform parentRT)
        {
            if (parentRT.GetComponent<Canvas>() != null)
                return cur;
            cur = parentRT;
        }
        return null;
    }

    /// <summary>
    /// 두 RectTransform의 레이아웃 속성을 복사합니다.
    /// </summary>
    private void CopyLayout(RectTransform from, RectTransform to)
    {
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;

        to.sizeDelta = from.sizeDelta;
        to.anchoredPosition3D = from.anchoredPosition3D;
    }

    /// <summary>
    /// 정적 메서드로 두 RectTransform의 위치와 배치를 맞춥니다.
    /// </summary>
    public static void CopyLayoutRT(
        RectTransform from, RectTransform to,
        bool ensureSameParent = true,
        bool copyScaleAndRotation = true,
        bool matchSiblingIndex = true)
    {
        if (!from || !to) return;

        if (ensureSameParent && to.parent != from.parent)
            to.SetParent(from.parent, worldPositionStays: false);

        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.sizeDelta = from.sizeDelta;
        to.anchoredPosition3D = from.anchoredPosition3D;

        if (copyScaleAndRotation)
        {
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }
        if (matchSiblingIndex)
            to.SetSiblingIndex(from.GetSiblingIndex());
    }

    private static Vector3 s_localPos;

    private static bool hasSnapshot;
    private static Vector2 s_anchorMin, s_anchorMax, s_pivot, s_sizeDelta;
    private static Vector3 s_anchoredPos3D, s_scale;
    private static Quaternion s_rotation;
    private static int s_siblingIndex;
    private static RectTransform s_parent;

    /// <summary>
    /// RectTransform의 배치 정보를 스냅샷으로 저장합니다.
    /// </summary>
    public static void SaveSnapshot(RectTransform rt)
    {
        if (!rt) return;
        s_parent = rt.parent as RectTransform;
        s_anchorMin = rt.anchorMin;
        s_anchorMax = rt.anchorMax;
        s_pivot = rt.pivot;
        s_sizeDelta = rt.sizeDelta;
        s_anchoredPos3D = rt.anchoredPosition3D;
        s_localPos = rt.localPosition;
        s_scale = rt.localScale;
        s_rotation = rt.localRotation;
        s_siblingIndex = rt.GetSiblingIndex();
        hasSnapshot = true;
    }

    /// <summary>
    /// 저장된 스냅샷 정보를 RectTransform에 복원합니다.
    /// </summary>
    public static void LoadSnapshot(RectTransform rt, bool applySiblingIndex = true)
    {
        if (!rt || !hasSnapshot) return;

        if (s_parent && rt.parent != s_parent)
            rt.SetParent(s_parent, worldPositionStays: false);

        rt.anchorMin = s_anchorMin;
        rt.anchorMax = s_anchorMax;
        rt.pivot = s_pivot;
        rt.sizeDelta = s_sizeDelta;

        rt.localPosition = s_localPos;
        rt.anchoredPosition3D = s_anchoredPos3D;

        rt.localScale = s_scale;
        rt.localRotation = s_rotation;

        if (applySiblingIndex) rt.SetSiblingIndex(s_siblingIndex);

        Canvas.ForceUpdateCanvases();
        var prt = rt.parent as RectTransform;
        if (prt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
    }

    /// <summary>
    /// 저장된 스냅샷을 초기화합니다.
    /// </summary>
    public static void ClearSnapshot()
    {
        hasSnapshot = false;
        s_parent = null;
    }

    /// <summary>
    /// 스냅샷 존재 여부를 반환합니다.
    /// </summary>
    public static bool HasSnapshot => hasSnapshot;
}
