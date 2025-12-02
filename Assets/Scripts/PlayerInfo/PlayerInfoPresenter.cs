using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 플레이어 정보 UI의 열기와 닫기, 스탯 표시를 담당하는 프리젠터입니다.
/// </summary>
public class PlayerInfoPresenter : MonoBehaviour
{
    [SerializeField] private GameObject playerInfoUI;
    [SerializeField] private Button exitButton;

    [SerializeField] private GameObject equipmentUI;
    [SerializeField] private bool forceCloseOnStart = true;

    [SerializeField] private Text statsLabelText;
    [SerializeField] private Text statsValueText;

    private Button InfoButton;
    private Image image;
    private Image playerInfoImage;
    private Sprite[] sprites;
    private RectTransform playerInfoRect;   // 드래그로 실제 위치가 이동하는 RectTransform입니다.
    private RectTransform equipmentRect;    // 장비 창에서 실제 이동하는 RectTransform입니다.
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private Coroutine initRoutine;          // 초기화 흐름을 제어하는 코루틴입니다.
    private PlayerStatsManager ps;          // 플레이어 스탯 매니저를 캐싱해 둡니다.

    /// <summary>
    /// 비활성 객체를 포함하여 이름이 일치하는 게임 오브젝트를 찾습니다.
    /// </summary>
    /// <param name="name">검색할 오브젝트 이름입니다.</param>
    /// <returns>조건에 맞는 게임 오브젝트입니다. 없으면 null을 반환합니다.</returns>
    private static GameObject FindIncludingInactive(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            var go = all[i];
            if (go && go.name == name && (go.hideFlags == 0))
                return go;
        }
        return null;
    }

    /// <summary>
    /// 창 루트에서 실제로 이동할 RectTransform을 찾아 반환합니다.
    /// </summary>
    /// <param name="root">창 루트 게임 오브젝트입니다.</param>
    /// <returns>이동 가능한 RectTransform입니다.</returns>
    private static RectTransform GetMovableWindowRT(GameObject root)
    {
        if (!root) return null;

        RectTransform cand = null;

        // HeadPanel을 우선적으로 찾습니다.
        var head = root.transform.Find("HeadPanel");
        if (head == null)
        {
            // 더 깊은 단계에 있을 가능성을 고려해 전체에서 탐색합니다.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "HeadPanel") { head = t; break; }
            }
        }

        if (head && head.parent is RectTransform headParentRT)
        {
            cand = headParentRT; // HeadPanel의 부모를 창 루트로 사용합니다.
        }
        else
        {
            // HeadPanel이 없으면 루트의 RectTransform을 사용합니다.
            cand = root.GetComponent<RectTransform>();
        }

        if (!cand) return null;

        // Canvas의 직계 자식 레벨까지 거슬러 올라가 동일한 기준으로 맞춥니다.
        RectTransform cur = cand;
        while (cur && cur.parent is RectTransform prt)
        {
            if (prt.GetComponent<Canvas>() != null) break; // 부모가 Canvas라면 현재가 직계입니다.
            cur = prt;
        }
        return cur;
    }

    /// <summary>
    /// 활성화될 때 초기화가 완료될 때까지 기다린 후 이벤트를 구독합니다.
    /// </summary>
    void OnEnable()
    {
        initRoutine = StartCoroutine(InitializeWhenReady());
    }

    /// <summary>
    /// 비활성화될 때 초기화 코루틴을 중단하고 이벤트를 해제합니다.
    /// </summary>
    void OnDisable()
    {
        if (initRoutine != null) { StopCoroutine(initRoutine); initRoutine = null; }
        UnsubscribeStatEvents();
    }

    /// <summary>
    /// 필요한 매니저와 UI가 준비될 때까지 기다린 뒤 이벤트를 설정합니다.
    /// </summary>
    private IEnumerator InitializeWhenReady()
    {
        // PlayerStatsManager가 준비될 때까지 대기합니다.
        while (PlayerStatsManager.Instance == null) yield return null;
        ps = PlayerStatsManager.Instance;

        // UI 참조가 비어 있으면 한 프레임 더 대기하여 찾습니다.
        if (playerInfoUI == null)
        {
            yield return null;
            playerInfoUI = GameObject.Find("PlayerInfoUI") ?? playerInfoUI;
        }

        // 이벤트를 구독하고 첫 스탯을 갱신합니다.
        SubscribeStatEvents();
        RefreshStatsText();
    }

    /// <summary>
    /// 플레이어 스탯 관련 이벤트를 구독합니다.
    /// </summary>
    private void SubscribeStatEvents()
    {
        if (ps == null) return;

        ps.OnHPChanged -= OnHPChanged;
        ps.OnMPChanged -= OnMPChanged;
        ps.OnExpChanged -= OnExpChanged;
        ps.OnLevelUp -= OnLevelUp;

        ps.OnHPChanged += OnHPChanged;
        ps.OnMPChanged += OnMPChanged;
        ps.OnExpChanged += OnExpChanged;
        ps.OnLevelUp += OnLevelUp;
    }

    /// <summary>
    /// 플레이어 스탯 관련 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeStatEvents()
    {
        if (ps == null) return;

        ps.OnHPChanged -= OnHPChanged;
        ps.OnMPChanged -= OnMPChanged;
        ps.OnExpChanged -= OnExpChanged;
        ps.OnLevelUp -= OnLevelUp;
    }

    /// <summary>
    /// HP 변경 시 스탯 텍스트를 갱신합니다.
    /// </summary>
    private void OnHPChanged(float cur, float max) => RefreshStatsText();

    /// <summary>
    /// MP 변경 시 스탯 텍스트를 갱신합니다.
    /// </summary>
    private void OnMPChanged(float cur, float max) => RefreshStatsText();

    /// <summary>
    /// 경험치 변경 시 스탯 텍스트를 갱신합니다.
    /// </summary>
    private void OnExpChanged(int level, float exp) => RefreshStatsText();

    /// <summary>
    /// 레벨업 시 스탯 텍스트를 갱신합니다.
    /// </summary>
    private void OnLevelUp(int level) => RefreshStatsText();

    /// <summary>
    /// 필요한 UI 요소를 찾고 초기 상태를 설정합니다.
    /// </summary>
    void Start()
    {
        if (!playerInfoUI) playerInfoUI = GameObject.Find("PlayerInfoUI") ?? FindIncludingInactive("PlayerInfoUI");
        if (!equipmentUI) equipmentUI = GameObject.Find("EquipmentUI") ?? FindIncludingInactive("EquipmentUI");
        exitButton = playerInfoUI.transform.GetChild(4).GetComponent<Button>();
        sprites = new Sprite[8];
        sprites = Resources.LoadAll<Sprite>("CharacterIcons");
        statsLabelText = playerInfoUI.transform.GetChild(7).transform.GetChild(0).GetComponent<Text>();
        statsValueText = playerInfoUI.transform.GetChild(7).transform.GetChild(1).GetComponent<Text>();

        // 값 텍스트가 우측 정렬되도록 설정합니다.
        if (statsValueText)
        {
            statsValueText.alignment = TextAnchor.UpperRight;
            statsValueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsValueText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        var ps = PlayerStatsManager.Instance;
        string race = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
                        ? ps.Data.Race
                        : "humanmale";

        var quickUI = GameObject.Find("QuickUI");
        if (quickUI != null && quickUI.transform.childCount > 1)
        {
            InfoButton = quickUI.transform.GetChild(2).GetComponent<Button>();
            if (InfoButton) InfoButton.onClick.AddListener(Toggle);
        }

        image = InfoButton.GetComponent<Image>();
        playerInfoImage = playerInfoUI.transform.GetChild(8).transform.GetChild(0).GetComponent<Image>();

        for (int i = 0; i < sprites.Length; i++)
        {
            Debug.Log(sprites[i].name);
            if (sprites[i].name == race)
            {
                image.sprite = sprites[i];
                playerInfoImage.sprite = sprites[i];
            }
        }

        UIEscapeStack.GetOrCreate();

        if (!playerInfoUI)
        {
            Debug.LogError("[PlayerInfoPresenter] playerInfoUI를 찾지 못했습니다.");
            enabled = false;
            return;
        }

        // 실제로 이동하는 RectTransform을 설정합니다.
        playerInfoRect = GetMovableWindowRT(playerInfoUI);
        equipmentRect = GetMovableWindowRT(equipmentUI);

        if (exitButton) exitButton.onClick.AddListener(Close);

        if (forceCloseOnStart)
        {
            if (playerInfoUI.activeSelf) playerInfoUI.SetActive(false);
            isOpen = false;
            UIEscapeStack.Instance.Remove("playerinfo");
        }
        else
        {
            isOpen = playerInfoUI.activeSelf;
            if (isOpen) UIEscapeStack.Instance.Push("playerinfo", Close, () => isOpen);
        }
    }

    /// <summary>
    /// 입력을 감지하여 창 토글과 배치 동기화를 수행합니다.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 장비 창이 켜져 있으면 이동 가능한 RectTransform을 기준으로 스냅샷을 저장하고 복사합니다.
            var eqPresenter = FindAnyObjectByType<EquipmentPresenter>();
            bool equipWasOpen = eqPresenter && eqPresenter.IsOpen;

            if (equipWasOpen && equipmentRect)
            {
                Debug.Log($"[SNAP] Save from: {PathOf(playerInfoRect)} localPos={playerInfoRect.localPosition}");
                UIPanelSwitcher.SaveSnapshot(equipmentRect);
            }

            if (equipWasOpen && equipmentRect && playerInfoRect)
                UIPanelSwitcher.CopyLayoutRT(equipmentRect, playerInfoRect);

            Toggle();

            if (equipWasOpen && eqPresenter)
            {
                eqPresenter.CloseEquipmentPublic();

                // PlayerInfo 위치를 장비 창에도 반영한 뒤 닫습니다.
                if (playerInfoRect && equipmentRect)
                    UIPanelSwitcher.CopyLayoutRT(playerInfoRect, equipmentRect);
            }
        }
    }

    /// <summary>
    /// 변환 경로를 문자열로 반환합니다.
    /// </summary>
    /// <param name="t">경로를 구할 트랜스폼입니다.</param>
    /// <returns>루트부터의 경로 문자열입니다.</returns>
    private static string PathOf(Transform t)
    {
        if (!t) return "<null>";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(t.name);
        while (t.parent)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 열림 상태에 따라 창을 열거나 닫습니다.
    /// </summary>
    public void Toggle() { if (isOpen) Close(); else Open(); }

    /// <summary>
    /// 플레이어 정보 창을 엽니다.
    /// </summary>
    public void Open()
    {
        if (isOpen || !playerInfoUI) return;

        // 기존 스냅샷이 있으면 먼저 적용합니다.
        if (playerInfoRect && UIPanelSwitcher.HasSnapshot)
        {
            Debug.Log($"[SNAP] Load  to: {PathOf(playerInfoRect)}");
            UIPanelSwitcher.LoadSnapshot(playerInfoRect);
        }

        playerInfoUI.SetActive(true);
        isOpen = true;
        UIEscapeStack.Instance.Push("playerinfo", Close, () => isOpen);

        // 창이 열릴 때 최신 스탯을 갱신합니다.
        RefreshStatsText();

        // 활성화로 인한 레이아웃 갱신 후 다음 프레임에 스냅샷을 다시 적용합니다.
        if (playerInfoRect && UIPanelSwitcher.HasSnapshot)
            StartCoroutine(ReapplySnapshotNextFrame(playerInfoRect));
    }

    /// <summary>
    /// 다음 프레임에 스냅샷을 재적용하여 위치를 보정합니다.
    /// </summary>
    /// <param name="rt">위치를 복원할 RectTransform입니다.</param>
    private System.Collections.IEnumerator ReapplySnapshotNextFrame(RectTransform rt)
    {
        yield return null; // SetActive 호출 이후 부모 레이아웃 리빌드가 끝난 다음 프레임까지 대기합니다.
        Debug.Log($"[SNAP] Load  to: {PathOf(playerInfoRect)}");
        UIPanelSwitcher.LoadSnapshot(rt);           // 스냅샷을 다시 적용합니다.
        Canvas.ForceUpdateCanvases();
        var prt = rt.parent as RectTransform;
        if (prt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
    }


    /// <summary>
    /// 플레이어 정보 창을 닫습니다.
    /// </summary>
    public void Close()
    {
        if (!isOpen || !playerInfoUI) return;

        // 닫히기 직전에 현재 위치를 스냅샷으로 저장합니다.
        if (playerInfoRect)
            UIPanelSwitcher.SaveSnapshot(playerInfoRect);

        playerInfoUI.SetActive(false);
        isOpen = false;
        UIEscapeStack.Instance.Remove("playerinfo");
    }

    /// <summary>
    /// 종족 코드에 맞는 표시 이름을 반환합니다.
    /// </summary>
    /// <param name="race">종족 코드 문자열입니다.</param>
    /// <returns>사용자에게 표시할 종족명입니다.</returns>
    private static string GetRaceDisplayName(string race)
    {
        if (string.IsNullOrEmpty(race)) return "인간";
        switch (race.ToLowerInvariant())
        {
            case "humanmale": return "인간";
            case "dwarfmale": return "드워프";
            case "gnomemale": return "노움";
            case "nightelfmale": return "엘프";
            case "orcmale": return "오크";
            case "trollmale": return "트롤";
            case "goblinmale": return "고블린";
            case "scourgefemale": return "언데드";
            default: return race; // 알 수 없으면 원문 표시
        }
    }

    /// <summary>
    /// 스탯 정보를 UI에 표시합니다.
    /// </summary>
    public void RefreshStatsText()
    {
        var ps = PlayerStatsManager.Instance;
        var d = ps != null ? ps.Data : null;
        var displayRace = GetRaceDisplayName(d.Race);

        // 데이터가 없으면 내용을 비운 채로 반환합니다.
        if (d == null)
        {
            if (statsLabelText) statsLabelText.text = "";
            if (statsValueText) statsValueText.text = "";
            return;
        }

        // 두 개의 열이 모두 있으면 라벨과 값을 분리하여 출력합니다.
        if (statsLabelText && statsValueText)
        {
            // 라벨 열을 구성합니다.
            var labels = new System.Text.StringBuilder();
            labels.AppendLine("종족");
            labels.AppendLine("레벨");
            labels.AppendLine("경험치");
            labels.AppendLine();
            labels.AppendLine("HP");
            labels.AppendLine("MP");
            labels.AppendLine("데미지(ATK)");
            labels.AppendLine("방어력(DEF)");
            labels.AppendLine("민첩성(DEX)");
            labels.AppendLine("공격 속도(AS)");
            labels.AppendLine("치명타 확률(CC)");
            labels.AppendLine("치명타 데미지(CD)");

            // 값 열을 구성합니다. 정렬은 텍스트 설정으로 처리합니다.
            var values = new System.Text.StringBuilder();
            values.AppendLine($"{displayRace}");
            values.AppendLine($"{d.Level}");
            values.AppendLine($"{d.Exp:#,0} / {d.ExpToNextLevel:#,0}");
            values.AppendLine();
            values.AppendLine($"{d.CurrentHP:#,0.##} / {d.MaxHP:#,0.##}");
            values.AppendLine($"{d.CurrentMP:#,0.##} / {d.MaxMP:#,0.##}");
            values.AppendLine($"{d.Atk:#,0.##}");
            values.AppendLine($"{d.Def:#,0.##}");
            values.AppendLine($"{d.Dex:#,0.##}");
            values.AppendLine($"{d.AttackSpeed:#,0.##}");
            values.AppendLine($"{d.CritChance * 100f:0.##}%");
            values.AppendLine($"{d.CritDamage:0.##}x");

            statsLabelText.text = labels.ToString();
            statsValueText.text = values.ToString();
            return;
        }
    }
}
