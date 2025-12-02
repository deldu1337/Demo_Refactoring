using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private GameObject CharacterBackground;
    [SerializeField] private GameObject CharacterPanel;
    [SerializeField] private GameObject CharacterObject;
    [SerializeField] private Button StartButton;
    private Button[] CharacterButtons;
    private Image[] CharacterImages;
    private Image image;
    private int currentIndex = 0;

    void Start()
    {
        CharacterButtons = new Button[8];
        CharacterImages = new Image[8];
        image = CharacterBackground.GetComponent<Image>();
        StartButton.onClick.AddListener(GameStart);

        if (CharacterPanel != null)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = i;  // i를 로컬 변수에 복사
                CharacterButtons[i] = CharacterPanel.transform.GetChild(i).GetComponent<Button>();
                CharacterButtons[i].onClick.AddListener(() => ChangeCharacter(index));
                CharacterImages[i] = CharacterButtons[i].transform.GetChild(0).GetComponent<Image>();
            }
        }
        CharacterObject.transform.GetChild(0).gameObject.SetActive(true);
        currentIndex = 0;
        ApplySelection(currentIndex); // 기본 선택 반영
    }

    public void ChangeCharacter(int ButtonNum)
    {
        for (int i = 0; i < 8; i++)
        {
            bool active = (i == ButtonNum);
            CharacterObject.transform.GetChild(i).gameObject.SetActive(active);
            if (active) image.sprite = CharacterImages[i].sprite;
        }
        ApplySelection(ButtonNum); // 선택 반영
    }

    private void ApplySelection(int index)
    {
        string raceName = CharacterObject.transform.GetChild(index).gameObject.name;
        GameContext.SelectedRace = raceName;              // 전역 저장
        Debug.Log($"선택된 종족: {GameContext.SelectedRace}");
    }

    private void GameStart()
    {
        var race = string.IsNullOrEmpty(GameContext.SelectedRace)
            ? CharacterObject.transform.GetChild(0).gameObject.name
            : GameContext.SelectedRace;

        // 해당 종족 저장 존재 여부 확인
        var existing = SaveLoadService.LoadPlayerDataForRaceOrNull(race);
        GameContext.IsNewGame = (existing == null);  // 있으면 false(이어하기), 없으면 true(새 게임)

        // (선택) ‘새로 시작(덮어쓰기)’ 버튼을 따로 둘 경우:
        // GameContext.ForceReset = true; // 사용자가 진짜 덮어쓰기를 원할 때만!

        SceneManager.LoadScene("DungeonScene");
    }
}
