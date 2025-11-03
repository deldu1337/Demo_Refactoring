using UnityEngine;

public class LookHP : MonoBehaviour
{
    private Camera _cam; // 카메라 참조 (메인 카메라)

    void Awake()
    {
        _cam = Camera.main; // 씬에서 MainCamera 자동 참조
    }

    void LateUpdate()
    {
        if (_cam == null) return; // 카메라가 없으면 아무것도 하지 않음

        // UI나 오브젝트가 카메라를 항상 바라보도록 회전 적용
        // 주로 HP 바, 이름표, 아이템 툴팁 등에 사용
        transform.rotation = _cam.transform.rotation;
    }
}