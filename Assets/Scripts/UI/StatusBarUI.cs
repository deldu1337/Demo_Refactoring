using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StatusBarUI : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image mpBar;
    [SerializeField] private Image expBar;

    [Header("Portrait")]
    [SerializeField] private GameObject Circle;
    [SerializeField] private string portraitsFolder = "Portraits";
    [SerializeField] private string defaultPortraitName = "default";

    private Image face;

    [Header("StatusUI Hierarchy Auto-Wire")]
    [SerializeField] private string statusUIRootName = "StatusUI";
    [SerializeField] private int hpIndex = 3;
    [SerializeField] private int mpIndex = 4;
    [SerializeField] private int expIndex = 5;

    private PlayerStatsManager playerStats;
    private Coroutine initRoutine;

    /// <summary>
    /// 초상화 이미지를 초기화합니다.
    /// </summary>
    private void Start()
    {
        if (Circle != null)
            face = Circle.GetComponent<Image>();
    }

    /// <summary>
    /// 활성화될 때 플레이어 정보를 기다립니다.
    /// </summary>
    private void OnEnable()
    {
        initRoutine = StartCoroutine(InitializeWhenReady());
    }

    /// <summary>
    /// 비활성화 시 초기화 루틴을 중단하고 이벤트를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (initRoutine != null) { StopCoroutine(initRoutine); initRoutine = null; }
        UnsubscribeEvents();
    }

    /// <summary>
    /// 플레이어 정보와 UI 참조를 준비한 뒤 이벤트를 구독합니다.
    /// </summary>
    private IEnumerator InitializeWhenReady()
    {
        while (PlayerStatsManager.Instance == null)
            yield return null;

        playerStats = PlayerStatsManager.Instance;

        Transform statusUI = null;
        while (statusUI == null)
        {
            var go = GameObject.Find(statusUIRootName);
            if (go != null) statusUI = go.transform;
            else yield return null;
        }

        if (hpBar == null && statusUI.childCount > hpIndex)
            hpBar = statusUI.GetChild(hpIndex).GetComponentInChildren<Image>();
        if (mpBar == null && statusUI.childCount > mpIndex)
            mpBar = statusUI.GetChild(mpIndex).GetComponentInChildren<Image>();
        if (expBar == null && statusUI.childCount > expIndex)
            expBar = statusUI.GetChild(expIndex).GetComponentInChildren<Image>();

        SubscribeEvents();

        RefreshPortrait();
        RefreshAll();
    }

    /// <summary>
    /// 플레이어 상태 이벤트를 구독합니다.
    /// </summary>
    private void SubscribeEvents()
    {
        if (playerStats == null) return;

        playerStats.OnHPChanged += OnHPChanged;
        playerStats.OnMPChanged += OnMPChanged;
        playerStats.OnExpChanged += OnExpChanged;
        playerStats.OnLevelUp += OnLevelUp;

        PlayerStatsManager.OnPlayerRevived -= OnPlayerRevivedRefreshPortrait;
        PlayerStatsManager.OnPlayerRevived += OnPlayerRevivedRefreshPortrait;
    }

    /// <summary>
    /// 플레이어 상태 이벤트를 해제합니다.
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnHPChanged -= OnHPChanged;
            playerStats.OnMPChanged -= OnMPChanged;
            playerStats.OnExpChanged -= OnExpChanged;
            playerStats.OnLevelUp -= OnLevelUp;
        }
        PlayerStatsManager.OnPlayerRevived -= OnPlayerRevivedRefreshPortrait;
    }

    /// <summary>
    /// 초상화 이미지를 현재 종족에 맞게 갱신합니다.
    /// </summary>
    private void RefreshPortrait()
    {
        if (face == null) return;

        string race = "humanmale";
        if (playerStats != null && playerStats.Data != null && !string.IsNullOrEmpty(playerStats.Data.Race))
            race = playerStats.Data.Race;

        string spriteName = MapRaceToPortraitName(race);

        Sprite sp = Resources.Load<Sprite>($"{portraitsFolder}/{spriteName}");
        if (sp == null)
        {
            sp = Resources.Load<Sprite>($"{portraitsFolder}/{defaultPortraitName}");
#if UNITY_EDITOR
            if (sp == null)
                Debug.LogWarning($"[StatusBarUI] Portrait not found: {portraitsFolder}/{spriteName} (also no default).");
#endif
        }

        face.sprite = sp;
        face.enabled = (sp != null);
        face.preserveAspect = true;
    }

    /// <summary>
    /// 종족 문자열을 포트레이트 이름으로 변환합니다.
    /// </summary>
    private string MapRaceToPortraitName(string race)
    {
        if (string.IsNullOrEmpty(race)) return "humanmale";

        switch (race.ToLowerInvariant())
        {
            case "humanmale": return "humanmale";
            case "dwarfmale": return "dwarfmale";
            case "gnomemale": return "gnomemale";
            case "nightelfmale": return "nightelfmale";
            case "orcmale": return "orcmale";
            case "trollmale": return "trollmale";
            case "goblinmale": return "goblinmale";
            case "scourgefemale": return "scourgefemale";
            default: return race.ToLowerInvariant();
        }
    }

    /// <summary>
    /// 부활 시 초상화를 새로고칩니다.
    /// </summary>
    private void OnPlayerRevivedRefreshPortrait()
    {
        RefreshPortrait();
    }

    /// <summary>
    /// HP 바를 최신 값으로 갱신합니다.
    /// </summary>
    private void OnHPChanged(float cur, float max)
    {
        if (hpBar == null) return;
        hpBar.fillAmount = (max > 0f) ? cur / max : 0f;
    }

    /// <summary>
    /// MP 바를 최신 값으로 갱신합니다.
    /// </summary>
    private void OnMPChanged(float cur, float max)
    {
        if (mpBar == null) return;
        mpBar.fillAmount = (max > 0f) ? cur / max : 0f;
    }

    /// <summary>
    /// 경험치 바를 최신 값으로 갱신합니다.
    /// </summary>
    private void OnExpChanged(int level, float exp)
    {
        if (expBar == null || playerStats == null || playerStats.Data == null) return;
        float ratio = Mathf.Clamp01(playerStats.Data.Exp / Mathf.Max(1f, playerStats.Data.ExpToNextLevel));
        expBar.fillAmount = ratio;
    }

    /// <summary>
    /// 레벨 업 시 전체 상태 UI를 새로 고칩니다.
    /// </summary>
    private void OnLevelUp(int level)
    {
        RefreshAll();
    }

    /// <summary>
    /// 외부에서 상태 갱신을 요청할 때 사용합니다.
    /// </summary>
    public void RefreshStatus() => RefreshAll();

    /// <summary>
    /// HP, MP, 경험치 바를 모두 갱신합니다.
    /// </summary>
    private void RefreshAll()
    {
        if (playerStats == null || playerStats.Data == null) return;

        if (hpBar != null)
        {
            float maxHp = Mathf.Max(1f, playerStats.MaxHP);
            hpBar.fillAmount = playerStats.CurrentHP / maxHp;
        }

        if (mpBar != null)
        {
            float maxMp = Mathf.Max(1f, playerStats.Data.MaxMP);
            mpBar.fillAmount = playerStats.Data.CurrentMP / maxMp;
        }

        if (expBar != null)
        {
            float ratio = Mathf.Clamp01(playerStats.Data.Exp / Mathf.Max(1f, playerStats.Data.ExpToNextLevel));
            expBar.fillAmount = ratio;
        }
    }
}
