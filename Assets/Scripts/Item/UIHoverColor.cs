using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UIHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 225);
    [SerializeField] private Color32 hoverColor = new Color32(3, 62, 113, 255);
    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img) img.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (img) img.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (img) img.color = normalColor;
    }

    // 외부에서 색을 바꾸고 싶을 때를 위한 세터 (선택)
    public void SetColors(Color32 normal, Color32 hover)
    {
        normalColor = normal;
        hoverColor = hover;
        if (img) img.color = normalColor;
    }
}
