//using UnityEngine;
//using System.Collections.Generic;
//using UnityEngine.UI;

//public class SkillManager : MonoBehaviour
//{
//    [SerializeField] private string playerClass = "warrior";
//    private Button SkillButton;

//    [Header("UI refs")]
//    public SkillQuickBar quickBar;   // 비워놔도 Start에서 자동으로 찾음
//    public SkillBookUI skillBook;    // 비워놔도 Start에서 자동으로 찾음

//    private PlayerStatsManager stats;
//    private readonly Dictionary<string, float> skillCooldowns = new();

//    // 레벨 해제 규칙 (원하면 더 추가)
//    private readonly List<SkillUnlockDef> unlockDefs = new()
//    {
//        new SkillUnlockDef("slash", 2),
//        new SkillUnlockDef("smash", 3),
//        new SkillUnlockDef("charge", 4),
//    };

//    // 아이콘 로더 (Resources/SkillIcons/{id}.png)
//    private Sprite GetIcon(string skillId) => Resources.Load<Sprite>($"SkillIcons/{skillId}");

//    private void Awake()
//    {
//        // PlayerStatsManager 싱글톤이 이미 있으면 잡고, 없으면 Start에서 다시 시도
//        stats = PlayerStatsManager.Instance;
//    }

//    private void Start()
//    {
//        // 레퍼런스 자동 찾기
//        if (!quickBar) quickBar = FindFirstObjectByType<SkillQuickBar>(FindObjectsInactive.Include);
//        if (!skillBook) skillBook = FindFirstObjectByType<SkillBookUI>(FindObjectsInactive.Include);
//        SkillButton = GameObject.Find("QuickUI").transform.GetChild(0).GetComponent<Button>();
//        SkillButton.onClick.AddListener(skillBook.Toggle);

//        // 스킬 데이터 로드
//        var jsonFile = Resources.Load<TextAsset>("Datas/skillData");
//        if (!jsonFile)
//        {
//            Debug.LogError("[SkillManager] Resources/Datas/skillData.json 없음");
//            return;
//        }
//        SkillFactory.LoadSkillsFromJson(jsonFile.text);

//        // 스킬북 구성 및 초기 상태
//        if (skillBook)
//        {
//            skillBook.Build(unlockDefs, GetIcon);
//            skillBook.Show(false); // 시작은 닫힘
//            var curLv = stats ? stats.Data.Level : 1;
//            skillBook.RefreshLocks(curLv);
//        }

//        // 퀵바 자동 배선 + 저장된 슬롯 불러오기
//        if (quickBar)
//        {
//            quickBar.AutoWireSlots();

//            // 저장 불러오기 (없으면 null)
//            var save = QuickBarPersistence.Load();
//            if (save != null)
//            {
//                // 현재 레벨 기준으로 사용 가능한 스킬만 적용
//                quickBar.ApplySaveData(
//                    save,
//                    GetIcon,
//                    (skillId) =>
//                    {
//                        int lv = stats ? stats.Data.Level : 1;
//                        var def = unlockDefs.Find(d => d.skillId == skillId);
//                        return def != null && lv >= def.unlockLevel;
//                    }
//                );
//            }

//            // 슬롯 구성 변경될 때마다 자동 저장 (드래그&드롭/스왑 등)
//            quickBar.OnChanged += () =>
//            {
//                QuickBarPersistence.Save(quickBar.ToSaveData());
//            };
//        }

//        // 레벨업 이벤트 구독 (레벨업 때만 자동 배치)
//        if (stats != null)
//        {
//            stats.OnLevelUp -= OnLevelUp;
//            stats.OnLevelUp += OnLevelUp;
//        }
//    }

//    private void OnDestroy()
//    {
//        if (stats != null) stats.OnLevelUp -= OnLevelUp;

//        // 마지막으로 한번 더 저장(선택)
//        if (quickBar != null)
//            QuickBarPersistence.Save(quickBar.ToSaveData());
//    }

//    private void Update()
//    {
//        // 스킬북 토글 (ESC 처리는 중앙 ESCView + UIEscapeStack이 담당)
//        if (Input.GetKeyDown(KeyCode.K) && skillBook != null)
//            skillBook.Toggle();

//        // 단축키 입력 → 슬롯 인덱스 실행
//        if (Input.GetKeyDown(KeyCode.A)) UseSlot(0);
//        if (Input.GetKeyDown(KeyCode.S)) UseSlot(1);
//        if (Input.GetKeyDown(KeyCode.D)) UseSlot(2);
//        if (Input.GetKeyDown(KeyCode.F)) UseSlot(3);
//        if (Input.GetKeyDown(KeyCode.G)) UseSlot(4);
//        if (Input.GetKeyDown(KeyCode.Z)) UseSlot(5);
//        if (Input.GetKeyDown(KeyCode.X)) UseSlot(6);
//        if (Input.GetKeyDown(KeyCode.C)) UseSlot(7);
//        if (Input.GetKeyDown(KeyCode.V)) UseSlot(8);
//    }

//    // ============ 레벨업 시에만 자동 배치 ============  
//    private void OnLevelUp(int level)
//    {
//        // 스킬북 잠금 갱신
//        skillBook?.RefreshLocks(level);

//        // 빈 슬롯에 자동 배치 (이미 있는 스킬은 건너뜀)
//        ApplyUnlocks(level);

//        // 저장
//        if (quickBar != null)
//            QuickBarPersistence.Save(quickBar.ToSaveData());
//    }

//    private void ApplyUnlocks(int level)
//    {
//        if (!quickBar) return;

//        foreach (var def in unlockDefs)
//        {
//            if (level < def.unlockLevel) continue;

//            // 스킬 존재 확인
//            var skill = SkillFactory.GetSkill(playerClass, def.skillId);
//            if (skill == null) continue;

//            // 이미 배치되어 있으면 패스 (스킬북은 해제 표시만)
//            if (IsSkillAlreadyAssigned(def.skillId))
//            {
//                skillBook?.MarkUnlocked(def.skillId);
//                continue;
//            }

//            // 빈 슬롯 있으면 자동 배치
//            Sprite icon = GetIcon(def.skillId);
//            if (quickBar.AssignToFirstEmpty(def.skillId, icon))
//            {
//                skillBook?.MarkUnlocked(def.skillId);
//                Debug.Log($"[{def.skillId}] 자동 할당 완료 (레벨 {level})");
//            }
//            else
//            {
//                // 빈 슬롯 없어도 해제 마킹은 해둠
//                skillBook?.MarkUnlocked(def.skillId);
//            }
//        }
//    }

//    private bool IsSkillAlreadyAssigned(string skillId)
//    {
//        if (quickBar == null || quickBar.slots == null) return false;
//        foreach (var s in quickBar.slots)
//        {
//            if (s != null && s.SkillId == skillId)
//                return true;
//        }
//        return false;
//    }

//    // ============ 스킬 실행 & 쿨다운 ============  
//    private void UseSlot(int index)
//    {
//        if (quickBar == null) return;
//        string skillId = quickBar.GetSkillAt(index);
//        if (string.IsNullOrEmpty(skillId)) return;

//        TryUseSkill(skillId, index);
//    }

//    private void TryUseSkill(string skillId, int slotIndex)
//    {
//        var skill = SkillFactory.GetSkill(playerClass, skillId);
//        if (skill == null) return;

//        if (skillCooldowns.TryGetValue(skill.Id, out float next) && Time.time < next)
//        {
//            Debug.Log($"{skill.Name} 스킬 쿨타임 중...");
//            return;
//        }

//        if (skill.Execute(gameObject, stats))
//        {
//            // 쿨타임 시작
//            skillCooldowns[skill.Id] = Time.time + skill.Cooldown;

//            var slot = quickBar.GetSlot(slotIndex);
//            if (slot != null && slot.cooldownUI != null)
//                slot.cooldownUI.StartCooldown(skill.Cooldown);
//            else
//                Debug.LogWarning($"[SkillManager] 슬롯 {slotIndex}에 cooldownUI가 없습니다.");

//            // 사용 후 저장(선택)
//            QuickBarPersistence.Save(quickBar.ToSaveData());
//        }
//    }
//}
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
