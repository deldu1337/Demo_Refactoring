using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneUI : MonoBehaviour
{
    public Button LoginButton;
    public Button QuitButton;
    public Sprite pressedSprite;

    /// <summary>
    /// 씬이 시작될 때 버튼 클릭 이벤트를 설정해 드립니다.
    /// </summary>
    private void Start()
    {
        // 로그인 버튼을 누르시면 캐릭터 선택 화면으로 이동하도록 등록해 드립니다.
        LoginButton.onClick.AddListener(GameStart);
        // 종료 버튼을 누르시면 게임을 종료하도록 등록해 드립니다.
        QuitButton.onClick.AddListener(GameExit);
    }

    /// <summary>
    /// 캐릭터 선택 씬을 불러와서 게임을 시작해 드립니다.
    /// </summary>
    private void GameStart()
    {
        SceneManager.LoadScene("CharacterScene");
    }

    /// <summary>
    /// 실행 환경에 맞춰 게임을 정중하게 종료해 드립니다.
    /// </summary>
    private void GameExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
