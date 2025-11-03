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
    [SerializeField] private GameObject Circle; // 초상화가 들어있는 오브젝트
    [SerializeField] private string portraitsFolder = "Portraits"; // Resources/Portraits/ 안에서 찾음
    [SerializeField] private string defaultPortraitName = "default"; // 대체 이미지 파일명

    private Image face;

    [Header("StatusUI Hierarchy Auto-Wire")]
    [SerializeField] private string statusUIRootName = "StatusUI";
    [SerializeField] private int hpIndex = 3;   // StatusUI 하위 자식 인덱스
    [SerializeField] private int mpIndex = 4;
    [SerializeField] private int expIndex = 5;

    private PlayerStatsManager playerStats;
    private Coroutine initRoutine;

    private void Start()
    {
        if (Circle != null)
            face = Circle.GetComponent<Image>();
    }

    private void OnEnable()
    {
        initRoutine = StartCoroutine(InitializeWhenReady());
    }

    private void OnDisable()
    {
        if (initRoutine != null) { StopCoroutine(initRoutine); initRoutine = null; }
        UnsubscribeEvents();
    }

    private IEnumerator InitializeWhenReady()
    {
        // 1) PlayerStats 준비 대기
        while (PlayerStatsManager.Instance == null)
            yield return null;

        playerStats = PlayerStatsManager.Instance;

        // 2) StatusUI 루트 대기
        Transform statusUI = null;
        while (statusUI == null)
        {
            var go = GameObject.Find(statusUIRootName);
            if (go != null) statusUI = go.transform;
            else yield return null;
        }

        // 3) 바 자동 탐색(비어있으면)
        if (hpBar == null && statusUI.childCount > hpIndex)
            hpBar = statusUI.GetChild(hpIndex).GetComponentInChildren<Image>();
        if (mpBar == null && statusUI.childCount > mpIndex)
            mpBar = statusUI.GetChild(mpIndex).GetComponentInChildren<Image>();
        if (expBar == null && statusUI.childCount > expIndex)
            expBar = statusUI.GetChild(expIndex).GetComponentInChildren<Image>();

        // 4) 이벤트 구독
        SubscribeEvents();

        // 5) 초상화/수치 즉시 1회 갱신
        RefreshPortrait();
        RefreshAll();
    }

    private void SubscribeEvents()
    {
        if (playerStats == null) return;

        playerStats.OnHPChanged += OnHPChanged;
        playerStats.OnMPChanged += OnMPChanged;
        playerStats.OnExpChanged += OnExpChanged;
        playerStats.OnLevelUp += OnLevelUp;

        // (선택) 부활 등 특정 시점에 종족이 바뀔 수 있다면 여기도 갱신 가능
        PlayerStatsManager.OnPlayerRevived -= OnPlayerRevivedRefreshPortrait;
        PlayerStatsManager.OnPlayerRevived += OnPlayerRevivedRefreshPortrait;
    }

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

    // ===== Portrait =====
    private void RefreshPortrait()
    {
        if (face == null) return;

        // 플레이어 종족명 얻기 (없으면 humanmale로 가정)
        string race = "humanmale";
        if (playerStats != null && playerStats.Data != null && !string.IsNullOrEmpty(playerStats.Data.Race))
            race = playerStats.Data.Race;

        // 파일명 매핑(파일명이 종족명과 다르면 여기서 변환)
        string spriteName = MapRaceToPortraitName(race);

        // Resources/Portraits/<spriteName>.png 로드
        Sprite sp = Resources.Load<Sprite>($"{portraitsFolder}/{spriteName}");
        if (sp == null)
        {
            // 대체 이미지 시도
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

    // 종족명 → 초상화 파일명 매핑(파일명이 같다면 그대로 반환)
    private string MapRaceToPortraitName(string race)
    {
        if (string.IsNullOrEmpty(race)) return "humanmale";

        switch (race.ToLowerInvariant())
        {
            // 파일명이 동일하면 그대로 반환
            case "humanmale": return "humanmale";
            case "dwarfmale": return "dwarfmale";
            case "gnomemale": return "gnomemale";
            case "nightelfmale": return "nightelfmale";
            case "orcmale": return "orcmale";
            case "trollmale": return "trollmale";
            case "goblinmale": return "goblinmale";
            case "scourgefemale": return "scourgefemale";

            // 파일명이 다르면 여기서 원하는 이름으로 변환
            // case "mycustomrace":  return "portrait_mycustom";

            default: return race.ToLowerInvariant(); // 시도 후 실패 시 default로 대체
        }
    }

    private void OnPlayerRevivedRefreshPortrait()
    {
        RefreshPortrait();
    }

    // ===== Bars =====
    private void OnHPChanged(float cur, float max)
    {
        if (hpBar == null) return;
        hpBar.fillAmount = (max > 0f) ? cur / max : 0f;
    }

    private void OnMPChanged(float cur, float max)
    {
        if (mpBar == null) return;
        mpBar.fillAmount = (max > 0f) ? cur / max : 0f;
    }

    private void OnExpChanged(int level, float exp)
    {
        if (expBar == null || playerStats == null || playerStats.Data == null) return;
        float ratio = Mathf.Clamp01(playerStats.Data.Exp / Mathf.Max(1f, playerStats.Data.ExpToNextLevel));
        expBar.fillAmount = ratio;
    }

    private void OnLevelUp(int level)
    {
        // 레벨업 시 초상화가 바뀌진 않지만, 혹시 종족 전환 시스템이 있다면 아래 호출 유지 가능
        // RefreshPortrait();
        RefreshAll();
    }

    public void RefreshStatus() => RefreshAll();

    private void RefreshAll()
    {
        if (playerStats == null || playerStats.Data == null) return;

        // HP
        if (hpBar != null)
        {
            float maxHp = Mathf.Max(1f, playerStats.MaxHP);
            hpBar.fillAmount = playerStats.CurrentHP / maxHp;
        }

        // MP
        if (mpBar != null)
        {
            float maxMp = Mathf.Max(1f, playerStats.Data.MaxMP);
            mpBar.fillAmount = playerStats.Data.CurrentMP / maxMp;
        }

        // EXP
        if (expBar != null)
        {
            float ratio = Mathf.Clamp01(playerStats.Data.Exp / Mathf.Max(1f, playerStats.Data.ExpToNextLevel));
            expBar.fillAmount = ratio;
        }
    }
}
