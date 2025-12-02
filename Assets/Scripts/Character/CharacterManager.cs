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

    /// <summary>
    /// 캐릭터 선택 UI를 초기화하고 버튼 이벤트를 설정합니다.
    /// </summary>
    void Start()
    {
        // 캐릭터 및 이미지 배열을 초기화합니다.
        CharacterButtons = new Button[8];
        CharacterImages = new Image[8];
        image = CharacterBackground.GetComponent<Image>();

        // 시작 버튼을 눌렀을 때 게임을 시작하도록 등록합니다.
        StartButton.onClick.AddListener(GameStart);

        if (CharacterPanel != null)
        {
            for (int i = 0; i < 8; i++)
            {
                // 반복문 클로저 문제를 피하기 위해 인덱스를 캡처합니다.
                int index = i;
                CharacterButtons[i] = CharacterPanel.transform.GetChild(i).GetComponent<Button>();
                CharacterButtons[i].onClick.AddListener(() => ChangeCharacter(index));
                CharacterImages[i] = CharacterButtons[i].transform.GetChild(0).GetComponent<Image>();
            }
        }

        // 기본 캐릭터를 활성화하고 선택 상태를 적용합니다.
        CharacterObject.transform.GetChild(0).gameObject.SetActive(true);
        currentIndex = 0;
        ApplySelection(currentIndex);
    }

    /// <summary>
    /// 선택한 캐릭터 버튼에 따라 캐릭터를 교체합니다.
    /// </summary>
    /// <param name="ButtonNum">선택된 버튼의 인덱스</param>
    public void ChangeCharacter(int ButtonNum)
    {
        for (int i = 0; i < 8; i++)
        {
            bool active = (i == ButtonNum);
            CharacterObject.transform.GetChild(i).gameObject.SetActive(active);
            if (active) image.sprite = CharacterImages[i].sprite;
        }
        ApplySelection(ButtonNum);
    }

    /// <summary>
    /// 현재 선택된 캐릭터 이름을 저장하고 로그로 남깁니다.
    /// </summary>
    /// <param name="index">선택한 캐릭터의 인덱스</param>
    private void ApplySelection(int index)
    {
        string raceName = CharacterObject.transform.GetChild(index).gameObject.name;
        GameContext.SelectedRace = raceName;              // 선택된 종족 이름을 유지합니다.
        Debug.Log($"선택된 종족 : {GameContext.SelectedRace}");
    }

    /// <summary>
    /// 선택한 캐릭터 정보를 기반으로 게임 씬을 로드합니다.
    /// </summary>
    private void GameStart()
    {
        var race = string.IsNullOrEmpty(GameContext.SelectedRace)
            ? CharacterObject.transform.GetChild(0).gameObject.name
            : GameContext.SelectedRace;

        // 선택한 종족에 대한 저장 데이터 존재 여부를 확인합니다.
        var existing = SaveLoadService.LoadPlayerDataForRaceOrNull(race);
        GameContext.IsNewGame = (existing == null);  // 저장 없음: true, 저장 있음: false입니다.

        // 필요 시 캐릭터 데이터를 강제로 초기화하고 싶다면 아래를 활성화합니다.
        // GameContext.ForceReset = true;

        SceneManager.LoadScene("DungeonScene");
    }
}
