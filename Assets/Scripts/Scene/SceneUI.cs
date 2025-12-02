using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneUI : MonoBehaviour
{
    public Button LoginButton;
    public Button QuitButton;
    public Sprite pressedSprite;

    // 씬 UI에서 버튼 클릭 리스너를 설정합니다.
    private void Start()
    {
        // 로그인 버튼을 누르면 게임을 시작하도록 연결합니다.
        LoginButton.onClick.AddListener(GameStart);
        // 종료 버튼을 누르면 게임을 종료하도록 연결합니다.
        QuitButton.onClick.AddListener(GameExit);
    }

    // 캐릭터 선택 씬으로 이동합니다.
    private void GameStart()
    {
        SceneManager.LoadScene("CharacterScene");
    }

    // 실행 환경에 맞춰 게임을 종료합니다.
    private void GameExit()
    {
#if UNITY_EDITOR
        // 에디터에서는 플레이 모드를 중지합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 게임에서는 애플리케이션을 종료합니다.
        Application.Quit();
#endif
    }
}
