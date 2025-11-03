using UnityEngine;
using UnityEngine.UI;

public class AngelResurrectionManager : MonoBehaviour
{
    public static AngelResurrectionManager Instance { get; private set; }

    [Header("Prefab")]
    [Tooltip("플레이어 사망 위치에 소환될 천사 프리팹(월드 오브젝트). 자식에 ResurrectButton(UIButton) 포함 권장.")]
    public GameObject angelPrefab;

    [Header("Resurrect Button Sprite Swap")]
    [Tooltip("버튼을 누르고 있는 동안(Pressed)에 보여줄 스프라이트")]
    public Sprite pressedSprite;
    [Tooltip("마우스 오버(Highlight) 시 보여줄 스프라이트 (선택)")]
    public Sprite highlightedSprite;
    [Tooltip("선택(Selected) 시 보여줄 스프라이트 (선택)")]
    public Sprite selectedSprite;
    [Tooltip("비활성(Disabled) 시 보여줄 스프라이트 (선택)")]
    public Sprite disabledSprite;

    private GameObject currentAngel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        PlayerStatsManager.OnPlayerDeathAnimFinished += SpawnAngelAtPlayer;
        PlayerStatsManager.OnPlayerRevived += CleanupAngel;
    }

    void OnDisable()
    {
        PlayerStatsManager.OnPlayerDeathAnimFinished -= SpawnAngelAtPlayer;
        PlayerStatsManager.OnPlayerRevived -= CleanupAngel;
    }

    private void SpawnAngelAtPlayer()
    {
        if (currentAngel || angelPrefab == null) return;

        var player = PlayerStatsManager.Instance;
        if (!player) return;

        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;

        currentAngel = Instantiate(angelPrefab, pos, rot);

        // 프리팹 내부에서 버튼을 찾아 이벤트 연결
        var resurrectBtn = currentAngel.GetComponentInChildren<Button>(true);
        if (resurrectBtn != null)
        {
            // 1) 버튼 Transition을 SpriteSwap으로 설정
            resurrectBtn.transition = Selectable.Transition.SpriteSwap;

            // 2) 기존 SpriteState를 가져와 필요한 항목만 덮어쓰기
            var st = resurrectBtn.spriteState;
            if (pressedSprite) st.pressedSprite = pressedSprite;
            if (highlightedSprite) st.highlightedSprite = highlightedSprite;
            if (selectedSprite) st.selectedSprite = selectedSprite;
            if (disabledSprite) st.disabledSprite = disabledSprite;
            resurrectBtn.spriteState = st;

            // 3) 클릭 동작
            resurrectBtn.onClick.RemoveAllListeners();
            resurrectBtn.onClick.AddListener(() =>
            {
                player.ReviveAt(pos, rot);
            });
        }
        else
        {
            Debug.LogWarning("[AngelResurrectionManager] ResurrectButton(Button)이 프리팹 내에 없습니다. 직접 연결하세요.");
        }
    }

    private void CleanupAngel()
    {
        if (currentAngel)
        {
            Destroy(currentAngel);
            currentAngel = null;
        }
    }
}
