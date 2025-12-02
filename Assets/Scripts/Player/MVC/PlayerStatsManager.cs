using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class PlayerDataEntry : PlayerData
{
    public string race; // JSON에서 읽기용(대소문자 구분 없이 비교)
}

[Serializable]
public class PlayerDataCollection
{
    public PlayerDataEntry[] entries;
}

public class PlayerStatsManager : MonoBehaviour, IHealth
{
    public static PlayerStatsManager Instance { get; private set; } // 단일 인스턴스를 유지합니다.

    // 전역 브로드캐스트 이벤트입니다.
    public static event Action OnPlayerDied;
    public static event Action OnPlayerDeathAnimFinished;
    public static event Action OnPlayerRevived;

    [Header("Death/Revive Options")]
    public bool pauseEditorOnDeath = false;

    [Header("Pose Root (모델 루트)")]
    [Tooltip("플레이어 메시에 해당하는 모델 루트를 지정(비우면 이 오브젝트 자체를 사용)")]
    [SerializeField] private Transform poseRoot;

    // 죽음을 한 번만 처리하도록 보호합니다.
    private bool isDead = false;
    public bool IsDead => isDead;

    private PlayerSkeletonSnapshot lastAliveSnapshot;

    public PlayerData Data { get; private set; }
    private ILevelUpStrategy levelUpStrategy;

    public float CurrentHP => Data.CurrentHP;
    public float MaxHP => Data.MaxHP;

    public event Action<float, float> OnHPChanged;
    public event Action<float, float> OnMPChanged;
    public event Action<int, float> OnExpChanged;
    public event Action<int> OnLevelUp;

    private float eqHP, eqMP, eqAtk, eqDef, eqDex, eqAS, eqCC, eqCD;

    /// <summary>싱글톤 인스턴스를 보존하고 레벨업 전략을 준비합니다.</summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        levelUpStrategy = new DefaultLevelUpStrategy();
    }

    /// <summary>파괴 시 싱글톤 참조를 정리합니다.</summary>
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>초기 데이터가 있다면 디버그 정보를 남깁니다.</summary>
    void Start()
    {
        if (Data != null)
            Debug.Log(Data.Level);
    }

    /// <summary>
    /// PlayerSpawn이 프리팹 인스턴스 생성 직후 호출하는 초기화 진입점입니다.
    /// - 새 게임이면 선택하신 종족의 기본값을 로드해 드립니다.
    /// - 이어하기일 경우 세이브 데이터를 우선 적용하며, 선택 종족이 다르면 기본값으로 새로 시작합니다.
    /// </summary>
    public void InitializeForSelectedRace()
    {
        string race = string.IsNullOrEmpty(GameContext.SelectedRace) ? "humanmale" : GameContext.SelectedRace;

        // 이어하기 경로: 해당 종족 세이브가 있으면 불러와서 종료합니다.
        var saved = SaveLoadService.LoadPlayerDataForRaceOrNull(race);
        if (!GameContext.IsNewGame && saved != null)
        {
            LoadData(saved);
            return;
        }

        // 새 게임이지만 기존 세이브가 있다면 기본적으로 덮어쓰지 않습니다.
        if (GameContext.IsNewGame && saved != null && !GameContext.ForceReset)
        {
            // 실수로 새 게임을 누르셨을 수 있으므로 기존 저장을 우선 존중합니다.
            LoadData(saved);
            return;
        }

        // 여기까지 오면 새 게임 또는 강제 리셋 상황이므로 기본값을 적용합니다.
        LoadRaceData_FromSingleFile(race);
        Data.Race = race;
        SaveLoadService.SavePlayerDataForRace(race, Data); // 첫 저장 또는 리셋 저장입니다.

        // 새 게임 플래그를 정리합니다.
        GameContext.IsNewGame = false;
        GameContext.ForceReset = false;
    }



    /// <summary>저장된 데이터를 불러와 현재 데이터로 설정합니다.</summary>
    public void LoadData(PlayerData loaded)
    {
        if (loaded != null) Data = loaded;
        else
        {
            Data = new PlayerData();
        }

        // Base 수치가 비어 있을 때 최종 수치에서 한 번 복사하여 초기화해 드립니다.
        if (Data.BaseMaxHP <= 0f && Data.MaxHP > 0f)
        {
            Data.BaseMaxHP = Data.MaxHP;
            Data.BaseMaxMP = Data.MaxMP;
            Data.BaseAtk = Data.Atk;
            Data.BaseDef = Data.Def;
            Data.BaseDex = Data.Dex;
            Data.BaseAttackSpeed = Data.AttackSpeed;
            Data.BaseCritChance = Data.CritChance;
            Data.BaseCritDamage = Data.CritDamage;
        }

        UpdateUI();
    }


    /// <summary>
    /// 단일 JSON 파일에서 선택하신 종족의 기본값을 읽어 옵니다.
    /// Resources/PlayerData/PlayerDataAll.json 파일을 사용합니다.
    /// </summary>
    public void LoadRaceData_FromSingleFile(string raceName)
    {
        TextAsset json = Resources.Load<TextAsset>("PlayerData/PlayerDataAll");
        if (json == null) { Debug.LogError("..."); LoadData(null); return; }

        var col = JsonUtility.FromJson<PlayerDataCollection>(json.text);
        if (col?.entries == null || col.entries.Length == 0) { Debug.LogError("..."); LoadData(null); return; }

        foreach (var e in col.entries)
        {
            if (string.Equals(e.race, raceName, StringComparison.OrdinalIgnoreCase))
            {
                Data = new PlayerData
                {
                    Race = raceName,

                    // Base를 종족 기본값으로 설정합니다.
                    BaseMaxHP = e.MaxHP,
                    BaseMaxMP = e.MaxMP,
                    BaseAtk = e.Atk,
                    BaseDef = e.Def,
                    BaseDex = e.Dex,
                    BaseAttackSpeed = e.AttackSpeed,
                    BaseCritChance = e.CritChance,
                    BaseCritDamage = e.CritDamage,

                    Level = e.Level,
                    Exp = e.Exp,
                    ExpToNextLevel = e.ExpToNextLevel,

                    // Final은 장비가 없는 기준으로 Base 값을 복사합니다.
                    MaxHP = e.MaxHP,
                    MaxMP = e.MaxMP,
                    Atk = e.Atk,
                    Def = e.Def,
                    Dex = e.Dex,
                    AttackSpeed = e.AttackSpeed,
                    CritChance = e.CritChance,
                    CritDamage = e.CritDamage,

                    CurrentHP = e.CurrentHP,
                    CurrentMP = e.CurrentMP
                };

                UpdateUI();
                Debug.Log($"{raceName} 기본 스탯 로드 완료 (Base/Final 분리).");
                return;
            }
        }

        Debug.LogError($"PlayerDataAll.json에 '{raceName}' 항목이 없습니다.");
        LoadData(null);
    }


    /// <summary>장비 정보를 반영하여 최종 스탯을 다시 계산합니다.</summary>
    public void RecalculateStats(IReadOnlyList<EquipmentSlot> equippedSlots)
    {
        // 장비에서 얻는 보너스를 합산합니다.
        eqHP = eqMP = eqAtk = eqDef = eqDex = eqAS = eqCC = eqCD = 0f;

        if (equippedSlots != null)
        {
            foreach (var slot in equippedSlots)
            {
                if (slot.equipped == null || slot.equipped.data == null || slot.equipped.rolled == null) continue;
                var eq = slot.equipped.rolled;
                eqHP += eq.hp; eqMP += eq.mp; eqAtk += eq.atk; eqDef += eq.def;
                eqDex += eq.dex; eqAS += eq.As; eqCC += eq.cc; eqCD += eq.cd;
            }
        }

        // 최종 수치는 기본값과 장비 보너스를 합산합니다.
        Data.MaxHP = Data.BaseMaxHP + eqHP;
        Data.MaxMP = Data.BaseMaxMP + eqMP;
        Data.Atk = Data.BaseAtk + eqAtk;
        Data.Def = Data.BaseDef + eqDef;
        Data.Dex = Data.BaseDex + eqDex;
        Data.AttackSpeed = Data.BaseAttackSpeed + eqAS;
        Data.CritChance = Data.BaseCritChance + eqCC;
        Data.CritDamage = Data.BaseCritDamage + eqCD;

        // 현재 HP/MP가 최대치를 넘지 않도록 제한합니다.
        Data.CurrentHP = Mathf.Min(Data.CurrentHP, Data.MaxHP);
        Data.CurrentMP = Mathf.Min(Data.CurrentMP, Data.MaxMP);

        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();
    }


    /// <summary>들어온 피해를 반영하고 사망 여부를 확인합니다.</summary>
    /// <param name="damage">입으신 피해량입니다.</param>
    public void TakeDamage(float damage)
    {
        if (isDead) return; // 이미 사망하셨다면 무시합니다.

        float finalDamage = Mathf.Max(damage - Data.Def, 1f);
        Data.CurrentHP = Mathf.Max(Data.CurrentHP - finalDamage, 0);
        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();

        if (Data.CurrentHP <= 0 && !isDead)
        {
            lastAliveSnapshot = PlayerSkeletonSnapshot.Capture(poseRoot, includeRootLocalTransform: false);
            HandleDeath();
        }
    }

    /// <summary>사망 시 한 번만 처리되도록 관련 동작을 실행합니다.</summary>
    private void HandleDeath()
    {
        isDead = true;

        // 전역 알림을 보내 모든 오브젝트가 즉시 반응할 수 있도록 합니다.
        try { OnPlayerDied?.Invoke(); } catch (Exception e) { Debug.LogException(e); }

        // 이동과 공격 컴포넌트를 잠시 비활성화합니다.
        var move = GetComponent<PlayerMove>();
        if (move) move.enabled = false;
        var attacks = GetComponent<PlayerAttacks>();
        if (attacks) attacks.enabled = false;

        // 죽음 애니메이션을 재생한 뒤 추가 처리를 진행합니다.
        StartCoroutine(PlayDeathAndPauseEditor());
    }

    /// <summary>죽음 애니메이션을 재생하고 종료 시점을 알립니다.</summary>
    private IEnumerator PlayDeathAndPauseEditor()
    {
        string deathAnim = "Death (ID 1 variation 0)";
        float duration = 0.7f; // 기본 대기 시간입니다.

        var anim = GetComponent<Animation>();
        if (anim && anim.GetClip(deathAnim))
        {
            var st = anim[deathAnim];
            st.wrapMode = WrapMode.Once;
            st.speed = 1f;
            anim.Stop();
            anim.Play(deathAnim);
            duration = st.length;
        }
        else
        {
            Debug.LogWarning($"[PlayerStatsManager] Death clip '{deathAnim}'을 찾지 못했습니다. 기본 대기시간({duration}s) 후 일시정지합니다.");
        }

        // 애니메이션이 끝날 때까지 기다립니다.
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 애니메이션 종료 후 알림을 보냅니다.
        try { OnPlayerDeathAnimFinished?.Invoke(); } catch (Exception e) { Debug.LogException(e); }

        // 에디터에서만 일시정지
//#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPaused = true;
//#endif
    }

    /// <summary>지정한 위치에서 부활시키고 죽기 직전 포즈를 복원합니다.</summary>
    /// <param name="reviveWorldPos">부활하실 월드 위치입니다.</param>
    /// <param name="reviveWorldRot">부활하실 월드 회전값입니다.</param>
    public void ReviveAt(Vector3 reviveWorldPos, Quaternion reviveWorldRot)
    {
        if (!isDead) return;

        // 먼저 위치와 자세를 설정합니다.
        transform.SetPositionAndRotation(reviveWorldPos, reviveWorldRot);

        // 물리 상태를 초기화합니다.
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        // 죽기 직전 포즈 스냅샷을 로컬 단위로 복원합니다.
        if (lastAliveSnapshot != null)
        {
            // 현재 위치를 유지하면서 포즈만 스냅샷으로 복원합니다.
            lastAliveSnapshot.Apply(poseRoot, transform.position, transform.rotation);
        }

        // 애니메이션을 초기화하여 잔상을 방지합니다.
        var anim = GetComponent<Animation>();
        if (anim)
        {
            anim.Stop();
            string idle = "Stand (ID 0 variation 0)";
            if (anim.GetClip(idle))
            {
                var st = anim[idle];
                st.wrapMode = WrapMode.Loop;
                st.time = 0f;
                anim.Play(idle); // 상태를 갱신합니다.
                anim.Sample();   // 즉시 0프레임을 적용합니다.
            }
        }

        // 체력과 마나를 모두 회복합니다.
        Data.CurrentHP = Data.MaxHP;
        Data.CurrentMP = Data.MaxMP;
        Data.Exp = 0f;
        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();

        // 이동과 공격 컨트롤을 다시 활성화합니다.
        var move = GetComponent<PlayerMove>(); if (move) move.enabled = true;
        var attacks = GetComponent<PlayerAttacks>(); if (attacks) attacks.enabled = true;

        isDead = false;

        // 스냅샷은 1회성으로 사용했으니 정리합니다.
        lastAliveSnapshot = null;

        try { OnPlayerRevived?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>체력을 회복시킵니다.</summary>
    /// <param name="amount">회복하실 체력량입니다.</param>
    public void Heal(float amount)
    {
        if (isDead) return; // 사망 상태에서는 회복을 적용하지 않습니다.
        Data.CurrentHP = Mathf.Min(Data.CurrentHP + amount, Data.MaxHP);
        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();
    }

    /// <summary>마나를 소모하고 가능 여부를 알려드립니다.</summary>
    /// <param name="amount">소모하실 마나량입니다.</param>
    /// <returns>마나가 충분하면 true를 반환합니다.</returns>
    public bool UseMana(float amount)
    {
        if (Data.CurrentMP < amount) return false;
        Data.CurrentMP -= amount;
        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();
        return true;
    }

    /// <summary>마나를 회복시킵니다.</summary>
    /// <param name="amount">회복하실 마나량입니다.</param>
    public void RestoreMana(float amount)
    {
        Data.CurrentMP = Mathf.Min(Data.CurrentMP + amount, Data.MaxMP);
        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();
    }

    /// <summary>경험치를 획득하고 필요하면 레벨업을 진행합니다.</summary>
    /// <param name="amount">획득하신 경험치 양입니다.</param>
    public void GainExp(float amount)
    {
        Data.Exp += amount;
        Debug.Log($"현재 EXP: {Data.Exp}/{Data.ExpToNextLevel}");

        // 필요하면 여러 번 레벨업을 처리합니다.
        while (Data.Exp >= Data.ExpToNextLevel)
        {
            Data.Exp -= Data.ExpToNextLevel; // 여분 EXP 유지
            LevelUp();
        }

        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();
    }

    /// <summary>레벨업 처리와 스탯 갱신을 수행합니다.</summary>
    private void LevelUp()
    {
        Data.Level++;
        Data.ExpToNextLevel = Mathf.Round(Data.ExpToNextLevel * 1.2f);

        // 레벨업 보너스를 기본 수치에 적용합니다.
        Data.BaseMaxHP += 10f;
        Data.BaseMaxMP += 5f;
        Data.BaseAtk += 2f;
        Data.BaseDef += 0.5f;

        // Dex, AS, 치명타 수치는 기본값을 유지합니다.

        // 최종 수치에 장비 보너스를 다시 반영합니다.
        Data.MaxHP = Data.BaseMaxHP + eqHP;
        Data.MaxMP = Data.BaseMaxMP + eqMP;
        Data.Atk = Data.BaseAtk + eqAtk;
        Data.Def = Data.BaseDef + eqDef;
        Data.Dex = Data.BaseDex + eqDex;           // 변화 없음
        Data.AttackSpeed = Data.BaseAttackSpeed + eqAS;            // 변화 없음
        Data.CritChance = Data.BaseCritChance + eqCC;            // 변화 없음
        Data.CritDamage = Data.BaseCritDamage + eqCD;            // 변화 없음

        // 레벨업 시 체력과 마나를 모두 회복합니다.
        Data.CurrentHP = Data.MaxHP;
        Data.CurrentMP = Data.MaxMP;

        SaveLoadService.SavePlayerDataForRace(Data.Race, Data);
        UpdateUI();

        OnLevelUp?.Invoke(Data.Level);
    }


    /// <summary>공격 데미지를 계산합니다.</summary>
    public float CalculateDamage() // 기존 그대로 유지 (호환용)
    {
        bool _;
        return CalculateDamage(out _);
    }

    /// <summary>치명타 여부까지 함께 반환하는 데미지 계산입니다.</summary>
    public float CalculateDamage(out bool isCrit)
    {
        float damage = Data.Atk;
        isCrit = UnityEngine.Random.value <= Data.CritChance;
        if (isCrit)
        {
            damage *= Data.CritDamage;
            Debug.Log($"치명타! {damage} 데미지");
        }
        return damage;
    }

    /// <summary>UI에 최신 수치를 반영하도록 알립니다.</summary>
    private void UpdateUI()
    {
        OnHPChanged?.Invoke(Data.CurrentHP, Data.MaxHP);
        OnMPChanged?.Invoke(Data.CurrentMP, Data.MaxMP);
        OnExpChanged?.Invoke(Data.Level, Data.Exp);
    }
}
