using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [Header("UI 숫자")]
    [SerializeField] private Image tensPlace;
    [SerializeField] private Image onesPlace;

    private PlayerStatsManager playerStats;
    private Sprite[] numberSprites;

    /// <summary>
    /// 숫자 스프라이트와 UI 참조를 초기화합니다.
    /// </summary>
    private void Awake()
    {
        numberSprites = Resources.LoadAll<Sprite>("Prefabs/Levels");
        if (numberSprites == null || numberSprites.Length < 10)
            Debug.LogError("[LevelUI] Prefabs/Levels 경로에 0~9 스프라이트가 없습니다.");

        if (!tensPlace || !onesPlace)
        {
            Transform statusUI = GameObject.Find("LevelUI")?.transform;
            if (statusUI != null)
            {
                tensPlace = tensPlace ? tensPlace : statusUI.GetChild(0).GetComponent<Image>();
                onesPlace = onesPlace ? onesPlace : statusUI.GetChild(1).GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// 활성화 시 플레이어 정보가 준비될 때까지 대기 후 바인딩합니다.
    /// </summary>
    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    /// <summary>
    /// 플레이어 정보가 준비되면 레벨 변경 이벤트를 연결합니다.
    /// </summary>
    private IEnumerator BindWhenReady()
    {
        while (PlayerStatsManager.Instance == null || PlayerStatsManager.Instance.Data == null)
            yield return null;

        playerStats = PlayerStatsManager.Instance;

        playerStats.OnLevelUp -= UpdateLevelUI;
        playerStats.OnLevelUp += UpdateLevelUI;

        UpdateLevelUI(playerStats.Data.Level);
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= UpdateLevelUI;
    }

    /// <summary>
    /// 파괴될 때 이벤트 구독을 안전하게 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= UpdateLevelUI;
    }

    /// <summary>
    /// 레벨 값을 UI 숫자 이미지로 표시합니다.
    /// </summary>
    private void UpdateLevelUI(int level)
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            Debug.LogWarning("[LevelUI] 숫자 스프라이트가 없습니다.");
            return;
        }

        int tens = level / 10;
        int ones = level % 10;

        if (tensPlace)
        {
            if (tens > 0)
            {
                tensPlace.sprite = numberSprites[tens];
                tensPlace.enabled = true;
            }
            else
            {
                tensPlace.enabled = false;
            }
        }

        if (onesPlace)
        {
            onesPlace.sprite = numberSprites[ones];
            onesPlace.enabled = true;
        }
    }
}
