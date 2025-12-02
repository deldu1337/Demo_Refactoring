using UnityEngine;

/// <summary>
/// 플레이어 스킬 사용과 종료를 관리합니다.
/// </summary>
public class PlayerSkill : MonoBehaviour
{
    private Animation animationComponent;
    private Quaternion savedRotation;
    private bool isUsingSkill = false;

    /// <summary>
    /// 스킬 사용에 필요한 애니메이션을 준비합니다.
    /// </summary>
    void Awake()
    {
        animationComponent = GetComponent<Animation>();
        if (animationComponent == null)
            Debug.LogError("Animation 컴포넌트를 찾지 못했습니다.");
    }

    /// <summary>
    /// 입력을 확인하여 스킬을 시작하거나 중단합니다.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartWhirlwind();
        }

        if (isUsingSkill && Input.GetMouseButtonDown(1))
        {
            StopWhirlwind();
        }

        if (isUsingSkill)
        {
            transform.rotation = savedRotation;
        }
    }

    /// <summary>
    /// 회오리 스킬을 시작하고 회전을 고정합니다.
    /// </summary>
    private void StartWhirlwind()
    {
        if (animationComponent == null) return;

        savedRotation = transform.rotation;
        animationComponent.CrossFade("Whirlwind (ID 126 variation 0)", 0.1f);
        isUsingSkill = true;
        Debug.Log(savedRotation);
    }

    /// <summary>
    /// 회오리 스킬을 멈추고 원래 회전을 복구합니다.
    /// </summary>
    private void StopWhirlwind()
    {
        if (!isUsingSkill) return;

        animationComponent.Stop();
        transform.rotation = savedRotation;
        isUsingSkill = false;
    }
}
