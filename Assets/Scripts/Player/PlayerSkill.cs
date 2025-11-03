using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    private Animation animationComponent;
    private Quaternion savedRotation;
    private bool isUsingSkill = false;

    void Awake()
    {
        animationComponent = GetComponent<Animation>();
        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트가 Player 프리팹 또는 자식에 없습니다!");
    }

    void Update()
    {
        // A키로 스킬 시작
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartWhirlwind();
        }

        // 이동 입력으로 스킬 끊기 (예시: 우클릭 이동)
        if (isUsingSkill && Input.GetMouseButtonDown(1))
        {
            StopWhirlwind();
        }

        // 스킬 중에는 매 프레임 방향 강제 고정
        if (isUsingSkill)
        {
            transform.rotation = savedRotation;
        }
    }

    private void StartWhirlwind()
    {
        if (animationComponent == null) return;

        savedRotation = transform.rotation; // 방향 저장
        animationComponent.CrossFade("Whirlwind (ID 126 variation 0)", 0.1f);
        isUsingSkill = true;
        Debug.Log(savedRotation);
    }

    private void StopWhirlwind()
    {
        if (!isUsingSkill) return;

        animationComponent.Stop();
        transform.rotation = savedRotation;
        isUsingSkill = false;
    }
}
