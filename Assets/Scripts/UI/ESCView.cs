using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ESCView : MonoBehaviour
{
    [SerializeField] private GameObject escUI;
    private Button LogoutButton;
    private Button SelectCharacterButton;
    private Button ExitGameButton;
    private Button ReturnToGameButton;
    private Button ExitButton;
    private bool show = false;

    /// <summary>
    /// ESC UI를 찾고 버튼 동작을 연결합니다.
    /// </summary>
    void Start()
    {
        UIEscapeStack.GetOrCreate();

        if (escUI == null)
            escUI = GameObject.Find("escUI");

        if (escUI != null)
        {
            LogoutButton = escUI.transform.GetChild(2).GetComponent<Button>();
            SelectCharacterButton = escUI.transform.GetChild(3).GetComponent<Button>();
            ExitGameButton = escUI.transform.GetChild(4).GetComponent<Button>();
            ReturnToGameButton = escUI.transform.GetChild(5).GetComponent<Button>();
            ExitButton = escUI.transform.GetChild(6).GetComponent<Button>();

            LogoutButton.onClick.AddListener(Logout);
            SelectCharacterButton.onClick.AddListener(SelectCharacter);
            ExitGameButton.onClick.AddListener(ExitGame);
            ReturnToGameButton.onClick.AddListener(ToggleESC);
            ExitButton.onClick.AddListener(ToggleESC);
        }

        if (escUI) escUI.SetActive(show);
    }

    /// <summary>
    /// ESC 키 입력을 감지하여 UI를 토글하거나 스택 상단을 닫습니다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (UIEscapeStack.Instance != null && UIEscapeStack.Instance.PopTop())
                return;

            ToggleESC();
        }
    }

    /// <summary>
    /// ESC UI의 표시 여부를 전환합니다.
    /// </summary>
    public void ToggleESC()
    {
        if (!escUI) return;
        show = !show;
        escUI.SetActive(show);
    }

    /// <summary>
    /// 로그인 화면으로 이동합니다.
    /// </summary>
    public void Logout()
    {
        SceneManager.LoadScene("LoginScene");
    }

    /// <summary>
    /// 캐릭터 선택 화면으로 이동합니다.
    /// </summary>
    public void SelectCharacter()
    {
        SceneManager.LoadScene("CharacterScene");
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
