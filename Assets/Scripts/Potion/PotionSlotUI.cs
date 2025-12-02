using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 포션 슬롯 UI의 상태를 관리하고 드롭 이벤트를 처리합니다.
/// </summary>
public class PotionSlotUI : MonoBehaviour, IDropHandler
{
    [Tooltip("0~3 (키 1~4 대응)")]
    public int index;

    [Header("UI")]
    public Image icon;                 // 슬롯에 표시될 아이콘입니다.
    public GameObject emptyOverlay;    // 비어 있을 때 표시되는 오버레이입니다.

    // 각 슬롯에 연결된 인벤토리 아이템의 고유 ID입니다.
    public string boundUniqueId;

    private Text qtyText; // 수량을 표시하는 텍스트입니다.

    /// <summary>
    /// 자식 이름을 기반으로 아이콘 이미지를 찾아 연결합니다.
    /// </summary>
    public void AutoWireIconByChildName(string childName)
    {
        if (icon) return;
        var t = transform.Find(childName);
        icon = t ? t.GetComponent<Image>() : GetComponentInChildren<Image>(true);
        if (icon)
        {
            icon.raycastTarget = false;
            icon.enabled = false; // 초기에는 표시하지 않습니다.
        }
    }

    /// <summary>
    /// 슬롯을 비우고 UI를 초기화합니다.
    /// </summary>
    public void Clear()
    {
        boundUniqueId = null;

        if (icon)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        SetQty(0); // 수량을 초기화합니다.
        if (emptyOverlay) emptyOverlay.SetActive(true);
    }

    /// <summary>
    /// 슬롯에 아이템과 수량을 설정합니다.
    /// </summary>
    public void Set(InventoryItem item, Sprite s, int quantity)
    {
        boundUniqueId = item.uniqueId;

        if (icon)
        {
            icon.sprite = s;
            icon.enabled = s != null;
        }

        SetQty(quantity); // 입력된 수량을 표시합니다.
        if (emptyOverlay) emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
    }

    /// <summary>
    /// 저장된 데이터로부터 슬롯을 설정합니다.
    /// </summary>
    public void SetBySave(string uniqueId, Sprite s, int quantity)
    {
        boundUniqueId = uniqueId;
        if (icon) { icon.sprite = s; icon.enabled = (s != null); }
        SetQty(quantity); // 저장된 수량을 표시합니다.
        if (emptyOverlay) emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
    }

    /// <summary>
    /// 저장된 데이터로부터 기본 수량을 사용해 슬롯을 설정합니다.
    /// </summary>
    public void SetBySave(string uniqueId, Sprite s)
    {
        SetBySave(uniqueId, s, 1);
    }

    /// <summary>
    /// 수량 텍스트를 업데이트합니다.
    /// </summary>
    public void SetQty(int q)
    {
        if (qtyText == null) qtyText = EnsureQtyLabel(transform);
        if (qtyText == null) return;

        if (q >= 1) // 한 개 이상이면 숫자를 보여드립니다.
        {
            qtyText.text = q.ToString();
            qtyText.enabled = true;
        }
        else
        {
            qtyText.text = "";
            qtyText.enabled = false;
        }
    }

    /// <summary>
    /// 수량을 표시하기 위한 텍스트 오브젝트를 확보합니다.
    /// </summary>
    private Text EnsureQtyLabel(Transform parent)
    {
        var t = parent.Find("Qty") as RectTransform;
        if (t == null)
        {
            var go = new GameObject("Qty", typeof(RectTransform));
            t = go.GetComponent<RectTransform>();
            t.SetParent(parent, false);
            t.anchorMin = new Vector2(1, 0);
            t.anchorMax = new Vector2(1, 0);
            t.pivot = new Vector2(1, 0);
            t.anchoredPosition = new Vector2(-10, 10);
            t.sizeDelta = new Vector2(60, 24);

            var txt = go.AddComponent<Text>();
            txt.alignment = TextAnchor.LowerRight;
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Font.CreateDynamicFontFromOSFont("Arial", 21); } catch { } }
            txt.font = f;
            txt.fontSize = 21;
            txt.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = true;

            qtyText = txt;
        }
        return t.GetComponent<Text>();
    }


    public bool IsEmpty => string.IsNullOrEmpty(boundUniqueId);

    /// <summary>
    /// 드래그된 아이템을 드롭했을 때 포션인지 확인하고 슬롯에 배정합니다.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemView>() : null;
        if (drag == null || drag.Item == null) return;

        var item = drag.Item;
        if (item.data == null || !string.Equals(item.data.type, "potion", StringComparison.OrdinalIgnoreCase))
        {
            // 포션이 아니라면 처리하지 않습니다.
            return;
        }

        // 아이콘 스프라이트를 불러옵니다.
        Sprite s = null;
        if (!string.IsNullOrEmpty(item.iconPath))
            s = Resources.Load<Sprite>(item.iconPath);

        PotionQuickBar.Instance.Assign(index, item, s);

        // 드래그된 뷰를 원래 자리로 돌려 UI 상태를 유지합니다.
        drag.SnapBackToOriginal();
    }

    /// <summary>
    /// 슬롯의 RectTransform을 반환합니다.
    /// </summary>
    public RectTransform GetRect() => GetComponent<RectTransform>();
    /// <summary>
    /// 슬롯이 속한 캔버스의 카메라를 반환합니다.
    /// </summary>
    public Camera GetCanvasCamera() => GetComponentInParent<Canvas>()?.worldCamera;

    /// <summary>
    /// 비어 있는 상태 여부에 따라 오버레이와 아이콘을 갱신합니다.
    /// </summary>
    public void RefreshEmptyOverlay()
    {
        if (emptyOverlay)
            emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
        if (icon) icon.enabled = !string.IsNullOrEmpty(boundUniqueId) && icon.sprite != null;
    }
}
