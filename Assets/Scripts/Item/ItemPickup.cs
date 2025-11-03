using UnityEngine;

// 아이템 픽업 스크립트 (근접 다중 툴팁 + 클릭 획득)
public class ItemPickup : MonoBehaviour
{
    [TextArea] public string itemInfo;
    [TextArea] public string id;
    public Sprite icon;

    [Header("근접/툴팁 설정")]
    public float showDistance = 7f;
    public bool hideWhenFar = true;

    private Transform player;
    private DataManager dataManager;
    private InventoryPresenter inventoryPresenter;
    private bool isShowing;

    private void Start()
    {
        // 장착 본(EquippedMarker) 아래에 있으면 자신 비활성화
        if (GetComponentInParent<EquippedMarker>() != null)
        {
            enabled = false;         // Update 비활성화
            return;
        }

        dataManager = DataManager.Instance;
        dataManager.LoadDatas();

        // Player 찾기(레이어)
        int playerLayer = LayerMask.NameToLayer("Player");
        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.layer == playerLayer) { player = obj.transform; break; }
        }

        inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        if (inventoryPresenter == null)
            Debug.LogError("InventoryPresenter를 찾을 수 없습니다!");
    }

    private void Update()
    {
        if (!player || ItemTooltipManager.Instance == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        bool inRange = dist <= showDistance;

        if (inRange && !isShowing)
        {
            isShowing = true;

            var (name, tier) = GetItemInfoSafe();

            ItemTooltipManager.Instance.ShowFor(
                transform,
                name,
                tier,
                onClick: Pickup
            );
        }
        else if (!inRange && isShowing && hideWhenFar)
        {
            isShowing = false;
            ItemTooltipManager.Instance.HideFor(transform);
        }
    }

    private (string, string) GetItemInfoSafe()
    {
        if (!int.TryParse(id, out int parsedId)) return (itemInfo, null);
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId))
            return (itemInfo, null);

        var data = dataManager.dicItemDatas[parsedId];
        return (data.name, data.tier);
    }


    private string GetDisplayNameSafe()
    {
        if (!int.TryParse(id, out int parsedId)) return itemInfo;
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId)) return itemInfo;
        return dataManager.dicItemDatas[parsedId].name;
    }

    private void Pickup()
    {
        if (inventoryPresenter == null) return;

        if (!int.TryParse(id, out int parsedId))
        {
            Debug.LogError($"[ItemPickup] id 파싱 실패: '{id}'");
            return;
        }
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId))
        {
            Debug.LogError($"[ItemPickup] DataManager에 id={parsedId} 없음");
            return;
        }

        string prefabPath = $"Prefabs/{dataManager.dicItemDatas[parsedId].uniqueName}";
        inventoryPresenter.AddItem(parsedId, icon, prefabPath);

        // 내 툴팁 닫고 자신 제거
        ItemTooltipManager.Instance?.HideFor(transform);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 씬 전환/파괴 시에도 안전 정리
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.HideFor(transform);
    }
}
