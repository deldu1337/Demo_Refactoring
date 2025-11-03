using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button exitButton;

    private Button[] inventoryButtons;

    public void Initialize(Action onExit)
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(() => onExit?.Invoke());

        inventoryButtons = buttonContainer.GetComponentsInChildren<Button>(true);
        Show(false);
    }

    public void Show(bool show) => inventoryPanel?.SetActive(show);

    public void UpdateInventoryUI(
        IReadOnlyList<InventoryItem> items,
        Action<string, string> onItemDropped,
        Action<string> onItemRemoved,
        Action<string> onItemEquipped
    )
    {
        // UpdateInventoryUI 시작 부분
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
                continue; // UI 슬롯 배정 안함

            var button = inventoryButtons[i];
            button.gameObject.SetActive(true);

            var image = button.GetComponent<Image>();
            if (image != null && !string.IsNullOrEmpty(item.iconPath))
            {
                var icon = Resources.Load<Sprite>(item.iconPath);
                if (icon != null) image.sprite = icon;
            }

            // ★ 우하단 수량 라벨(필요 시 생성 후 갱신)
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
            hover.SetContext(ItemOrigin.Inventory);   // ★ 추가: 인벤토리 컨텍스트

            var CanvasGroup = button.GetComponent<CanvasGroup>();
            CanvasGroup.blocksRaycasts = true;
        }
    }

    // 가장 위에 using 유지:
    // using UnityEngine.UI;

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

            // ★ 변경된 부분: 내장 폰트 이름 교체 + 폴백
            Font f = null;
            try
            {
                // 새 내장 폰트 이름
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch { /* 일부 버전에서 예외 가능 */ }

            // 만약 위에서 못 찾으면 OS 폰트 폴백 시도
            if (f == null)
            {
                try { f = Font.CreateDynamicFontFromOSFont("Arial", 18); } catch { }
            }

            txt.font = f;            // f가 null이어도 Text는 동작하지만 가능하면 세팅됨
            txt.fontSize = 16;
            txt.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = true;
        }
        return t.GetComponent<Text>();
    }

}

