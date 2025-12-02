using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 사망 시 부활 천사를 소환하고 정리하는 관리자를 담당합니다.
/// </summary>
public class AngelResurrectionManager : MonoBehaviour
{
    public static AngelResurrectionManager Instance { get; private set; }

    [Header("Prefab")]
    [Tooltip("부활 버튼이 포함된 천사 프리팹을 지정해 주세요.")]
    public GameObject angelPrefab;

    [Header("Resurrect Button Sprite Swap")]
    [Tooltip("버튼이 눌렸을 때 사용할 스프라이트입니다.")]
    public Sprite pressedSprite;
    [Tooltip("커서가 올라갔을 때 사용할 스프라이트입니다.")]
    public Sprite highlightedSprite;
    [Tooltip("선택 상태일 때 사용할 스프라이트입니다.")]
    public Sprite selectedSprite;
    [Tooltip("비활성 상태일 때 사용할 스프라이트입니다.")]
    public Sprite disabledSprite;

    private GameObject currentAngel;

    /// <summary>
    /// 싱글톤 인스턴스를 설정합니다.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 플레이어 사망 및 부활 이벤트를 구독합니다.
    /// </summary>
    void OnEnable()
    {
        PlayerStatsManager.OnPlayerDeathAnimFinished += SpawnAngelAtPlayer;
        PlayerStatsManager.OnPlayerRevived += CleanupAngel;
    }

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    void OnDisable()
    {
        PlayerStatsManager.OnPlayerDeathAnimFinished -= SpawnAngelAtPlayer;
        PlayerStatsManager.OnPlayerRevived -= CleanupAngel;
    }

    /// <summary>
    /// 플레이어 위치에 천사를 소환하고 부활 버튼을 설정합니다.
    /// </summary>
    private void SpawnAngelAtPlayer()
    {
        if (currentAngel || angelPrefab == null) return;

        var player = PlayerStatsManager.Instance;
        if (!player) return;

        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;

        currentAngel = Instantiate(angelPrefab, pos, rot);

        // 부활 버튼을 찾아서 필요한 설정을 적용해 드립니다.
        var resurrectBtn = currentAngel.GetComponentInChildren<Button>(true);
        if (resurrectBtn != null)
        {
            // 버튼 전환 방식을 스프라이트 교체로 설정합니다.
            resurrectBtn.transition = Selectable.Transition.SpriteSwap;

            // 스프라이트 상태를 지정해 드립니다.
            var st = resurrectBtn.spriteState;
            if (pressedSprite) st.pressedSprite = pressedSprite;
            if (highlightedSprite) st.highlightedSprite = highlightedSprite;
            if (selectedSprite) st.selectedSprite = selectedSprite;
            if (disabledSprite) st.disabledSprite = disabledSprite;
            resurrectBtn.spriteState = st;

            // 클릭 시 플레이어를 부활시키도록 리스너를 연결합니다.
            resurrectBtn.onClick.RemoveAllListeners();
            resurrectBtn.onClick.AddListener(() =>
            {
                player.ReviveAt(pos, rot);
            });
        }
        else
        {
            Debug.LogWarning("[AngelResurrectionManager] 부활 버튼(Button)을 찾지 못했습니다. 버튼을 배치해 주세요.");
        }
    }

    /// <summary>
    /// 소환된 천사를 제거하여 씬을 정리합니다.
    /// </summary>
    private void CleanupAngel()
    {
        if (currentAngel)
        {
            Destroy(currentAngel);
            currentAngel = null;
        }
    }
}
