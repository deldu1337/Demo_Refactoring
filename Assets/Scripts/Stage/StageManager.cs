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

    void Start()
    {
        UpdateStageUI();
    }

    public void NextStage()
    {
        currentStage++;
        UpdateStageUI();
        if (mapGen != null) mapGen.ReloadMap();
    }

    public bool IsBossStage() => (bossEvery > 0) && (currentStage % bossEvery == 0);

    public void UpdateStageUI()
    {
        if (!stageText) return;

        stageText.text = $"Stage {currentStage}";
        // 보스면 빨간색, 아니면 기본색으로 복구
        stageText.color = IsBossStage() ? Color.red : _defaultStageColor;
    }
}
