//using UnityEngine;
//using UnityEngine.UI;

//[RequireComponent(typeof(Button))]
//public class UIPanelSwitcher : MonoBehaviour
//{
//    [Header("현재(From) 패널 루트 - 비워두면 자동으로 찾음")]
//    [SerializeField] private RectTransform fromPanelRoot;

//    [Header("전환 대상(To) 패널 루트")]
//    [SerializeField] private RectTransform toPanelRoot;

//    [Header("옵션")]
//    [SerializeField] private bool copyScaleAndRotation = true;
//    [SerializeField] private bool matchSiblingIndex = true;

//    private Button btn;

//    void Awake()
//    {
//        btn = GetComponent<Button>();
//        btn.onClick.AddListener(Switch);

//        if (!fromPanelRoot)
//            fromPanelRoot = FindRootPanelUnderCanvas(transform as RectTransform);

//        if (!fromPanelRoot || !toPanelRoot)
//            Debug.LogError("[UIPanelSwitcher] fromPanelRoot / toPanelRoot 을(를) 설정하세요.");
//    }

//    /// <summary>
//    /// 버튼 클릭 → toPanel로 전환
//    /// </summary>
//    public void Switch()
//    {
//        if (!fromPanelRoot || !toPanelRoot) return;

//        // 같은 부모(Canvas 하위)로 맞춤
//        if (toPanelRoot.parent != fromPanelRoot.parent)
//            toPanelRoot.SetParent(fromPanelRoot.parent, worldPositionStays: false);

//        // 레이아웃 복사 (앵커/피벗/크기/위치)
//        CopyLayout(fromPanelRoot, toPanelRoot);

//        // 필요 시 스케일/로테이션/형제 인덱스까지 동기화
//        if (copyScaleAndRotation)
//        {
//            toPanelRoot.localScale = fromPanelRoot.localScale;
//            toPanelRoot.localRotation = fromPanelRoot.localRotation;
//        }
//        if (matchSiblingIndex)
//            toPanelRoot.SetSiblingIndex(fromPanelRoot.GetSiblingIndex());

//        // 활성/비활성 전환
//        toPanelRoot.gameObject.SetActive(true);
//        fromPanelRoot.gameObject.SetActive(false);
//    }

//    /// <summary>
//    /// 현재 버튼이 속한 트리에서 Canvas 바로 아래의 "패널 루트" RectTransform을 찾아 반환
//    /// (예: HeadPanel → EquipmentUI / PlayerInfoUI)
//    /// </summary>
//    private RectTransform FindRootPanelUnderCanvas(RectTransform start)
//    {
//        RectTransform cur = start;
//        while (cur && cur.parent is RectTransform parentRT)
//        {
//            // 부모가 Canvas면 cur가 패널 루트
//            if (parentRT.GetComponent<Canvas>() != null)
//                return cur;
//            cur = parentRT;
//        }
//        return null;
//    }

//    private void CopyLayout(RectTransform from, RectTransform to)
//    {
//        to.anchorMin = from.anchorMin;
//        to.anchorMax = from.anchorMax;
//        to.pivot = from.pivot;

//        to.sizeDelta = from.sizeDelta;
//        to.anchoredPosition3D = from.anchoredPosition3D;
//    }

//    // 외부에서 안전하게 호출 가능한 유틸 (버튼 없이도 사용)
//    public static void CopyLayoutRT(RectTransform from, RectTransform to,
//        bool copyScaleAndRotation = true, bool matchSiblingIndex = true)
//    {
//        if (!from || !to) return;
//        to.anchorMin = from.anchorMin;
//        to.anchorMax = from.anchorMax;
//        to.pivot = from.pivot;
//        to.sizeDelta = from.sizeDelta;
//        to.anchoredPosition3D = from.anchoredPosition3D;

//        if (copyScaleAndRotation)
//        {
//            to.localScale = from.localScale;
//            to.localRotation = from.localRotation;
//        }
//        if (matchSiblingIndex)
//            to.SetSiblingIndex(from.GetSiblingIndex());
//    }

//    // ===== 스냅샷 저장소 (추가) =====
//    private static bool hasSnapshot;
//    private static Vector2 s_anchorMin, s_anchorMax, s_pivot, s_sizeDelta;
//    private static Vector3 s_anchoredPos3D, s_scale;
//    private static Quaternion s_rotation;
//    private static int s_siblingIndex;

//    public static void SaveSnapshot(RectTransform rt)
//    {
//        if (!rt) return;
//        s_anchorMin = rt.anchorMin;
//        s_anchorMax = rt.anchorMax;
//        s_pivot = rt.pivot;
//        s_sizeDelta = rt.sizeDelta;
//        s_anchoredPos3D = rt.anchoredPosition3D;
//        s_scale = rt.localScale;
//        s_rotation = rt.localRotation;
//        s_siblingIndex = rt.GetSiblingIndex();
//        hasSnapshot = true;
//    }

//    public static void LoadSnapshot(RectTransform rt, bool applySiblingIndex = true)
//    {
//        if (!rt || !hasSnapshot) return;
//        rt.anchorMin = s_anchorMin;
//        rt.anchorMax = s_anchorMax;
//        rt.pivot = s_pivot;
//        rt.sizeDelta = s_sizeDelta;
//        rt.anchoredPosition3D = s_anchoredPos3D;
//        rt.localScale = s_scale;
//        rt.localRotation = s_rotation;
//        if (applySiblingIndex) rt.SetSiblingIndex(s_siblingIndex);
//    }

//    public static bool HasSnapshot => hasSnapshot;
//}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIPanelSwitcher : MonoBehaviour
{
    [Header("현재(From) 패널 루트 - 비워두면 자동으로 찾음")]
    [SerializeField] private RectTransform fromPanelRoot;

    [Header("전환 대상(To) 패널 루트")]
    [SerializeField] private RectTransform toPanelRoot;

    [Header("옵션")]
    [SerializeField] private bool copyScaleAndRotation = true;
    [SerializeField] private bool matchSiblingIndex = true;

    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(Switch);

        if (!fromPanelRoot)
            fromPanelRoot = FindRootPanelUnderCanvas(transform as RectTransform);

        if (!fromPanelRoot || !toPanelRoot)
            Debug.LogError("[UIPanelSwitcher] fromPanelRoot / toPanelRoot 을(를) 설정하세요.");
    }

    /// <summary>
    /// 버튼 클릭 → toPanel로 전환
    /// </summary>
    public void Switch()
    {
        if (!fromPanelRoot || !toPanelRoot) return;

        // 같은 부모(Canvas 하위)로 맞춤
        if (toPanelRoot.parent != fromPanelRoot.parent)
            toPanelRoot.SetParent(fromPanelRoot.parent, worldPositionStays: false);

        // 레이아웃 복사 (앵커/피벗/크기/위치)
        CopyLayout(fromPanelRoot, toPanelRoot);

        // 필요 시 스케일/로테이션/형제 인덱스까지 동기화
        if (copyScaleAndRotation)
        {
            toPanelRoot.localScale = fromPanelRoot.localScale;
            toPanelRoot.localRotation = fromPanelRoot.localRotation;
        }
        if (matchSiblingIndex)
            toPanelRoot.SetSiblingIndex(fromPanelRoot.GetSiblingIndex());

        // 활성/비활성 전환
        toPanelRoot.gameObject.SetActive(true);
        fromPanelRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// 현재 버튼이 속한 트리에서 Canvas 바로 아래의 "패널 루트" RectTransform을 찾아 반환
    /// (예: HeadPanel → EquipmentUI / PlayerInfoUI)
    /// </summary>
    private RectTransform FindRootPanelUnderCanvas(RectTransform start)
    {
        RectTransform cur = start;
        while (cur && cur.parent is RectTransform parentRT)
        {
            // 부모가 Canvas면 cur가 패널 루트
            if (parentRT.GetComponent<Canvas>() != null)
                return cur;
            cur = parentRT;
        }
        return null;
    }

    private void CopyLayout(RectTransform from, RectTransform to)
    {
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;

        to.sizeDelta = from.sizeDelta;
        to.anchoredPosition3D = from.anchoredPosition3D;
    }

    // ---- 외부 유틸 (부모까지 기본적으로 맞춰줌) ----
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

    // ===== 스냅샷 저장소 =====

    // ----- UIPanelSwitcher.cs 의 스냅샷 필드에 추가 -----
    private static Vector3 s_localPos;   // ★ 추가: 드래그가 바꾸는 값

    private static bool hasSnapshot;
    private static Vector2 s_anchorMin, s_anchorMax, s_pivot, s_sizeDelta;
    private static Vector3 s_anchoredPos3D, s_scale;
    private static Quaternion s_rotation;
    private static int s_siblingIndex;
    private static RectTransform s_parent; // ★ 부모까지 저장

    // ----- SaveSnapshot 수정 -----
    public static void SaveSnapshot(RectTransform rt)
    {
        if (!rt) return;
        s_parent = rt.parent as RectTransform;
        s_anchorMin = rt.anchorMin;
        s_anchorMax = rt.anchorMax;
        s_pivot = rt.pivot;
        s_sizeDelta = rt.sizeDelta;
        s_anchoredPos3D = rt.anchoredPosition3D;
        s_localPos = rt.localPosition;   // ★ 추가
        s_scale = rt.localScale;
        s_rotation = rt.localRotation;
        s_siblingIndex = rt.GetSiblingIndex();
        hasSnapshot = true;
    }

    // ----- LoadSnapshot 수정 -----
    public static void LoadSnapshot(RectTransform rt, bool applySiblingIndex = true)
    {
        if (!rt || !hasSnapshot) return;

        if (s_parent && rt.parent != s_parent)
            rt.SetParent(s_parent, worldPositionStays: false);

        // 레이아웃 속성 복원
        rt.anchorMin = s_anchorMin;
        rt.anchorMax = s_anchorMax;
        rt.pivot = s_pivot;
        rt.sizeDelta = s_sizeDelta;

        // 위치 복원 (localPosition 우선 → 앵커 좌표도 백업 적용)
        rt.localPosition = s_localPos;       // ★ 핵심
        rt.anchoredPosition3D = s_anchoredPos3D;  // 보정 겸 호환

        rt.localScale = s_scale;
        rt.localRotation = s_rotation;

        if (applySiblingIndex) rt.SetSiblingIndex(s_siblingIndex);

        // 레이아웃 강제 갱신(부모에 레이아웃 컴포넌트 있을 때 초기화 버그 방지)
        Canvas.ForceUpdateCanvases();
        var prt = rt.parent as RectTransform;
        if (prt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
    }

    // UIPanelSwitcher.cs 내부 (클래스 하단 쯤)
    public static void ClearSnapshot()
    {
        hasSnapshot = false;
        s_parent = null;
    }

    public static bool HasSnapshot => hasSnapshot;
}
