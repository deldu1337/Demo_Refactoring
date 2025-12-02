using UnityEngine;

public class LookHP : MonoBehaviour
{
    private Camera _cam; // HP UI가 바라볼 카메라

    /// <summary>
    /// 메인 카메라를 찾아 HP UI가 따라볼 대상을 설정한다.
    /// </summary>
    void Awake()
    {
        _cam = Camera.main; // 기본 메인 카메라 참조
    }

    /// <summary>
    /// HP UI를 항상 카메라를 향하도록 회전시킨다.
    /// </summary>
    void LateUpdate()
    {
        if (_cam == null) return; // 카메라가 없으면 처리하지 않음

        // UI가 카메라를 정면으로 바라보도록 회전을 맞춘다
        transform.rotation = _cam.transform.rotation;
    }
}
