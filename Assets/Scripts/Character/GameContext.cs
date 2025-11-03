public static class GameContext
{
    // 캐릭터 선택 씬에서 설정
    public static string SelectedRace;   // 예: "humanmale", "orc", ...
    public static bool IsNewGame;        // 캐릭터 선택 → 새 게임 시작 플래그
    public static bool ForceReset; // [새로 시작(덮어쓰기)] 전용
}
