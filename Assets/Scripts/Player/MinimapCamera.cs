using UnityEngine;
using UnityEngine.UI;

public class MinimapCamera : MonoBehaviour
{
    private Camera minimapCam; // Minimap 카메라 참조
    private Image arrowImage;
    private Vector3 distance; // 카메라와 플레이어 간 상대 위치 (offset)
    private Vector3 fixedRotation = new Vector3(90f, 45f, 0f); // 미니맵 카메라 고정 회전값

    void Start()
    {
        // Minimap 태그를 가진 카메라 찾아서 저장
        minimapCam = GameObject.FindGameObjectWithTag("Minimap").GetComponent<Camera>();
        if (minimapCam == null)
        {
            Debug.LogError("Minimap 태그를 가진 카메라를 찾을 수 없습니다!");
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

        // 카메라 회전 고정
        minimapCam.transform.eulerAngles = fixedRotation;

        // 초기 카메라 위치를 기준으로 offset 계산
        Vector3 vector3 = new Vector3(transform.position.x, 50f, transform.position.z); // 카메라 초기 위치
        distance = vector3 - transform.position;       // 플레이어 위치 기준 offset 계산
       
        // 주석 처리됨, 고정된 초기 벡터로 offset 설정
    }

    void FixedUpdate()
    {
        // 카메라 위치를 플레이어 위치 + offset으로 설정
        minimapCam.transform.position = distance + transform.position;

        // 현재 카메라 위치 기준 offset 갱신
        distance = minimapCam.transform.position - transform.position;

        // 플레이어 y축 회전(방향) 기준으로 UI Z축 회전
        float playerYRotation = transform.eulerAngles.y;
        arrowImage.rectTransform.localEulerAngles = new Vector3(45f, 0f, -playerYRotation + 45f);
        // 45는 인스펙터에서 설정한 x값 유지, y는 0으로 고정, z만 플레이어 회전 반영
    }
}