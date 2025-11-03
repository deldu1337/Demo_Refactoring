using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneUI : MonoBehaviour
{
    public Button LoginButton;
    public Button QuitButton;
    public Sprite pressedSprite;

    private void Start()
    {
        LoginButton.onClick.AddListener(GameStart);
        QuitButton.onClick.AddListener(GameExit);
    }

    private void GameStart()
    {
        SceneManager.LoadScene("CharacterScene");
    }

    // 게임 종료 함수는 전처리 사용(#if, #else, #endif)
    private void GameExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif   
    }
}