public static class GameContext
{
    /// <summary>
    /// 현재 선택된 캐릭터의 종족 이름입니다 (예: "humanmale", "orc" 등).
    /// </summary>
    public static string SelectedRace;

    /// <summary>
    /// 선택된 캐릭터로 새 게임을 시작해야 하는지 여부입니다.
    /// </summary>
    public static bool IsNewGame;

    /// <summary>
    /// 기존 저장 여부와 관계없이 데이터를 강제로 초기화할지 여부입니다.
    /// </summary>
    public static bool ForceReset;
}
