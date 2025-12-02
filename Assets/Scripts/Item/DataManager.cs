using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemDataArray { public ItemData[] items; }

[Serializable]
public class ItemStatRange { public float min; public float max; }

[Serializable]
public class ItemRangeEntry
{
    public int id;
    public ItemStatRange hp;
    public ItemStatRange mp;
    public ItemStatRange atk;
    public ItemStatRange def;
    public ItemStatRange dex;
    public ItemStatRange As;
    public ItemStatRange cc;
    public ItemStatRange cd;
}

[Serializable]
public class ItemRangeArray { public ItemRangeEntry[] items; }

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public Dictionary<int, ItemData> dicItemDatas;

    // 아이템 ID별로 스탯 범위를 관리하는 내부 사전입니다.
    private readonly Dictionary<int, Dictionary<string, ItemStatRange>> _ranges
        = new Dictionary<int, Dictionary<string, ItemStatRange>>();

    /// <summary>
    /// 싱글턴 인스턴스를 설정하고 아이템 데이터와 범위를 불러옵니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDatas();
        LoadRanges();
    }

    /// <summary>
    /// 리소스에서 아이템 기본 정보를 읽어와 사전에 저장합니다.
    /// </summary>
    public void LoadDatas()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Datas/itemData");
        if (textAsset == null) { Debug.LogError("Resources/Datas/itemData.json 파일을 찾지 못했습니다"); return; }

        var json = textAsset.text;
        ItemDataArray wrapper = JsonUtility.FromJson<ItemDataArray>(json);
        if (wrapper == null || wrapper.items == null) { Debug.LogError("JSON 형식이 올바르지 않습니다. 파일을 확인해 주십시오"); return; }

        dicItemDatas = new Dictionary<int, ItemData>();
        foreach (var data in wrapper.items) dicItemDatas[data.id] = data;
    }

    /// <summary>
    /// 리소스에서 아이템 스탯 범위를 읽어와 내부 사전에 저장합니다.
    /// </summary>
    private void LoadRanges()
    {
        _ranges.Clear();

        var ta = Resources.Load<TextAsset>("Datas/itemRanges");
        if (ta == null)
        {
            Debug.LogWarning("[DataManager] itemRanges.json 파일을 찾지 못했습니다. 범위 기능을 건너뜁니다");
            return;
        }

        var wrapper = JsonUtility.FromJson<ItemRangeArray>(ta.text);
        if (wrapper?.items == null) return;

        foreach (var e in wrapper.items)
        {
            var map = new Dictionary<string, ItemStatRange>();
            if (e.hp != null) map["hp"] = e.hp;
            if (e.mp != null) map["mp"] = e.mp;
            if (e.atk != null) map["atk"] = e.atk;
            if (e.def != null) map["def"] = e.def;
            if (e.dex != null) map["dex"] = e.dex;
            if (e.As != null) map["As"] = e.As;
            if (e.cc != null) map["cc"] = e.cc;
            if (e.cd != null) map["cd"] = e.cd;

            _ranges[e.id] = map;
        }
    }

    /// <summary>
    /// 요청한 아이템의 특정 스탯 범위를 반환합니다.
    /// </summary>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <param name="stat">조회할 스탯 키입니다.</param>
    /// <param name="range">찾은 범위를 반환할 출력 값입니다.</param>
    /// <returns>범위를 찾았을 때 참을 반환합니다.</returns>
    public bool TryGetRange(int itemId, string stat, out ItemStatRange range)
    {
        range = null;
        if (_ranges.TryGetValue(itemId, out var map))
            return map.TryGetValue(stat, out range);
        return false;
    }
}
