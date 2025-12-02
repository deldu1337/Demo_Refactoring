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

    private bool isOpen = false;                 // 장비창 열림 여부
    private RectTransform equipmentRect;         // 이동 가능한 장비창 RectTransform
    private RectTransform playerInfoRect;        // 이동 가능한 플레이어 정보 RectTransform

    [SerializeField] private Camera uiCamera;           // 장비 UI 전용 카메라
    [SerializeField] private Transform targetCharacter; // 캐릭터 모델 트랜스폼

    private Button EquipButton;

    // 현재 플레이어 종족(카메라 간격 스위치에 사용)
    private string currentRace = "humanmale";

    public bool IsOpen => isOpen;

    /// <summary>
    /// 비활성 객체를 포함해 이름으로 GameObject를 찾는다.
    /// </summary>
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
    /// 창 루트를 찾아 실제로 이동하는 RectTransform을 반환한다.
    /// </summary>
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
            cand = headParentRT; // HeadPanel의 부모가 창 루트
        }
        else
        {
            // 2) fallback: root의 RectTransform
            cand = root.GetComponent<RectTransform>();
        }

        if (!cand) return null;

        // 3) Canvas 직계 자식 레벨까지 끌어올리기
        RectTransform cur = cand;
        while (cur && cur.parent is RectTransform prt)
        {
            if (prt.GetComponent<Canvas>() != null) break;
            cur = prt;
        }
        return cur;
    }

    /// <summary>
    /// 초기화 시 모델, 뷰, 카메라를 설정하고 장비 UI를 준비한다.
    /// </summary>
    private void Start()
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

        // 루트 오브젝트에서 이동 가능한 RectTransform을 찾는다.
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

    /// <summary>
    /// 현재 장비 슬롯 목록을 외부에 제공한다.
    /// </summary>
    public IReadOnlyList<EquipmentSlot> GetEquipmentSlots()
    {
        return model?.Slots;
    }

    /// <summary>
    /// 입력을 감지해 장비창과 플레이어 정보창 전환을 처리한다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            var pi = FindAnyObjectByType<PlayerInfoPresenter>();
            bool playerWasOpen = pi && pi.IsOpen;

            // 전환 직전, 플레이어 정보가 켜져 있으면 스냅샷 저장 후 위치를 복사한다.
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

    /// <summary>
    /// 트랜스폼의 경로를 문자열로 반환한다.
    /// </summary>
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
    /// UI 카메라가 캐릭터를 바라보도록 위치를 유지한다.
    /// </summary>
    private void LateUpdate()
    {
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

    /// <summary>
    /// 종족별 카메라 거리를 계산한다.
    /// </summary>
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

    /// <summary>
    /// 종족별로 바라볼 높이를 계산한다.
    /// </summary>
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

    /// <summary>
    /// 종족별 카메라 높이를 계산한다.
    /// </summary>
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

    /// <summary>
    /// 장비 UI 카메라를 찾거나 지정한다.
    /// </summary>
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

    /// <summary>
    /// 외부에서 장비창을 열도록 요청한다.
    /// </summary>
    public void OpenEquipment()
    {
        if (!isOpen) ToggleEquipment();
    }

    /// <summary>
    /// 외부에서 장비창을 닫도록 요청한다.
    /// </summary>
    public void CloseEquipmentPublic()
    {
        if (isOpen) CloseEquipment();
    }

    /// <summary>
    /// 장비창을 토글한다.
    /// </summary>
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
            // 닫힐 때: 반드시 스냅샷 저장하도록 CloseEquipment 사용
            CloseEquipment();
        }
    }

    /// <summary>
    /// 장비창을 닫고 위치 스냅샷을 저장한다.
    /// </summary>
    private void CloseEquipment()
    {
        if (!isOpen) return;

        // 닫히기 직전 현재 위치 스냅샷 저장
        if (equipmentRect) UIPanelSwitcher.SaveSnapshot(equipmentRect);

        view.Show(false);
        if (uiCamera != null) uiCamera.gameObject.SetActive(false);
        isOpen = false;
        UIEscapeStack.Instance.Remove("equipment");
    }

    /// <summary>
    /// 플레이어 레벨을 가져온다.
    /// </summary>
    private int GetPlayerLevel()
    {
        var ps = PlayerStatsManager.Instance;
        return (ps != null && ps.Data != null) ? ps.Data.Level : 1;
    }

    /// <summary>
    /// 아이템을 장착하고 관련 UI와 데이터를 갱신한다.
    /// </summary>
    public void HandleEquipItem(InventoryItem item)
    {
        int reqLevel = Mathf.Max(1, item.data.level);
        int curLevel = GetPlayerLevel();
        if (curLevel < reqLevel)
        {
            Debug.LogWarning($"[장착 실패] 요구 레벨 {reqLevel}, 현재 레벨 {curLevel} 로 '{item.data.name}' 장착 불가");
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

        // UI와 스탯 갱신
        ApplyStatsAndSave();
        inventoryPresenter?.Refresh();
        RefreshEquipmentUI();
    }

    /// <summary>
    /// 인벤토리에서 끌어온 아이템을 장착할 때 호출한다.
    /// </summary>
    private void HandleEquipFromInventory(string slotType, InventoryItem item)
    {
        HandleEquipItem(item);
    }

    /// <summary>
    /// 슬롯에서 아이템을 해제하고 UI를 갱신한다.
    /// </summary>
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

    /// <summary>
    /// 능력치를 다시 계산하고 저장하며 정보 텍스트를 갱신한다.
    /// </summary>
    private void ApplyStatsAndSave()
    {
        var ps = PlayerStatsManager.Instance;
        if (ps != null)
        {
            ps.RecalculateStats(model.Slots);
            SaveLoadService.SavePlayerDataForRace(ps.Data.Race, ps.Data);
        }

        // 플레이어 정보 텍스트 갱신
        var pi = FindAnyObjectByType<PlayerInfoPresenter>(FindObjectsInactive.Include);
        if (pi != null) pi.RefreshStatsText();
    }

    /// <summary>
    /// 저장된 장비 프리팹을 캐릭터에 장착하고 스탯을 초기화한다.
    /// </summary>
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

    /// <summary>
    /// 특정 슬롯 본에 프리팹을 장착하고 물리 및 인터랙션 컴포넌트를 정리한다.
    /// </summary>
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

    /// <summary>
    /// 슬롯에 장착된 프리팹을 제거한다.
    /// </summary>
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

    /// <summary>
    /// 슬롯 유형에 해당하는 본을 찾는다.
    /// </summary>
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

        Debug.LogWarning($"{slotType} 슬롯에 해당하는 본 {boneName} 을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 슬롯별 위치 오프셋을 반환한다.
    /// </summary>
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

    /// <summary>
    /// 뷰에 현재 장비 정보를 반영한다.
    /// </summary>
    private void RefreshEquipmentUI()
    {
        if (view != null && model != null)
            view.UpdateEquipmentUI(model.Slots, HandleUnequipItem);
    }
}
