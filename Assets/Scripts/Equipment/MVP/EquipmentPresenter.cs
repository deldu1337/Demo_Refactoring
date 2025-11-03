//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// 장비 UI와 캐릭터 모델을 연결하고 장착/해제 및 스탯 계산을 담당하는 Presenter
///// </summary>
//public class EquipmentPresenter : MonoBehaviour
//{
//    private EquipmentModel model;                // 장비 데이터 모델
//    private EquipmentView view;                  // 장비 UI
//    private InventoryPresenter inventoryPresenter; // 인벤토리 Presenter
//    private PlayerInfoPresenter playerInfoPresenter;
//    private bool isOpen = false;                 // 장비창 열림 상태
//    private RectTransform equipmentUI;
//    private RectTransform playerInfoUI;

//    [SerializeField] private Camera uiCamera;           // 장비 UI 전용 카메라
//    [SerializeField] private Transform targetCharacter; // 캐릭터 모델

//    private Button EquipButton;

//    // 현재 플레이어 종족(카메라 간격 스위치에 사용)
//    private string currentRace = "humanmale";

//    public bool IsOpen => isOpen;

//    void Start()
//    {
//        UIEscapeStack.GetOrCreate();

//        // 종족 구해서 모델 생성 + currentRace 저장
//        var ps = PlayerStatsManager.Instance;
//        currentRace = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
//                        ? ps.Data.Race
//                        : "humanmale";

//        model = new EquipmentModel(currentRace);

//        view = FindAnyObjectByType<EquipmentView>();
//        inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();

//        if (!playerInfoPresenter)
//            playerInfoPresenter = FindAnyObjectByType<PlayerInfoPresenter>(FindObjectsInactive.Include);

//        equipmentUI = GameObject.Find("EquipmentUI").GetComponent<RectTransform>();
//        playerInfoUI = GameObject.Find("PlayerInfoUI").GetComponent<RectTransform>();

//        if (view != null)
//            view.Initialize(CloseEquipment, HandleEquipFromInventory);

//        EquipButton = GameObject.Find("QuickUI").transform.GetChild(1).GetComponent<Button>();
//        EquipButton.onClick.AddListener(ToggleEquipment);

//        SetupUICamera();
//        InitializeEquippedItems();
//        RefreshEquipmentUI();
//    }

//    public IReadOnlyList<EquipmentSlot> GetEquipmentSlots()
//    {
//        return model?.Slots; // EquipmentModel에서 슬롯 리스트 가져오기
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.E))
//        {
//            var pi = FindAnyObjectByType<PlayerInfoPresenter>(FindObjectsInactive.Include);
//            bool playerWasOpen = pi && pi.IsOpen;

//            RectTransform equipRT = null, playerRT = null;

//            if (!equipRT && view != null) equipRT = view.RootRect;
//            var piGO = GameObject.Find("PlayerInfoUI")
//                       ?? Resources.FindObjectsOfTypeAll<GameObject>()?.FirstOrDefault(go => go.name == "PlayerInfoUI" && go.hideFlags == 0);
//            if (piGO) playerRT = piGO.GetComponent<RectTransform>();

//            // ★ 전환 직전: 플레이어인포가 켜져 있으면 현재 위치 스냅샷 저장
//            if (playerWasOpen && playerRT)
//                UIPanelSwitcher.SaveSnapshot(playerRT);

//            // 기존 로직 유지
//            if (playerWasOpen && equipRT && playerRT)
//                UIPanelSwitcher.CopyLayoutRT(playerRT, equipRT);

//            ToggleEquipment();

//            if (playerWasOpen && pi)
//            {
//                pi.Close();
//                UIPanelSwitcher.CopyLayoutRT(equipRT, playerRT);
//            }
//        }
//    }


//    void LateUpdate()
//    {
//        // UI 카메라가 캐릭터를 바라보도록 유지
//        if (uiCamera != null && targetCharacter != null)
//        {
//            // 종족별 전/후 거리만 스위치로 조절
//            float dist = GetRaceCameraDistance(currentRace);
//            float height = GetRaceLookAtHeight(currentRace);
//            float lookY = GetRaceCameraHeight(currentRace);

//            Vector3 offset = targetCharacter.forward * dist + Vector3.up * height; // 높이는 그대로 유지
//            //Vector3 offset = targetCharacter.forward * 2.2f + Vector3.up * 1.5f;
//            uiCamera.transform.position = targetCharacter.position + offset;
//            uiCamera.transform.LookAt(targetCharacter.position + Vector3.up * lookY);
//        }
//    }

//    /// <summary>종족별 카메라 전/후 거리</summary>
//    private float GetRaceCameraDistance(string race)
//    {
//        // 비교 편의를 위해 소문자 통일
//        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();

//        switch (r)
//        {
//            case "humanmale": return 2.2f;
//            case "dwarfmale": return 2.0f;
//            case "gnomemale": return 1.2f;
//            case "nightelfmale": return 2.7f;
//            case "orcmale": return 2.5f;
//            case "trollmale": return 2.8f;
//            case "goblinmale": return 1.7f;
//            case "scourgefemale": return 2.0f;
//            default: return 2.2f;
//        }
//    }

//    /// <summary>종족별 시선(LookAt) 높이 — 필요 시 미세조정</summary>
//    private float GetRaceLookAtHeight(string race)
//    {
//        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();
//        switch (r)
//        {
//            case "humanmale": return 1.4f;
//            case "dwarfmale": return 1.3f;
//            case "gnomemale": return 0.7f;
//            case "nightelfmale": return 1.6f;
//            case "orcmale": return 1.4f;
//            case "trollmale": return 1.5f;
//            case "goblinmale": return 1.0f;
//            case "scourgefemale": return 1.2f;
//            default: return 1.4f;
//        }
//    }

//    /// <summary>종족별 카메라 높이</summary>
//    private float GetRaceCameraHeight(string race)
//    {
//        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();
//        switch (r)
//        {
//            case "humanmale": return 1.0f;
//            case "dwarfmale": return 0.7f;
//            case "gnomemale": return 0.5f;
//            case "nightelfmale": return 1.3f;
//            case "orcmale": return 1.1f;
//            case "trollmale": return 1.3f;
//            case "goblinmale": return 0.6f;
//            case "scourgefemale": return 0.9f;
//            default: return 1.0f;
//        }
//    }

//    /// <summary>UI 카메라 자동 세팅</summary>
//    private void SetupUICamera()
//    {
//        if (uiCamera == null)
//        {
//            int uiLayer = LayerMask.NameToLayer("UICharacter");
//            if (uiLayer != -1)
//            {
//                Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
//                foreach (Camera cam in cameras)
//                {
//                    if (cam.gameObject.layer == uiLayer)
//                    {
//                        uiCamera = cam;
//                        break;
//                    }
//                }
//            }
//        }
//    }

//    /// <summary>외부에서 켜기</summary>
//    public void OpenEquipment()                     // ← 추가
//    {
//        if (!isOpen) ToggleEquipment();
//    }

//    /// <summary>외부에서 끄기</summary>
//    public void CloseEquipmentPublic()              // ← 추가 (이름 충돌 방지용)
//    {
//        if (isOpen) CloseEquipment();               // 기존 CloseEquipment 호출
//    }

//    /// <summary>장비창 열기/닫기</summary>
//    public void ToggleEquipment()
//    {
//        if (view == null) return;

//        // ★ 열리는 순간이라면, 먼저 스냅샷 복원
//        if (!isOpen)
//        {
//            var eqRT = equipmentUI ? equipmentUI : (view != null ? view.RootRect : null);
//            if (eqRT && UIPanelSwitcher.HasSnapshot)
//                UIPanelSwitcher.LoadSnapshot(eqRT);
//        }

//        isOpen = !isOpen;
//        view.Show(isOpen);
//        if (uiCamera != null) uiCamera.gameObject.SetActive(isOpen);

//        if (isOpen)
//        {
//            RefreshEquipmentUI();
//            UIEscapeStack.Instance.Push("equipment", CloseEquipment, () => isOpen);
//        }
//        else
//        {
//            UIEscapeStack.Instance.Remove("equipment");
//        }
//    }

//    private void CloseEquipment()
//    {
//        if (!isOpen) return;

//        // ★ 닫히기 직전 현재 위치 스냅샷 저장
//        var eqRT = equipmentUI ? equipmentUI : (view != null ? view.RootRect : null);
//        if (eqRT) UIPanelSwitcher.SaveSnapshot(eqRT);

//        view.Show(false);
//        if (uiCamera != null) uiCamera.gameObject.SetActive(false);
//        isOpen = false;
//        UIEscapeStack.Instance.Remove("equipment");
//    }

//    private int GetPlayerLevel()
//    {
//        var ps = PlayerStatsManager.Instance;
//        return (ps != null && ps.Data != null) ? ps.Data.Level : 1;
//    }

//    /// <summary>아이템 장착 (uniqueId 기반)</summary>
//    public void HandleEquipItem(InventoryItem item)
//    {
//        // === 레벨 제한 체크 ===
//        int reqLevel = Mathf.Max(1, item.data.level);
//        int curLevel = GetPlayerLevel();
//        if (curLevel < reqLevel)
//        {
//            Debug.LogWarning($"[장착 실패] 요구 레벨 {reqLevel}, 현재 레벨 {curLevel} → '{item.data.name}' 장착 불가");
//            // 필요하면 여기서 UI 토스트/사운드/버튼 흔들기 등 피드백 호출
//            return;
//        }

//        string slotType = item.data.type;
//        var slot = model.GetSlot(slotType);

//        // 기존 장비를 인벤토리에 추가
//        if (slot?.equipped != null)
//        {
//            inventoryPresenter?.AddExistingItem(slot.equipped);
//        }

//        // 장비 데이터 교체
//        model.EquipItem(slotType, item);

//        // 인벤토리에서 해당 아이템 제거
//        inventoryPresenter?.RemoveItemFromInventory(item.uniqueId);

//        // 캐릭터 모델 프리팹 장착
//        if (!string.IsNullOrEmpty(item.prefabPath) && targetCharacter != null)
//        {
//            var prefab = Resources.Load<GameObject>(item.prefabPath);
//            if (prefab != null)
//                AttachPrefabToCharacter(prefab, slotType);
//        }

//        // UI & 스탯 갱신
//        ApplyStatsAndSave();
//        inventoryPresenter?.Refresh();
//        RefreshEquipmentUI();
//    }

//    /// <summary>드래그앤드롭으로 장비 장착</summary>
//    private void HandleEquipFromInventory(string slotType, InventoryItem item)
//    {
//        HandleEquipItem(item);
//    }

//    /// <summary>아이템 해제 (uniqueId 기반)</summary>
//    public void HandleUnequipItem(string slotType)
//    {
//        var slot = model.GetSlot(slotType);
//        if (slot?.equipped == null) return;

//        var item = slot.equipped;

//        // 인벤토리로 반환
//        inventoryPresenter?.AddExistingItem(item);

//        // 장비 데이터 업데이트
//        model.UnequipItem(slotType);

//        // 캐릭터 모델에서 프리팹 제거
//        RemovePrefabFromCharacter(slotType);

//        inventoryPresenter?.Refresh();
//        ApplyStatsAndSave();
//        RefreshEquipmentUI();
//    }

//    // 장비 변경 시 즉시 스탯 갱신 + 저장
//    private void ApplyStatsAndSave()
//    {
//        var ps = PlayerStatsManager.Instance;
//        if (ps != null)
//        {
//            ps.RecalculateStats(model.Slots);
//            SaveLoadService.SavePlayerDataForRace(ps.Data.Race, ps.Data);
//        }
//    }

//    /// <summary>JSON 저장된 장비 데이터 복원</summary>
//    private void InitializeEquippedItems()
//    {
//        foreach (var slot in model.Slots)
//        {
//            if (slot.equipped != null && !string.IsNullOrEmpty(slot.equipped.prefabPath))
//            {
//                GameObject prefab = Resources.Load<GameObject>(slot.equipped.prefabPath);
//                if (prefab != null)
//                    AttachPrefabToCharacter(prefab, slot.slotType);
//            }
//        }
//        var ps = PlayerStatsManager.Instance;
//        if (ps != null) ps.RecalculateStats(model.Slots);
//    }

//    /// <summary>장비 프리팹 캐릭터 본에 장착</summary>
//    private void AttachPrefabToCharacter(GameObject prefab, string slotType)
//    {
//        Transform bone = GetSlotTransform(slotType);
//        if (bone == null) return;

//        if (bone.childCount > 0)
//        {
//            Transform lastChild = bone.GetChild(bone.childCount - 1);
//            Destroy(lastChild.gameObject);
//        }

//        // 새 프리팹 장착
//        GameObject instance = Instantiate(prefab, bone);
//        instance.transform.SetAsLastSibling();
//        instance.transform.localPosition = GetSlotOffset(slotType);
//        instance.transform.localRotation = Quaternion.identity;

//        // ── 여기부터 추가: 장비로 붙을 땐 줍기/툴팁/물리 비활성화 ──
//        // 1) 장착 마커 부착
//        if (instance.GetComponent<EquippedMarker>() == null)
//            instance.AddComponent<EquippedMarker>();

//        // 2) 근접 줍기/툴팁 스크립트 제거
//        foreach (var pickup in instance.GetComponentsInChildren<ItemPickup>(true))
//            Destroy(pickup);

//        // (혹시 붙어있을 수 있는) 월드 툴팁 트리거류도 제거하고 싶다면:
//        foreach (var hover in instance.GetComponentsInChildren<ItemHoverTooltip>(true))
//            Destroy(hover);

//        // 3) 물리 충돌/중력 제거 (캐릭터 본에 붙었을 땐 불필요)
//        foreach (var col in instance.GetComponentsInChildren<Collider>(true))
//            col.enabled = false;
//        foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true))
//            Destroy(rb);

//        // 4) (선택) 레이어 분리해서 다른 시스템에서 무시되게
//        // int equipLayer = LayerMask.NameToLayer("UICharacter");
//        // if (equipLayer != -1) SetLayerRecursively(instance, equipLayer);
//    }

//    /// <summary>장비 프리팹 캐릭터 본에서 제거</summary>
//    private void RemovePrefabFromCharacter(string slotType)
//    {
//        Transform bone = GetSlotTransform(slotType);
//        if (bone == null) return;

//        if (bone.childCount > 0)
//        {
//            Transform lastChild = bone.GetChild(bone.childCount - 1);
//            Destroy(lastChild.gameObject);
//        }
//    }

//    /// <summary>슬롯 본 찾기</summary>
//    private Transform GetSlotTransform(string slotType)
//    {
//        string boneName = slotType switch
//        {
//            "weapon" => "HandR",
//            "shield" => "HandL",
//            "head" => "Head",
//            "lshoulder" => "ShoulderL",
//            "rshoulder" => "ShoulderR",
//            _ => null
//        };

//        if (boneName == null) return null;

//        foreach (Transform t in targetCharacter.GetComponentsInChildren<Transform>())
//            if (t.name == boneName)
//                return t;

//        Debug.LogWarning($"{slotType} 슬롯에 해당하는 본({boneName})을 찾을 수 없습니다.");
//        return null;
//    }

//    /// <summary>슬롯별 오프셋</summary>
//    private Vector3 GetSlotOffset(string slotType)
//    {
//        return slotType switch
//        {
//            "head" => new Vector3(-0.12f, 0.035f, 0),
//            "shield" => new Vector3(0, 0, -0.05f),
//            "lshoulder" => new Vector3(0, 0, -0.2f),
//            "rshoulder" => new Vector3(0, 0, 0.2f),
//            _ => Vector3.zero
//        };
//    }

//    /// <summary>장비 UI 갱신</summary>
//    private void RefreshEquipmentUI()
//    {
//        if (view != null && model != null)
//            view.UpdateEquipmentUI(model.Slots, HandleUnequipItem);
//    }
//}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EquipmentPresenter : MonoBehaviour
{
    private EquipmentModel model;
    private EquipmentView view;
    private InventoryPresenter inventoryPresenter;

    private bool isOpen = false;                 // 장비창 열림 상태
    private RectTransform equipmentRect;         // ★ 실제로 움직이는 RT
    private RectTransform playerInfoRect;        // ★ 실제로 움직이는 RT

    [SerializeField] private Camera uiCamera;           // 장비 UI 전용 카메라
    [SerializeField] private Transform targetCharacter; // 캐릭터 모델

    private Button EquipButton;

    // 현재 플레이어 종족(카메라 간격 스위치에 사용)
    private string currentRace = "humanmale";

    public bool IsOpen => isOpen;

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

    void Start()
    {
        UIEscapeStack.GetOrCreate();

        // 종족 구해서 모델 생성 + currentRace 저장
        var ps = PlayerStatsManager.Instance;
        currentRace = (ps != null && ps.Data != null && !string.IsNullOrEmpty(ps.Data.Race))
                        ? ps.Data.Race
                        : "humanmale";

        model = new EquipmentModel(currentRace);

        view = FindAnyObjectByType<EquipmentView>();
        inventoryPresenter = FindAnyObjectByType<InventoryPresenter>();

        // 루트 GO에서 "움직이는 RT"를 얻음
        var eqRoot = GameObject.Find("EquipmentUI") ?? FindIncludingInactive("EquipmentUI");
        var piRoot = GameObject.Find("PlayerInfoUI") ?? FindIncludingInactive("PlayerInfoUI");
        equipmentRect = GetMovableWindowRT(eqRoot);
        playerInfoRect = GetMovableWindowRT(piRoot);

        if (view != null)
            view.Initialize(CloseEquipment, HandleEquipFromInventory);

        var quickUI = GameObject.Find("QuickUI");
        if (quickUI != null && quickUI.transform.childCount > 1)
        {
            EquipButton = quickUI.transform.GetChild(1).GetComponent<Button>();
            if (EquipButton) EquipButton.onClick.AddListener(ToggleEquipment);
        }

        SetupUICamera();
        InitializeEquippedItems();
        RefreshEquipmentUI();
    }

    public IReadOnlyList<EquipmentSlot> GetEquipmentSlots()
    {
        return model?.Slots;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            var pi = FindAnyObjectByType<PlayerInfoPresenter>();
            bool playerWasOpen = pi && pi.IsOpen;

            // 전환 직전, 플레이어인포가 켜져 있으면 스냅샷 저장 + 위치 복사 (모두 "움직이는 RT" 기준)
            if (playerWasOpen && playerInfoRect)
            {
                Debug.Log($"[SNAP] Save from: {PathOf(equipmentRect)} localPos={equipmentRect.localPosition}");
                UIPanelSwitcher.SaveSnapshot(playerInfoRect);
            }

            if (playerWasOpen && equipmentRect && playerInfoRect)
                UIPanelSwitcher.CopyLayoutRT(playerInfoRect, equipmentRect);

            ToggleEquipment();

            if (playerWasOpen && pi)
            {
                pi.Close();
                if (equipmentRect && playerInfoRect)
                    UIPanelSwitcher.CopyLayoutRT(equipmentRect, playerInfoRect);
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


    void LateUpdate()
    {
        // UI 카메라가 캐릭터를 바라보도록 유지
        if (uiCamera != null && targetCharacter != null)
        {
            float dist = GetRaceCameraDistance(currentRace);
            float height = GetRaceLookAtHeight(currentRace);
            float lookY = GetRaceCameraHeight(currentRace);

            Vector3 offset = targetCharacter.forward * dist + Vector3.up * height;
            uiCamera.transform.position = targetCharacter.position + offset;
            uiCamera.transform.LookAt(targetCharacter.position + Vector3.up * lookY);
        }
    }

    private float GetRaceCameraDistance(string race)
    {
        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();
        switch (r)
        {
            case "humanmale": return 2.2f;
            case "dwarfmale": return 2.0f;
            case "gnomemale": return 1.2f;
            case "nightelfmale": return 2.7f;
            case "orcmale": return 2.5f;
            case "trollmale": return 2.8f;
            case "goblinmale": return 1.7f;
            case "scourgefemale": return 2.0f;
            default: return 2.2f;
        }
    }

    private float GetRaceLookAtHeight(string race)
    {
        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();
        switch (r)
        {
            case "humanmale": return 1.4f;
            case "dwarfmale": return 1.3f;
            case "gnomemale": return 0.7f;
            case "nightelfmale": return 1.6f;
            case "orcmale": return 1.4f;
            case "trollmale": return 1.5f;
            case "goblinmale": return 1.0f;
            case "scourgefemale": return 1.2f;
            default: return 1.4f;
        }
    }

    private float GetRaceCameraHeight(string race)
    {
        string r = string.IsNullOrEmpty(race) ? "humanmale" : race.ToLowerInvariant();
        switch (r)
        {
            case "humanmale": return 1.0f;
            case "dwarfmale": return 0.7f;
            case "gnomemale": return 0.5f;
            case "nightelfmale": return 1.3f;
            case "orcmale": return 1.1f;
            case "trollmale": return 1.3f;
            case "goblinmale": return 0.6f;
            case "scourgefemale": return 0.9f;
            default: return 1.0f;
        }
    }

    private void SetupUICamera()
    {
        if (uiCamera == null)
        {
            int uiLayer = LayerMask.NameToLayer("UICharacter");
            if (uiLayer != -1)
            {
                Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (Camera cam in cameras)
                {
                    if (cam.gameObject.layer == uiLayer)
                    {
                        uiCamera = cam;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>외부에서 켜기</summary>
    public void OpenEquipment()
    {
        if (!isOpen) ToggleEquipment();
    }

    /// <summary>외부에서 끄기</summary>
    public void CloseEquipmentPublic()
    {
        if (isOpen) CloseEquipment();
    }

    /// <summary>장비창 열기/닫기</summary>
    public void ToggleEquipment()
    {
        if (view == null) return;

        if (!isOpen)
        {
            // 열릴 때: 스냅샷 복원
            if (equipmentRect && UIPanelSwitcher.HasSnapshot)
                UIPanelSwitcher.LoadSnapshot(equipmentRect);

            isOpen = true;
            view.Show(true);
            if (uiCamera) uiCamera.gameObject.SetActive(true);
            RefreshEquipmentUI();
            UIEscapeStack.Instance.Push("equipment", CloseEquipment, () => isOpen);
        }
        else
        {
            // 닫힐 때: 반드시 스냅샷 저장하도록 CloseEquipment() 사용
            CloseEquipment();
        }
    }


    private void CloseEquipment()
    {
        if (!isOpen) return;

        // 닫히기 직전 현재 위치 스냅샷 저장 (움직이는 RT)
        if (equipmentRect) UIPanelSwitcher.SaveSnapshot(equipmentRect);

        view.Show(false);
        if (uiCamera != null) uiCamera.gameObject.SetActive(false);
        isOpen = false;
        UIEscapeStack.Instance.Remove("equipment");
    }

    private int GetPlayerLevel()
    {
        var ps = PlayerStatsManager.Instance;
        return (ps != null && ps.Data != null) ? ps.Data.Level : 1;
    }

    /// <summary>아이템 장착 (uniqueId 기반)</summary>
    public void HandleEquipItem(InventoryItem item)
    {
        int reqLevel = Mathf.Max(1, item.data.level);
        int curLevel = GetPlayerLevel();
        if (curLevel < reqLevel)
        {
            Debug.LogWarning($"[장착 실패] 요구 레벨 {reqLevel}, 현재 레벨 {curLevel} → '{item.data.name}' 장착 불가");
            return;
        }

        string slotType = item.data.type;
        var slot = model.GetSlot(slotType);

        // 기존 장비를 인벤토리에 추가
        if (slot?.equipped != null)
        {
            inventoryPresenter?.AddExistingItem(slot.equipped);
        }

        // 장비 데이터 교체
        model.EquipItem(slotType, item);

        // 인벤토리에서 해당 아이템 제거
        inventoryPresenter?.RemoveItemFromInventory(item.uniqueId);

        // 캐릭터 모델 프리팹 장착
        if (!string.IsNullOrEmpty(item.prefabPath) && targetCharacter != null)
        {
            var prefab = Resources.Load<GameObject>(item.prefabPath);
            if (prefab != null)
                AttachPrefabToCharacter(prefab, slotType);
        }

        // UI & 스탯 갱신
        ApplyStatsAndSave();
        inventoryPresenter?.Refresh();
        RefreshEquipmentUI();
    }

    private void HandleEquipFromInventory(string slotType, InventoryItem item)
    {
        HandleEquipItem(item);
    }

    public void HandleUnequipItem(string slotType)
    {
        var slot = model.GetSlot(slotType);
        if (slot?.equipped == null) return;

        var item = slot.equipped;

        inventoryPresenter?.AddExistingItem(item);
        model.UnequipItem(slotType);
        RemovePrefabFromCharacter(slotType);

        inventoryPresenter?.Refresh();
        ApplyStatsAndSave();
        RefreshEquipmentUI();
    }

    private void ApplyStatsAndSave()
    {
        var ps = PlayerStatsManager.Instance;
        if (ps != null)
        {
            ps.RecalculateStats(model.Slots);
            SaveLoadService.SavePlayerDataForRace(ps.Data.Race, ps.Data);
        }

        // ★ 추가: 플레이어인포 텍스트 갱신
        var pi = FindAnyObjectByType<PlayerInfoPresenter>(FindObjectsInactive.Include);
        if (pi != null) pi.RefreshStatsText();
    }

    private void InitializeEquippedItems()
    {
        foreach (var slot in model.Slots)
        {
            if (slot.equipped != null && !string.IsNullOrEmpty(slot.equipped.prefabPath))
            {
                GameObject prefab = Resources.Load<GameObject>(slot.equipped.prefabPath);
                if (prefab != null)
                    AttachPrefabToCharacter(prefab, slot.slotType);
            }
        }
        var ps = PlayerStatsManager.Instance;
        if (ps != null) ps.RecalculateStats(model.Slots);
    }

    private void AttachPrefabToCharacter(GameObject prefab, string slotType)
    {
        Transform bone = GetSlotTransform(slotType);
        if (bone == null) return;

        if (bone.childCount > 0)
        {
            Transform lastChild = bone.GetChild(bone.childCount - 1);
            Destroy(lastChild.gameObject);
        }

        GameObject instance = Instantiate(prefab, bone);
        instance.transform.SetAsLastSibling();
        instance.transform.localPosition = GetSlotOffset(slotType);
        instance.transform.localRotation = Quaternion.identity;

        if (instance.GetComponent<EquippedMarker>() == null)
            instance.AddComponent<EquippedMarker>();

        foreach (var pickup in instance.GetComponentsInChildren<ItemPickup>(true))
            Destroy(pickup);
        foreach (var hover in instance.GetComponentsInChildren<ItemHoverTooltip>(true))
            Destroy(hover);
        foreach (var col in instance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
        foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true))
            Destroy(rb);
    }

    private void RemovePrefabFromCharacter(string slotType)
    {
        Transform bone = GetSlotTransform(slotType);
        if (bone == null) return;

        if (bone.childCount > 0)
        {
            Transform lastChild = bone.GetChild(bone.childCount - 1);
            Destroy(lastChild.gameObject);
        }
    }

    private Transform GetSlotTransform(string slotType)
    {
        string boneName = slotType switch
        {
            "weapon" => "HandR",
            "shield" => "HandL",
            "head" => "Head",
            "lshoulder" => "ShoulderL",
            "rshoulder" => "ShoulderR",
            _ => null
        };

        if (boneName == null) return null;

        foreach (Transform t in targetCharacter.GetComponentsInChildren<Transform>())
            if (t.name == boneName)
                return t;

        Debug.LogWarning($"{slotType} 슬롯에 해당하는 본({boneName})을 찾을 수 없습니다.");
        return null;
    }

    private Vector3 GetSlotOffset(string slotType)
    {
        return slotType switch
        {
            "head" => new Vector3(-0.12f, 0.035f, 0),
            "shield" => new Vector3(0, 0, -0.05f),
            "lshoulder" => new Vector3(0, 0, -0.2f),
            "rshoulder" => new Vector3(0, 0, 0.2f),
            _ => Vector3.zero
        };
    }

    private void RefreshEquipmentUI()
    {
        if (view != null && model != null)
            view.UpdateEquipmentUI(model.Slots, HandleUnequipItem);
    }
}
