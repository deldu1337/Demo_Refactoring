using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Header("플레이어 레이어")]
    [SerializeField] private LayerMask playerLayer;

    private StageManager stageManager;

    public void Setup(TileMapGenerator owner)
    {
        // owner는 안 써도 되지만, 필요시 참조 가능
        stageManager = FindAnyObjectByType<StageManager>();
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        int idx = LayerMask.NameToLayer("Player");
        if (idx >= 0) playerLayer = 1 << idx;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;
        if (!isPlayer) return;

        if (stageManager != null) stageManager.NextStage();
    }
}
