using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PotionSlotUI : MonoBehaviour, IDropHandler
{
    [Tooltip("0~3 (키 1~4에 매핑)")]
    public int index;

    [Header("UI")]
    public Image icon;                 // 자식에 있는 이미지(예: "1", "2", "3", "4")
    public GameObject emptyOverlay;    // 빈 슬롯일 때 보이는 배경(선택)

    // 현재 슬롯에 바인딩된 인벤토리 아이템의 uniqueId
    public string boundUniqueId;

    private Text qtyText; // ★ 추가

    // 외부에서 아이콘을 자동으로 끌어다 연결하고 싶으면 호출
    public void AutoWireIconByChildName(string childName)
    {
        if (icon) return;
        var t = transform.Find(childName);
        icon = t ? t.GetComponent<Image>() : GetComponentInChildren<Image>(true);
        if (icon)
        {
            icon.raycastTarget = false;
            icon.enabled = false; // ← 초기엔 비활성
        }
    }

    public void Clear()
    {
        boundUniqueId = null;

        if (icon)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        SetQty(0); // ★ 숨김
        if (emptyOverlay) emptyOverlay.SetActive(true);
    }

    // ★ 수량 지원 버전
    public void Set(InventoryItem item, Sprite s, int quantity)
    {
        boundUniqueId = item.uniqueId;

        if (icon)
        {
            icon.sprite = s;
            icon.enabled = s != null;
        }

        SetQty(quantity); // ★
        if (emptyOverlay) emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
    }

    public void SetBySave(string uniqueId, Sprite s, int quantity)
    {
        boundUniqueId = uniqueId;
        if (icon) { icon.sprite = s; icon.enabled = (s != null); }
        SetQty(quantity); // ★
        if (emptyOverlay) emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
    }

    public void SetBySave(string uniqueId, Sprite s) // ← 기존 시그니처가 쓰이는 곳 있으면 유지
    {
        SetBySave(uniqueId, s, 1);
    }

    public void SetQty(int q)
    {
        if (qtyText == null) qtyText = EnsureQtyLabel(transform);
        if (qtyText == null) return;

        if (q >= 1) // 1개는 표시 안 해도 되면 이렇게, 필요하면 >=1 로
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

    // === 인벤토리에서 드래그 드롭 수신 (포션만 허용) ===
    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemView>() : null;
        if (drag == null || drag.Item == null) return;

        var item = drag.Item;
        if (item.data == null || !string.Equals(item.data.type, "potion", StringComparison.OrdinalIgnoreCase))
        {
            // 포션만 허용
            return;
        }

        // 아이콘 로드
        Sprite s = null;
        if (!string.IsNullOrEmpty(item.iconPath))
            s = Resources.Load<Sprite>(item.iconPath);

        PotionQuickBar.Instance.Assign(index, item, s);

        // 드래그 시각물은 원래 자리로 복귀(인벤토리에서만 드래그되므로 UI는 그대로)
        drag.SnapBackToOriginal();
    }

    // PotionSlotUI.cs 내부에
    public RectTransform GetRect() => GetComponent<RectTransform>();
    public Camera GetCanvasCamera() => GetComponentInParent<Canvas>()?.worldCamera;

    public void RefreshEmptyOverlay()
    {
        if (emptyOverlay)
            emptyOverlay.SetActive(string.IsNullOrEmpty(boundUniqueId));
        if (icon) icon.enabled = !string.IsNullOrEmpty(boundUniqueId) && icon.sprite != null;
    }
}
