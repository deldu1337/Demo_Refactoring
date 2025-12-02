using UnityEngine;

/// <summary>
/// 플레이어 생성과 맵 재생성 시 위치 이동을 담당합니다.
/// </summary>
public class PlayerSpawn : MonoBehaviour
{
    public TileMapGenerator mapGenerator;

    private GameObject currentPlayer;

    /// <summary>
    /// 맵 생성 이벤트를 구독하고 기본 종족을 설정합니다.
    /// </summary>
    void Start()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("TileMapGenerator를 할당해 주세요.");
            return;
        }

        mapGenerator.OnMapGenerated += ReloadPlayer;

        if (string.IsNullOrEmpty(GameContext.SelectedRace))
        {
            GameContext.SelectedRace = "humanmale";
        }

        RespawnPlayer();
    }

    /// <summary>
    /// 객체 파괴 시 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (mapGenerator != null) mapGenerator.OnMapGenerated -= ReloadPlayer;
    }

    /// <summary>
    /// 맵이 다시 생성될 때 플레이어를 새로운 방 중앙으로 옮깁니다.
    /// </summary>
    public void ReloadPlayer()
    {
        if (currentPlayer == null) return;
        RectInt playerRoom = mapGenerator.GetPlayerRoom();
        Vector3 newPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);
        currentPlayer.transform.position = newPos;
    }

    /// <summary>
    /// 플레이어 프리팹을 새로 생성하고 스탯을 초기화합니다.
    /// </summary>
    public void RespawnPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);

        RectInt playerRoom = mapGenerator.GetPlayerRoom();
        Vector3 spawnPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);

        string prefabName = GameContext.SelectedRace;
        GameObject prefab = Resources.Load<GameObject>($"Characters/{prefabName}");
        if (prefab == null)
        {
            Debug.LogError($"Resources/Characters/{prefabName}.prefab 파일을 찾지 못했습니다.");
            return;
        }

        currentPlayer = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

        var stats = currentPlayer.GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            stats.InitializeForSelectedRace();
        }
        else
        {
            Debug.LogWarning("PlayerStatsManager 컴포넌트를 찾지 못했습니다.");
        }
    }
}
