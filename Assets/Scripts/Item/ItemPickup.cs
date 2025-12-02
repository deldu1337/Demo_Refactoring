using UnityEngine;

/// <summary>
/// 아이템을 표시하고 획득 동작을 처리하는 컴포넌트입니다.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [TextArea] public string itemInfo;
    [TextArea] public string id;
    public Sprite icon;

    [Header("툴팁 거리 설정")]
    public float showDistance = 7f;
    public bool hideWhenFar = true;

    private Transform player;
    private DataManager dataManager;
    private InventoryPresenter inventoryPresenter;
    private bool isShowing;

    /// <summary>
    /// 플레이어와 프레젠터를 찾고 데이터 매니저를 초기화합니다.
    /// </summary>
    private void Start()
    {
        if (GetComponentInParent<EquippedMarker>() != null)
        {
            enabled = false;
            return;
        }

        dataManager = DataManager.Instance;
        dataManager.LoadDatas();

        int playerLayer = LayerMask.NameToLayer("Player");
        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.layer == playerLayer) { player = obj.transform; break; }
        }

        inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();
        if (inventoryPresenter == null)
            Debug.LogError("InventoryPresenter를 찾지 못했습니다");
    }

    /// <summary>
    /// 플레이어와의 거리를 확인하며 말풍선 노출 여부를 갱신합니다.
    /// </summary>
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

    /// <summary>
    /// 아이템 정보를 안전하게 조회하여 이름과 티어를 반환합니다.
    /// </summary>
    private (string, string) GetItemInfoSafe()
    {
        if (!int.TryParse(id, out int parsedId)) return (itemInfo, null);
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId))
            return (itemInfo, null);

        var data = dataManager.dicItemDatas[parsedId];
        return (data.name, data.tier);
    }

    /// <summary>
    /// 툴팁에 표시할 안전한 이름 문자열을 반환합니다.
    /// </summary>
    private string GetDisplayNameSafe()
    {
        if (!int.TryParse(id, out int parsedId)) return itemInfo;
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId)) return itemInfo;
        return dataManager.dicItemDatas[parsedId].name;
    }

    /// <summary>
    /// 인벤토리에 아이템을 추가하고 월드 상의 객체를 제거합니다.
    /// </summary>
    private void Pickup()
    {
        if (inventoryPresenter == null) return;

        if (!int.TryParse(id, out int parsedId))
        {
            Debug.LogError($"[ItemPickup] id를 정수로 변환하지 못했습니다: '{id}'");
            return;
        }
        if (dataManager?.dicItemDatas == null || !dataManager.dicItemDatas.ContainsKey(parsedId))
        {
            Debug.LogError($"[ItemPickup] DataManager에서 id={parsedId} 항목을 찾지 못했습니다");
            return;
        }

        string prefabPath = $"Prefabs/{dataManager.dicItemDatas[parsedId].uniqueName}";
        inventoryPresenter.AddItem(parsedId, icon, prefabPath);

        ItemTooltipManager.Instance?.HideFor(transform);
        Destroy(gameObject);
    }

    /// <summary>
    /// 비활성화될 때 노출 중인 말풍선을 정리합니다.
    /// </summary>
    private void OnDisable()
    {
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.HideFor(transform);
    }
}
