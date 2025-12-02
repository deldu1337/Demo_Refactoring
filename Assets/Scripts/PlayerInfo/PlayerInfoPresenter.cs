using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerInfoPresenter : MonoBehaviour
{
    [SerializeField] private GameObject playerInfoUI;
    [SerializeField] private Button exitButton;

    [SerializeField] private GameObject equipmentUI;
    [SerializeField] private bool forceCloseOnStart = true;

    // ★ 추가: 스탯 텍스트 레퍼런스
    [SerializeField] private Text statsLabelText;    // ← 추가: 라벨 열
    [SerializeField] private Text statsValueText;    // ← 추가: 값 열(오른쪽 정렬)

    private Button InfoButton;
    private Image image;
    private Image playerInfoImage;
    private Sprite[] sprites;
    private RectTransform playerInfoRect;   // ★ 실제로 움직이는 RT
    private RectTransform equipmentRect;    // ★ 실제로 움직이는 RT
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private Coroutine initRoutine;                // ★ 추가: 초기화 코루틴
    private PlayerStatsManager ps;                // ★ 추가: 캐시

    // 비활성 포함 탐색 (이름으로)
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

    // 루트에서 실제 드래그로 이동하는 "창 패널" RT 찾기
    // 규칙: 루트 하위에서 UIDragHandler를 찾고, 그 핸들러의 parent RT를 반환. 없으면 루트의 RT.
    // ★ 공통: 실제로 움직일 "창 루트" RT를 얻는다.
    // 규칙:
    //  1) root 하위에서 이름이 "HeadPanel"인 트랜스폼을 찾고 -> 그 parent RT를 창 루트로 사용
    //  2) 없으면 root의 RectTransform 사용
    //  3) 마지막으로 Canvas의 '직계 자식' 레벨까지 타고 올라가 통일
    private static RectTransform GetMovableWindowRT(GameObject root)
    {
        if (!root) return null;

        RectTransform cand = null;

        // 1) HeadPanel 우선
        var head = root.transform.Find("HeadPanel");
        if (head == null)
        {
            // 혹시 더 깊이 있을 수 있으니 전체 탐색
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "HeadPanel") { head = t; break; }
            }
        }

        if (head && head.parent is RectTransform headParentRT)
        {
            cand = headParentRT; // HeadPanel의 부모가 '창 루트'
        }
        else
        {
            // 2) fallback: root의 RT
            cand = root.GetComponent<RectTransform>();
        }

        if (!cand) return null;

        // 3) Canvas 직계 자식 레벨까지 끌어올리기(양쪽 패널을 동일 레벨로 통일)
        RectTransform cur = cand;
        while (cur && cur.parent is RectTransform prt)
        {
            if (prt.GetComponent<Canvas>() != null) break; // prt가 Canvas → cur는 Canvas 직계
            cur = prt;
        }
        return cur;
    }

    void OnEnable()
    {
        // ★ 플레이어/매니저 준비될 때까지 대기 후 이벤트 구독
        initRoutine = StartCoroutine(InitializeWhenReady());
    }

    void OnDisable()
    {
        if (initRoutine != null) { StopCoroutine(initRoutine); initRoutine = null; }
        UnsubscribeStatEvents();
    }

    private IEnumerator InitializeWhenReady()     // ★ 추가
    {
        // PlayerStatsManager.Instance 대기
        while (PlayerStatsManager.Instance == null) yield return null;
        ps = PlayerStatsManager.Instance;

        // UI 참조가 아직 null이면 한 프레임 정도 더 대기
        if (playerInfoUI == null)
        {
            yield return null;
            playerInfoUI = GameObject.Find("PlayerInfoUI") ?? playerInfoUI;
        }

        // 이벤트 구독 & 첫 갱신
        SubscribeStatEvents();
        RefreshStatsText();
    }

    private void SubscribeStatEvents()            // ★ 추가
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

    private void UnsubscribeStatEvents()          // ★ 추가
    {
        if (ps == null) return;

        ps.OnHPChanged -= OnHPChanged;
        ps.OnMPChanged -= OnMPChanged;
        ps.OnExpChanged -= OnExpChanged;
        ps.OnLevelUp -= OnLevelUp;
    }

    // ===== 이벤트 핸들러: 전부 텍스트 리프레시로 연결 =====
    private void OnHPChanged(float cur, float max) => RefreshStatsText();
    private void OnMPChanged(float cur, float max) => RefreshStatsText();
    private void OnExpChanged(int level, float exp) => RefreshStatsText();
    private void OnLevelUp(int level) => RefreshStatsText();

    // ===== Start/Update/Toggle/Open/Close 등 기존 로직은 그대로 두고,
    //      Open()에서 한 번 더 RefreshStatsText() 호출하는 정도만 유지 =====

    void Start()
    {
        if (!playerInfoUI) playerInfoUI = GameObject.Find("PlayerInfoUI") ?? FindIncludingInactive("PlayerInfoUI");
        if (!equipmentUI) equipmentUI = GameObject.Find("EquipmentUI") ?? FindIncludingInactive("EquipmentUI");
        exitButton = playerInfoUI.transform.GetChild(4).GetComponent<Button>();
        sprites = new Sprite[8];
        sprites = Resources.LoadAll<Sprite>("CharacterIcons");
        statsLabelText = playerInfoUI.transform.GetChild(7).transform.GetChild(0).GetComponent<Text>();
        statsValueText = playerInfoUI.transform.GetChild(7).transform.GetChild(1).GetComponent<Text>();

        // 🔸 값 텍스트를 우측 정렬
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

        // 실제 움직이는 RT로 세팅
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 전환 직전: 장비창이 켜져 있으면, 장비창의 "움직이는 RT" 기준으로 스냅샷 저장 + 레이아웃 복사
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

                // (선택) 현재 PI 위치를 장비쪽에도 반영해두고 닫기
                if (playerInfoRect && equipmentRect)
                    UIPanelSwitcher.CopyLayoutRT(playerInfoRect, equipmentRect);
            }
        }
    }

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

    public void Toggle() { if (isOpen) Close(); else Open(); }

    // PlayerInfoPresenter
    public void Open()
    {
        if (isOpen || !playerInfoUI) return;

        // 1차 적용(먹을 때도 있음)
        if (playerInfoRect && UIPanelSwitcher.HasSnapshot)
        {
            Debug.Log($"[SNAP] Load  to: {PathOf(playerInfoRect)}");
            UIPanelSwitcher.LoadSnapshot(playerInfoRect);
        }

        playerInfoUI.SetActive(true);
        isOpen = true;
        UIEscapeStack.Instance.Push("playerinfo", Close, () => isOpen);

        // ★ 추가: 열릴 때 최신 스탯 갱신
        RefreshStatsText();

        // ★ 핵심: 활성화로 인한 레이아웃 리빌드가 끝난 "다음 프레임"에 다시 복원
        if (playerInfoRect && UIPanelSwitcher.HasSnapshot)
            StartCoroutine(ReapplySnapshotNextFrame(playerInfoRect));
    }

    private System.Collections.IEnumerator ReapplySnapshotNextFrame(RectTransform rt)
    {
        yield return null; // 한 프레임 대기 (SetActive → 부모 레이아웃 리빌드 끝난 뒤)
        Debug.Log($"[SNAP] Load  to: {PathOf(playerInfoRect)}");
        UIPanelSwitcher.LoadSnapshot(rt);           // 스냅샷 재적용
        Canvas.ForceUpdateCanvases();
        var prt = rt.parent as RectTransform;
        if (prt) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
    }


    public void Close()
    {
        if (!isOpen || !playerInfoUI) return;

        // 닫히기 직전 현재 위치 스냅샷 저장 (움직이는 RT)
        if (playerInfoRect)
            UIPanelSwitcher.SaveSnapshot(playerInfoRect);

        playerInfoUI.SetActive(false);
        isOpen = false;
        UIEscapeStack.Instance.Remove("playerinfo");
    }

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

    // ★ 추가: 스탯 텍스트 갱신 함수 (외부에서도 호출 가능)
    public void RefreshStatsText()
    {
        var ps = PlayerStatsManager.Instance;
        var d = ps != null ? ps.Data : null;
        var displayRace = GetRaceDisplayName(d.Race);

        // 폴백: 데이터 없으면 기존 동작
        if (d == null)
        {
            if (statsLabelText) statsLabelText.text = "";
            if (statsValueText) statsValueText.text = "";
            return;
        }

        // 🔸 두 열이 존재하면 라벨/값을 분리해서 출력 (값은 오른쪽 정렬)
        if (statsLabelText && statsValueText)
        {
            // 라벨 빌드 (왼쪽)
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

            // 값 빌드 (오른쪽 정렬은 Text 설정으로 처리)
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
