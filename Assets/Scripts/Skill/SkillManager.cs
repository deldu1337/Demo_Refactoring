using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 스킬 데이터 로딩과 퀵바, 스킬북 UI를 관리합니다.
/// </summary>
public class SkillManager : MonoBehaviour
{
    [SerializeField] private string playerClass = "warrior";
    private Button SkillButton;

    [Header("UI refs")]
    public SkillQuickBar quickBar;
    public SkillBookUI skillBook;

    private PlayerStatsManager stats;
    private readonly Dictionary<string, float> skillCooldowns = new();

    private readonly List<SkillUnlockDef> unlockDefs = new()
    {
        new SkillUnlockDef("slash", 2),
        new SkillUnlockDef("smash", 3),
        new SkillUnlockDef("charge", 4),
    };

    private Sprite GetIcon(string skillId) => Resources.Load<Sprite>($"SkillIcons/{skillId}");

    /// <summary>
    /// 플레이어 능력치 매니저를 가져옵니다.
    /// </summary>
    void Awake()
    {
        stats = PlayerStatsManager.Instance;
    }

    /// <summary>
    /// 현재 종족 이름을 반환합니다.
    /// </summary>
    private string CurrentRace =>
        string.IsNullOrEmpty(stats?.Data?.Race) ? "humanmale" : stats.Data.Race;

    /// <summary>
    /// 스킬 데이터를 로드하고 UI를 초기화합니다.
    /// </summary>
    void Start()
    {
        if (!quickBar) quickBar = FindFirstObjectByType<SkillQuickBar>(FindObjectsInactive.Include);
        if (!skillBook) skillBook = FindFirstObjectByType<SkillBookUI>(FindObjectsInactive.Include);

        SkillButton = GameObject.Find("QuickUI").transform.GetChild(0).GetComponent<Button>();
        SkillButton.onClick.AddListener(skillBook.Toggle);

        var jsonFile = Resources.Load<TextAsset>("Datas/skillData");
        if (!jsonFile)
        {
            Debug.LogError("[SkillManager] Resources/Datas/skillData.json 을 찾을 수 없습니다.");
            return;
        }
        SkillFactory.LoadSkillsFromJson(jsonFile.text);

        if (skillBook)
        {
            skillBook.Build(unlockDefs, GetIcon);
            skillBook.Show(false);
            var curLv = stats ? stats.Data.Level : 1;
            skillBook.RefreshLocks(curLv);
        }

        if (quickBar)
        {
            quickBar.AutoWireSlots();

            var race = CurrentRace;
            var save = QuickBarPersistence.LoadForRaceOrNull(race);
            if (save != null)
            {
                quickBar.ApplySaveData(
                    save,
                    GetIcon,
                    (skillId) =>
                    {
                        int lv = stats ? stats.Data.Level : 1;
                        var def = unlockDefs.Find(d => d.skillId == skillId);
                        return def != null && lv >= def.unlockLevel;
                    }
                );
            }

            quickBar.OnChanged += () =>
            {
                QuickBarPersistence.SaveForRace(race, quickBar.ToSaveData());
            };
        }

        if (stats != null)
        {
            stats.OnLevelUp -= OnLevelUp;
            stats.OnLevelUp += OnLevelUp;
        }
    }

    /// <summary>
    /// 객체 파괴 시 이벤트를 해제하고 저장합니다.
    /// </summary>
    void OnDestroy()
    {
        if (stats != null) stats.OnLevelUp -= OnLevelUp;

        if (quickBar != null)
            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData());
    }

    /// <summary>
    /// 입력을 받아 스킬북 토글과 슬롯 사용을 처리합니다.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && skillBook != null)
            skillBook.Toggle();

        if (Input.GetKeyDown(KeyCode.A)) UseSlot(0);
        if (Input.GetKeyDown(KeyCode.S)) UseSlot(1);
        if (Input.GetKeyDown(KeyCode.D)) UseSlot(2);
        if (Input.GetKeyDown(KeyCode.F)) UseSlot(3);
        if (Input.GetKeyDown(KeyCode.G)) UseSlot(4);
        if (Input.GetKeyDown(KeyCode.Z)) UseSlot(5);
        if (Input.GetKeyDown(KeyCode.X)) UseSlot(6);
        if (Input.GetKeyDown(KeyCode.C)) UseSlot(7);
        if (Input.GetKeyDown(KeyCode.V)) UseSlot(8);
    }

    /// <summary>
    /// 레벨 업 시 잠금 해제와 저장을 처리합니다.
    /// </summary>
    private void OnLevelUp(int level)
    {
        skillBook?.RefreshLocks(level);
        ApplyUnlocks(level);

        if (quickBar != null)
            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData());
    }

    /// <summary>
    /// 해당 레벨에서 사용할 수 있는 스킬을 퀵바에 배치합니다.
    /// </summary>
    private void ApplyUnlocks(int level)
    {
        if (!quickBar) return;

        foreach (var def in unlockDefs)
        {
            if (level < def.unlockLevel) continue;

            var skill = SkillFactory.GetSkill(playerClass, def.skillId);
            if (skill == null) continue;

            if (IsSkillAlreadyAssigned(def.skillId))
            {
                skillBook?.MarkUnlocked(def.skillId);
                continue;
            }

            Sprite icon = GetIcon(def.skillId);
            if (quickBar.AssignToFirstEmpty(def.skillId, icon))
            {
                skillBook?.MarkUnlocked(def.skillId);
                Debug.Log($"[{def.skillId}] 슬롯에 배치했습니다. (레벨 {level})");
            }
            else
            {
                skillBook?.MarkUnlocked(def.skillId);
            }
        }
    }

    /// <summary>
    /// 이미 퀵바에 등록된 스킬인지 확인합니다.
    /// </summary>
    private bool IsSkillAlreadyAssigned(string skillId)
    {
        if (quickBar == null || quickBar.slots == null) return false;
        foreach (var s in quickBar.slots)
            if (s != null && s.SkillId == skillId) return true;
        return false;
    }

    /// <summary>
    /// 인덱스에 해당하는 슬롯의 스킬을 사용합니다.
    /// </summary>
    private void UseSlot(int index)
    {
        if (quickBar == null) return;
        string skillId = quickBar.GetSkillAt(index);
        if (string.IsNullOrEmpty(skillId)) return;

        TryUseSkill(skillId, index);
    }

    /// <summary>
    /// 스킬 실행과 쿨다운 처리를 시도합니다.
    /// </summary>
    private void TryUseSkill(string skillId, int slotIndex)
    {
        var skill = SkillFactory.GetSkill(playerClass, skillId);
        if (skill == null) return;

        if (skillCooldowns.TryGetValue(skill.Id, out float next) && Time.time < next)
        {
            Debug.Log($"{skill.Name} 쿨다운 중입니다...");
            return;
        }

        if (skill.Execute(gameObject, stats))
        {
            skillCooldowns[skill.Id] = Time.time + skill.Cooldown;

            var slot = quickBar.GetSlot(slotIndex);
            if (slot != null && slot.cooldownUI != null)
                slot.cooldownUI.StartCooldown(skill.Cooldown);
            else
                Debug.LogWarning($"[SkillManager] 슬롯 {slotIndex}에 cooldownUI가 없습니다.");

            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData());
        }
    }
}
