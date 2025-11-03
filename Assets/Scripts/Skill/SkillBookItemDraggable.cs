using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillBookItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public Image icon;                 // 자식 "Icon"
    public GameObject lockOverlay;     // 자식 "LockOverlay"
    [SerializeField] private Image bg; // 자식 "Bg" (선택)

    [Header("Runtime")]
    public string SkillId { get; private set; }
    public int UnlockLevel { get; private set; }
    public bool Unlocked { get; private set; }
    public Sprite IconSprite { get; private set; }

    private Canvas rootCanvas;
    private Image ghost;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>(true);

        if (bg) bg.raycastTarget = false;

        if (icon)
        {
            icon.preserveAspect = true;
            icon.raycastTarget = false; // 드래그시 마우스 픽 차단
        }
    }

    void OnEnable() => EnsureIconVisibility();

    // 🔹 패널/아이템이 꺼질 때 떠 있는 고스트 정리
    void OnDisable()
    {
        ForceEndDrag();
    }

    public void ForceEndDrag()
    {
        if (ghost)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

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

    public void SetUnlocked(bool unlocked)
    {
        Unlocked = unlocked;
        if (lockOverlay) lockOverlay.SetActive(!unlocked);
        EnsureIconVisibility();
    }

    private void EnsureIconVisibility()
    {
        // 아이콘 스프라이트가 있으면 항상 보이게
        if (icon)
            icon.enabled = (IconSprite != null || icon.sprite != null);
        // 잠금 상태라면 약간의 알파를 줄 수도 있음 (원하면 주석 해제)
        // if (icon) icon.color = Unlocked ? Color.white : new Color(1, 1, 1, 0.7f);
    }

    // ===== Drag =====
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

        // 아이콘 크기에 맞춰 사이즈
        var size = icon.rectTransform.rect.size;
        ghost.rectTransform.sizeDelta = size;

        UpdateGhost(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (ghost) UpdateGhost(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (ghost) Destroy(ghost.gameObject);
        ghost = null;
    }

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
