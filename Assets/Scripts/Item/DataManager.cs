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

    // ★ 추가: 아이템별 스탯 범위 맵
    // id -> (stat -> range)
    private readonly Dictionary<int, Dictionary<string, ItemStatRange>> _ranges
        = new Dictionary<int, Dictionary<string, ItemStatRange>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDatas();
        LoadRanges(); // ★ 추가
    }

    public void LoadDatas()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Datas/itemData");
        if (textAsset == null) { Debug.LogError("Resources/Datas/itemData.json 파일을 확인하세요!"); return; }

        var json = textAsset.text;
        ItemDataArray wrapper = JsonUtility.FromJson<ItemDataArray>(json);
        if (wrapper == null || wrapper.items == null) { Debug.LogError("JSON 파싱 실패! JSON 형식 확인 필요."); return; }

        dicItemDatas = new Dictionary<int, ItemData>();
        foreach (var data in wrapper.items) dicItemDatas[data.id] = data;
    }

    // ★ 범위 JSON 로더 (예: Resources/Datas/itemRanges.json)
    private void LoadRanges()
    {
        _ranges.Clear();

        var ta = Resources.Load<TextAsset>("Datas/itemRanges");
        if (ta == null)
        {
            Debug.LogWarning("[DataManager] itemRanges.json 을 찾지 못했습니다. (범위 롤링 비활성)");
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

    // ★ 외부에서 범위 요청
    public bool TryGetRange(int itemId, string stat, out ItemStatRange range)
    {
        range = null;
        if (_ranges.TryGetValue(itemId, out var map))
            return map.TryGetValue(stat, out range);
        return false;
    }
}
