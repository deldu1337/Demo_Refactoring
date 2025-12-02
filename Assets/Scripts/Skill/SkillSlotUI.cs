using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 퀵슬롯 하나를 표현하며 드래그 앤 드롭을 처리합니다.
/// </summary>
public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Refs")]
    public Image icon;
    public SkillCooldownUI cooldownUI;
    public int index;

    [Header("Runtime")]
    public string SkillId { get; private set; }
    private Transform dragParent;
    private Canvas rootCanvas;
    private Image ghost;
    private Sprite currentIcon;

    /// <summary>
    /// 기본 참조를 준비하고 아이콘을 숨깁니다.
    /// </summary>
    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (icon != null) icon.enabled = false;
    }

    /// <summary>
    /// 슬롯에 스킬 아이디와 아이콘을 설정합니다.
    /// </summary>
    public void SetSkill(string id, Sprite sp)
    {
        SkillId = id;
        currentIcon = sp;
        if (icon != null)
        {
            icon.sprite = sp;
            icon.enabled = !string.IsNullOrEmpty(id);
        }
    }

    /// <summary>
    /// 현재 슬롯 데이터를 반환합니다.
    /// </summary>
    public (string id, Sprite sprite) GetData() => (SkillId, currentIcon);

    /// <summary>
    /// 전달된 데이터를 슬롯에 적용합니다.
    /// </summary>
    public void ApplyData((string id, Sprite sprite) data) => SetSkill(data.id, data.sprite);

    /// <summary>
    /// 드래그 시작 시 고스트 아이콘을 생성합니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData e)
    {
        if (string.IsNullOrEmpty(SkillId) || icon == null) return;

        ghost = new GameObject("Ghost", typeof(Image)).GetComponent<Image>();
        ghost.raycastTarget = false;
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.rectTransform.sizeDelta = icon.rectTransform.rect.size;
        ghost.sprite = icon.sprite;
        ghost.color = new Color(1, 1, 1, 0.8f);

        UpdateGhostPosition(e);
    }

    /// <summary>
    /// 드래그 중 고스트 위치를 갱신합니다.
    /// </summary>
    public void OnDrag(PointerEventData e)
    {
        if (ghost) UpdateGhostPosition(e);
    }

    /// <summary>
    /// 드래그 종료 시 고스트를 제거합니다.
    /// </summary>
    public void OnEndDrag(PointerEventData e)
    {
        if (ghost) Destroy(ghost.gameObject);
        ghost = null;
    }

    /// <summary>
    /// 고스트 이미지를 현재 포인터 위치로 이동합니다.
    /// </summary>
    void UpdateGhostPosition(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, e.position, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera, out var local);
        ghost.rectTransform.anchoredPosition = local;
    }

    /// <summary>
    /// 다른 슬롯이나 스킬북 아이템을 드롭했을 때 처리합니다.
    /// </summary>
    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag == null) return;

        var quickBar = GetComponentInParent<SkillQuickBar>();
        var fromSlot = e.pointerDrag.GetComponent<SkillSlotUI>();
        if (fromSlot != null && fromSlot != this)
        {
            quickBar?.Swap(fromSlot.index, index);
            return;
        }

        var bookItem = e.pointerDrag.GetComponent<SkillBookItemDraggable>();
        if (bookItem != null && bookItem.Unlocked)
        {
            quickBar?.Assign(index, bookItem.SkillId, bookItem.IconSprite);
            return;
        }
    }
}
