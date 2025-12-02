using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Header("포탈 충돌 감지")]
    [SerializeField] private LayerMask playerLayer;

    private StageManager stageManager;

    /// <summary>
    /// 맵 생성기에서 호출되어 스테이지 관리자를 준비합니다.
    /// </summary>
    /// <param name="owner">포탈을 소유한 타일 맵 생성기입니다.</param>
    public void Setup(TileMapGenerator owner)
    {
        stageManager = FindAnyObjectByType<StageManager>();
    }

    /// <summary>
    /// 콜라이더 기본 설정을 초기화합니다.
    /// </summary>
    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        int idx = LayerMask.NameToLayer("Player");
        if (idx >= 0) playerLayer = 1 << idx;
    }

    /// <summary>
    /// 플레이어가 포탈 영역에 들어오면 다음 스테이지로 이동합니다.
    /// </summary>
    /// <param name="other">트리거에 진입한 콜라이더입니다.</param>
    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;
        if (!isPlayer) return;

        if (stageManager != null) stageManager.NextStage();
    }
}
