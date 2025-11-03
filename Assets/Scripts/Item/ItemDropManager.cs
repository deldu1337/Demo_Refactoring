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

    public void DropItems()
    {
        if (dropTable == null || dropTable.Length == 0)
        {
            Debug.LogWarning("드롭 테이블이 비어 있습니다!");
            return;
        }

        foreach (var drop in dropTable)
        {
            if (drop.itemPrefab == null)
            {
                Debug.LogWarning("DropItem의 itemPrefab이 비어 있음!");
                continue;
            }

            float randomValue = Random.value * 100f;
            Debug.Log($"[{drop.itemPrefab.name}] 드롭 확률 체크: {randomValue} <= {drop.dropChance}");

            if (randomValue > drop.dropChance)
            {
                Debug.Log($"[{drop.itemPrefab.name}] 드롭 실패 (확률 미달)");
                continue;
            }

            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            Debug.Log($"[{drop.itemPrefab.name}] 드롭 확정! 수량: {amount}");

            for (int i = 0; i < amount; i++)
            {
                Vector3 basePos = dropPoint != null ? dropPoint.position : transform.position;
                Vector3 offset = Random.insideUnitSphere * dropRadius;
                offset.y = 0;
                Vector3 dropPos = basePos + offset;

                // X축으로 90도 회전
                Quaternion rot = Quaternion.Euler(90f, 0f, 0f);
                GameObject instance = Instantiate(drop.itemPrefab, dropPos, rot, transform.parent);
                Debug.Log($"[{drop.itemPrefab.name}] 생성 완료 at {dropPos}");
            }
        }
    }
}
