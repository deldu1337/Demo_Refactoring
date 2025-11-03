using System;
using System.Collections.Generic;

[Serializable]
public class PotionQuickBarSave
{
    public List<PotionSlotEntry> slots = new(); // index, uid, id, iconPath, prefabPath, hp, mp
}

[Serializable]
public class PotionSlotEntry
{
    public int index;
    public string uniqueId;     // 인벤 UID (퀵슬롯에 이관된 후에도 유지)
    public int itemId;          // DataManager용 id (재구성용)
    public string iconPath;     // Resources 경로 (예: "Icons/HPPotion")
    public string prefabPath;   // 원래 프리팹 경로(있으면)
    public float hp;
    public float mp;

    // ★ 추가: 수량
    public int qty;
}
