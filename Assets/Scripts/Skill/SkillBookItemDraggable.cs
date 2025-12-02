using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 북 아이템을 드래그하여 퀵슬롯에 배치하는 UI 요소입니다.
/// </summary>
public class SkillBookItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public Image icon;
    public GameObject lockOverlay;
    [SerializeField] private Image bg;

    [Header("Runtime")]
    public string SkillId { get; private set; }
    public int UnlockLevel { get; private set; }
    public bool Unlocked { get; private set; }
    public Sprite IconSprite { get; private set; }

    private Canvas rootCanvas;
    private Image ghost;

    /// <summary>
    /// 캔버스와 아이콘 렌더링 설정을 초기화합니다.
    /// </summary>
    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>(true);

        if (bg) bg.raycastTarget = false;

        if (icon)
        {
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
    }

    /// <summary>
    /// 활성화될 때 아이콘 표시 상태를 보정합니다.
    /// </summary>
    void OnEnable() => EnsureIconVisibility();

    /// <summary>
    /// 비활성화 시 남아 있는 고스트 아이콘을 제거합니다.
    /// </summary>
    void OnDisable()
    {
        ForceEndDrag();
    }

    /// <summary>
    /// 드래그 중 생성된 고스트를 강제로 제거합니다.
    /// </summary>
    public void ForceEndDrag()
    {
        if (ghost)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    /// <summary>
    /// 스킬 정보를 아이템에 반영합니다.
    /// </summary>
    public void Setup(string id, Sprite sp, int unlockLv, bool unlocked)
    {
        SkillId = id;
        UnlockLevel = unlockLv;
        IconSprite = sp;
        Unlocked = unlocked;

        if (icon) icon.sprite = sp;
        if (lockOverlay) lockOverlay.SetActive(!unlocked);

        EnsureIconVisibility();
    }

    /// <summary>
    /// 잠금 여부를 갱신합니다.
    /// </summary>
    public void SetUnlocked(bool unlocked)
    {
        Unlocked = unlocked;
        if (lockOverlay) lockOverlay.SetActive(!unlocked);
        EnsureIconVisibility();
    }

    /// <summary>
    /// 아이콘과 잠금 표시의 노출 상태를 조정합니다.
    /// </summary>
    private void EnsureIconVisibility()
    {
        if (icon)
            icon.enabled = (IconSprite != null || icon.sprite != null);
    }

    /// <summary>
    /// 드래그를 시작할 때 고스트 이미지를 생성합니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData e)
    {
        if (!Unlocked || icon == null || icon.sprite == null) return;
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        if (!rootCanvas) return;

        ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        ghost.raycastTarget = false;
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.sprite = icon.sprite;
        ghost.color = new Color(1, 1, 1, 0.85f);

        var size = icon.rectTransform.rect.size;
        ghost.rectTransform.sizeDelta = size;

        UpdateGhost(e);
    }

    /// <summary>
    /// 드래그 중 고스트 위치를 갱신합니다.
    /// </summary>
    public void OnDrag(PointerEventData e)
    {
        if (ghost) UpdateGhost(e);
    }

    /// <summary>
    /// 드래그가 끝나면 고스트를 제거합니다.
    /// </summary>
    public void OnEndDrag(PointerEventData e)
    {
        if (ghost) Destroy(ghost.gameObject);
        ghost = null;
    }

    /// <summary>
    /// 포인터 위치를 캔버스 좌표로 변환하여 고스트를 이동합니다.
    /// </summary>
    private void UpdateGhost(PointerEventData e)
    {
        if (!rootCanvas || !ghost) return;

        Camera cam = null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = rootCanvas.worldCamera;
        else if (rootCanvas.renderMode == RenderMode.WorldSpace)
            cam = rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            e.position,
            cam,
            out var local
        );
        ghost.rectTransform.anchoredPosition = local;
    }
}
