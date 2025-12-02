using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TileMapGenerator mapGen;
    [SerializeField] private Text stageText;

    [Header("Stage")]
    public int currentStage = 1;
    public int bossEvery = 3;

    private Color _defaultStageColor = Color.white;

    /// <summary>
    /// 시작 시 현재 스테이지 정보를 UI에 반영합니다.
    /// </summary>
    void Start()
    {
        UpdateStageUI();
    }

    /// <summary>
    /// 다음 스테이지로 이동하고 관련 UI와 맵을 갱신합니다.
    /// </summary>
    public void NextStage()
    {
        currentStage++;
        UpdateStageUI();
        if (mapGen != null) mapGen.ReloadMap();
    }

    /// <summary>
    /// 현재 스테이지가 보스 스테이지인지 확인합니다.
    /// </summary>
    public bool IsBossStage() => (bossEvery > 0) && (currentStage % bossEvery == 0);

    /// <summary>
    /// 스테이지 숫자와 색상을 UI에 표시합니다.
    /// </summary>
    public void UpdateStageUI()
    {
        if (!stageText) return;

        stageText.text = $"Stage {currentStage}";
        stageText.color = IsBossStage() ? Color.red : _defaultStageColor;
    }
}
