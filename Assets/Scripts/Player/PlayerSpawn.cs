//using UnityEngine;

//public class PlayerSpawn : MonoBehaviour
//{
//    public GameObject player;               // 스폰할 플레이어 프리팹
//    public TileMapGenerator mapGenerator;   // 맵 생성기 참조

//    private GameObject currentPlayer;        // 실제 씬에 존재하는 플레이어

//    void Start()
//    {
//        // mapGenerator가 연결되지 않았으면 오류 출력 후 종료
//        if (mapGenerator == null)
//        {
//            Debug.LogError("TileMapGenerator를 연결해주세요!");
//            return;
//        }

//        // 맵 생성 완료 이벤트에 RespawnPlayer 메서드 등록
//        mapGenerator.OnMapGenerated += ReloadPlayer;

//        // 게임 시작 시 플레이어 스폰
//        RespawnPlayer();
//    }

//    public void ReloadPlayer()
//    {
//        // 맵 생성 완료 후 새 위치로 이동
//        RectInt playerRoom = mapGenerator.GetPlayerRoom();
//        Vector3 newPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);
//        currentPlayer.transform.position = newPos;
//    }

//    public void RespawnPlayer()
//    {
//        // 기존 플레이어 제거
//        if (currentPlayer != null)
//            Destroy(currentPlayer);

//        // 플레이어 시작 위치 계산
//        RectInt playerRoom = mapGenerator.GetPlayerRoom();
//        Vector3 spawnPos = new Vector3(playerRoom.center.x, 0.5f, playerRoom.center.y);

//        // 새로운 플레이어 인스턴스 생성
//        currentPlayer = Instantiate(player, spawnPos, Quaternion.identity, transform);
//    }
//}

// PlayerSpawn.cs (교체해서 사용)
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
