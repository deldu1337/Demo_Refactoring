using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public TileMapGenerator mapGenerator;

    private GameObject currentPlayer;

    void Start()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("TileMapGenerator를 연결해주세요!");
            return;
        }

        mapGenerator.OnMapGenerated += ReloadPlayer;

        if (string.IsNullOrEmpty(GameContext.SelectedRace))
        {
            // 캐릭터 선택 없이 바로 들어온 경우 이어하기 시나리오일 수 있으니 기본값만 방어
            GameContext.SelectedRace = "humanmale";
        }

        RespawnPlayer();
    }

    private void OnDestroy()
    {
        if (mapGenerator != null) mapGenerator.OnMapGenerated -= ReloadPlayer;
    }

    public void ReloadPlayer()
    {
        if (currentPlayer == null) return; // ★ 가드
        RectInt playerRoom = mapGenerator.GetPlayerRoom();
        Vector3 newPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);
        currentPlayer.transform.position = newPos;
    }

    public void RespawnPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);

        RectInt playerRoom = mapGenerator.GetPlayerRoom();
        Vector3 spawnPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);

        string prefabName = GameContext.SelectedRace; // 예: "humanmale"
        GameObject prefab = Resources.Load<GameObject>($"Characters/{prefabName}");
        if (prefab == null)
        {
            Debug.LogError($"프리팹 'Resources/Characters/{prefabName}.prefab' 를 찾을 수 없습니다.");
            return;
        }

        currentPlayer = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

        // ★ 스탯 초기화
        var stats = currentPlayer.GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            stats.InitializeForSelectedRace(); // 아래 3)에서 구현
        }
        else
        {
            Debug.LogWarning("PlayerStatsManager 컴포넌트를 찾지 못했습니다.");
        }
    }
}
