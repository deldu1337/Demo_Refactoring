using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerStatsManager stats;
    private EquipmentPresenter equipmentPresenter;

    /// <summary>플레이어 스탯과 장비 연동자를 준비합니다.</summary>
    void Awake()
    {
        stats = PlayerStatsManager.Instance;
        equipmentPresenter = FindAnyObjectByType<EquipmentPresenter>();
    }

    /// <summary>게임 시작 시 현재 장비를 반영하여 스탯을 갱신합니다.</summary>
    void Start()
    {
        // 스탯 관리자가 준비되어 있다면 장비 정보를 반영해 드립니다.
        if (equipmentPresenter != null && stats != null)
            stats.RecalculateStats(equipmentPresenter.GetEquipmentSlots());
    }

    /// <summary>게임 종료 시점에 최신 스탯을 저장합니다.</summary>
    void OnApplicationQuit()
    {
        if (equipmentPresenter != null && stats != null)
            stats.RecalculateStats(equipmentPresenter.GetEquipmentSlots());

        if (stats != null)
            SaveLoadService.SavePlayerDataForRace(stats.Data.Race, stats.Data);
    }
}
