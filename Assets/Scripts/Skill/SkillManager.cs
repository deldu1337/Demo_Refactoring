using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

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

    void Awake()
    {
        stats = PlayerStatsManager.Instance;
    }

    // ★ 종족명 헬퍼 (없으면 humanmale)
    private string CurrentRace =>
        string.IsNullOrEmpty(stats?.Data?.Race) ? "humanmale" : stats.Data.Race;

    void Start()
    {
        if (!quickBar) quickBar = FindFirstObjectByType<SkillQuickBar>(FindObjectsInactive.Include);
        if (!skillBook) skillBook = FindFirstObjectByType<SkillBookUI>(FindObjectsInactive.Include);

        SkillButton = GameObject.Find("QuickUI").transform.GetChild(0).GetComponent<Button>();
        SkillButton.onClick.AddListener(skillBook.Toggle);

        var jsonFile = Resources.Load<TextAsset>("Datas/skillData");
        if (!jsonFile)
        {
            Debug.LogError("[SkillManager] Resources/Datas/skillData.json 없음");
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

            // ★ 종족별로 로드 (레거시가 있으면 QuickBarPersistence가 마이그레이션 처리)
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

            // ★ 변경 시 종족별 저장
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

    void OnDestroy()
    {
        if (stats != null) stats.OnLevelUp -= OnLevelUp;

        if (quickBar != null)
            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData()); // ★
    }

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

    // ===== 레벨업 시 =====
    private void OnLevelUp(int level)
    {
        skillBook?.RefreshLocks(level);
        ApplyUnlocks(level);

        if (quickBar != null)
            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData()); // ★
    }

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
                Debug.Log($"[{def.skillId}] 자동 할당 완료 (레벨 {level})");
            }
            else
            {
                skillBook?.MarkUnlocked(def.skillId);
            }
        }
    }

    private bool IsSkillAlreadyAssigned(string skillId)
    {
        if (quickBar == null || quickBar.slots == null) return false;
        foreach (var s in quickBar.slots)
            if (s != null && s.SkillId == skillId) return true;
        return false;
    }

    // ===== 실행 & 쿨다운 =====
    private void UseSlot(int index)
    {
        if (quickBar == null) return;
        string skillId = quickBar.GetSkillAt(index);
        if (string.IsNullOrEmpty(skillId)) return;

        TryUseSkill(skillId, index);
    }

    private void TryUseSkill(string skillId, int slotIndex)
    {
        var skill = SkillFactory.GetSkill(playerClass, skillId);
        if (skill == null) return;

        if (skillCooldowns.TryGetValue(skill.Id, out float next) && Time.time < next)
        {
            Debug.Log($"{skill.Name} 스킬 쿨타임 중...");
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

            // ★ 사용 후에도 현재 종족으로 저장
            QuickBarPersistence.SaveForRace(CurrentRace, quickBar.ToSaveData());
        }
    }
}
