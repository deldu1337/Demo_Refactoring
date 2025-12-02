using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UIHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color32 normalColor = new Color32(0, 0, 0, 225);
    [SerializeField] private Color32 hoverColor = new Color32(3, 62, 113, 255);
    private Image img;

    /// <summary>
    /// 이미지 컴포넌트를 준비하고 기본 색상을 설정합니다.
    /// </summary>
    private void Awake()
    {
        img = GetComponent<Image>();
        if (img) img.color = normalColor;
    }

    /// <summary>
    /// 포인터가 들어올 때 호버 색상으로 변경합니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (img) img.color = hoverColor;
    }

    /// <summary>
    /// 포인터가 나갈 때 기본 색상으로 되돌립니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (img) img.color = normalColor;
    }

    /// <summary>
    /// 런타임에 사용할 색상을 설정하고 즉시 적용합니다.
    /// </summary>
    /// <param name="normal">기본 색상입니다.</param>
    /// <param name="hover">호버 시 적용할 색상입니다.</param>
    public void SetColors(Color32 normal, Color32 hover)
    {
        normalColor = normal;
        hoverColor = hover;
        if (img) img.color = normalColor;
    }
}
