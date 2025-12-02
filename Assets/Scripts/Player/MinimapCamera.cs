using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 카메라의 위치와 방향을 플레이어에 맞춰 조정합니다.
/// </summary>
public class MinimapCamera : MonoBehaviour
{
    private Camera minimapCam;
    private Image arrowImage;
    private Vector3 distance;
    private Vector3 fixedRotation = new Vector3(90f, 45f, 0f);

    /// <summary>
    /// 미니맵 카메라와 화살표 UI를 준비합니다.
    /// </summary>
    void Start()
    {
        minimapCam = GameObject.FindGameObjectWithTag("Minimap").GetComponent<Camera>();
        if (minimapCam == null)
        {
            Debug.LogError("Minimap 태그의 카메라를 찾지 못했습니다.");
            return;
        }
        Transform canvasTransform = transform.Find("Canvas");
        if (canvasTransform != null)
        {
            Transform arrowTransform = canvasTransform.Find("ArrowImage");
            if (arrowTransform != null)
            {
                arrowImage = arrowTransform.GetComponent<Image>();
            }
        }

        minimapCam.transform.eulerAngles = fixedRotation;

        Vector3 vector3 = new Vector3(transform.position.x, 50f, transform.position.z);
        distance = vector3 - transform.position;
    }

    /// <summary>
    /// 미니맵 카메라 위치와 UI 화살표 회전을 갱신합니다.
    /// </summary>
    void FixedUpdate()
    {
        minimapCam.transform.position = distance + transform.position;

        distance = minimapCam.transform.position - transform.position;

        float playerYRotation = transform.eulerAngles.y;
        arrowImage.rectTransform.localEulerAngles = new Vector3(45f, 0f, -playerYRotation + 45f);
    }
}
