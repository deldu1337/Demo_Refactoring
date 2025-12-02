using UnityEngine;

[System.Serializable]
public class DropItem
{
    public GameObject itemPrefab;
    [Range(0f, 100f)] public float dropChance = 100f;
    public int minAmount = 1;
    public int maxAmount = 1;
}

public class ItemDropManager : MonoBehaviour
{
    [Header("드롭 설정")]
    public DropItem[] dropTable;
    public Transform dropPoint;
    public float dropRadius = 1f;

    /// <summary>
    /// 드롭 테이블을 확인하여 확률에 따라 아이템을 생성합니다.
    /// </summary>
    public void DropItems()
    {
        if (dropTable == null || dropTable.Length == 0)
        {
            Debug.LogWarning("드롭할 항목이 없습니다");
            return;
        }

        foreach (var drop in dropTable)
        {
            if (drop.itemPrefab == null)
            {
                Debug.LogWarning("드롭할 프리팹이 지정되지 않았습니다");
                continue;
            }

            float randomValue = Random.value * 100f;
            Debug.Log($"[{drop.itemPrefab.name}] 확률 검사 값: {randomValue} <= {drop.dropChance}");

            if (randomValue > drop.dropChance)
            {
                Debug.Log($"[{drop.itemPrefab.name}] 드롭되지 않았습니다");
                continue;
            }

            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            Debug.Log($"[{drop.itemPrefab.name}] 드롭 개수: {amount}");

            for (int i = 0; i < amount; i++)
            {
                Vector3 basePos = dropPoint != null ? dropPoint.position : transform.position;
                Vector3 offset = Random.insideUnitSphere * dropRadius;
                offset.y = 0;
                Vector3 dropPos = basePos + offset;

                Quaternion rot = Quaternion.Euler(90f, 0f, 0f);
                GameObject instance = Instantiate(drop.itemPrefab, dropPos, rot, transform.parent);
                Debug.Log($"[{drop.itemPrefab.name}] 드롭 완료 위치 {dropPos}");
            }
        }
    }
}
