using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image tensPlace;   // 10의 자리
    [SerializeField] private Image onesPlace;   // 1의 자리

    private PlayerStatsManager playerStats;     // 싱글톤 참조
    private Sprite[] numberSprites;

    private void Awake()
    {
        // 숫자 스프라이트 로드 (Assets/Resources/Prefabs/Levels/0~9)
        numberSprites = Resources.LoadAll<Sprite>("Prefabs/Levels");
        if (numberSprites == null || numberSprites.Length < 10)
            Debug.LogError("[LevelUI] Prefabs/Levels 폴더에 0~9 스프라이트가 필요합니다!");

        // 필요 시, 하이어라키에서 직접 찾아 연결 (직접 드래그 연결되어 있으면 생략 가능)
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

    private void OnEnable()
    {
        // 싱글톤/데이터 준비될 때까지 대기 → 구독 + 초기 1회 렌더
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        while (PlayerStatsManager.Instance == null || PlayerStatsManager.Instance.Data == null)
            yield return null;

        playerStats = PlayerStatsManager.Instance;

        // 중복 방지 후 구독
        playerStats.OnLevelUp -= UpdateLevelUI;
        playerStats.OnLevelUp += UpdateLevelUI;

        // 초기 1회 강제 렌더
        UpdateLevelUI(playerStats.Data.Level);
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= UpdateLevelUI;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= UpdateLevelUI;
    }

    /// <summary>
    /// 레벨 값으로 UI 업데이트 (이벤트 기반)
    /// </summary>
    private void UpdateLevelUI(int level)
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            Debug.LogWarning("[LevelUI] 숫자 스프라이트가 준비되지 않음");
            return;
        }

        int tens = level / 10;  // 10의 자리
        int ones = level % 10;  // 1의 자리

        // 10의 자리
        if (tensPlace)
        {
            if (tens > 0)
            {
                tensPlace.sprite = numberSprites[tens];
                tensPlace.enabled = true;
            }
            else
            {
                tensPlace.enabled = false; // 한 자리 레벨이면 숨김
            }
        }

        // 1의 자리
        if (onesPlace)
        {
            onesPlace.sprite = numberSprites[ones];
            onesPlace.enabled = true;
        }
    }
}
