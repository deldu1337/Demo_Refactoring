using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI를 표시하고 버튼 상태를 갱신하는 뷰입니다.
/// </summary>
public class InventoryView : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button exitButton;

    private Button[] inventoryButtons;

    /// <summary>
    /// 종료 버튼 리스너를 설정하고 초기 상태를 숨깁니다.
    /// </summary>
    public void Initialize(Action onExit)
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(() => onExit?.Invoke());

        inventoryButtons = buttonContainer.GetComponentsInChildren<Button>(true);
        Show(false);
    }

    /// <summary>
    /// 인벤토리 패널 표시 여부를 설정합니다.
    /// </summary>
    public void Show(bool show) => inventoryPanel?.SetActive(show);

    /// <summary>
    /// 인벤토리 버튼 UI를 최신 데이터로 갱신합니다.
    /// </summary>
    public void UpdateInventoryUI(
        IReadOnlyList<InventoryItem> items,
        Action<string, string> onItemDropped,
        Action<string> onItemRemoved,
        Action<string> onItemEquipped
    )
    {
        // 이전에 남아있을 수 있는 플레이스홀더 정리
        foreach (Transform child in buttonContainer)
        {
            if (child && (child.name == "Placeholder"))
                GameObject.Destroy(child.gameObject);
        }

        foreach (var btn in inventoryButtons)
            btn.gameObject.SetActive(false);

        for (int i = 0; i < items.Count && i < inventoryButtons.Length; i++)
        {
            var item = items[i];
            if (InventoryGuards.IsInvalid(item))
                continue; // 잘못된 데이터는 스킵

            var button = inventoryButtons[i];
            button.gameObject.SetActive(true);

            var image = button.GetComponent<Image>();
            if (image != null && !string.IsNullOrEmpty(item.iconPath))
            {
                var icon = Resources.Load<Sprite>(item.iconPath);
                if (icon != null) image.sprite = icon;
            }

            // 중첩 수량 표시 라벨 확보
            var qty = EnsureQtyLabel(button.transform);
            if (item.data != null && item.data.type == "potion" && item.quantity >= 1)
            {
                qty.text = item.quantity.ToString();
                qty.enabled = true;
            }
            else
            {
                qty.text = "";
                qty.enabled = false;
            }

            var draggable = button.GetComponent<DraggableItemView>();
            if (draggable == null)
                draggable = button.gameObject.AddComponent<DraggableItemView>();

            Action<string, ItemOrigin> wrappedEquip = null;
            if (onItemEquipped != null)
                wrappedEquip = (uid, origin) => onItemEquipped.Invoke(uid);

            draggable.Initialize(item, ItemOrigin.Inventory, onItemDropped, onItemRemoved, wrappedEquip, null);

            var hover = button.GetComponent<ItemHoverTooltip>();
            if (hover == null) hover = button.gameObject.AddComponent<ItemHoverTooltip>();
            hover.SetItem(item);
            hover.SetContext(ItemOrigin.Inventory);   // 툴팁 컨텍스트 설정

            var CanvasGroup = button.GetComponent<CanvasGroup>();
            CanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 버튼 하위에 수량 표시 텍스트를 확보하거나 새로 만듭니다.
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
            t.anchoredPosition = new Vector2(-4, 4);
            t.sizeDelta = new Vector2(60, 24);

            var txt = go.AddComponent<Text>();
            txt.alignment = TextAnchor.LowerRight;

            // 기본 폰트 지정
            Font f = null;
            try
            {
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch { }

            // 기본 폰트가 없으면 OS 폰트로 대체
            if (f == null)
            {
                try { f = Font.CreateDynamicFontFromOSFont("Arial", 18); } catch { }
            }

            txt.font = f;            // 폰트가 null이어도 Text 생성은 가능
            txt.fontSize = 16;
            txt.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = true;
        }
        return t.GetComponent<Text>();
    }
}
