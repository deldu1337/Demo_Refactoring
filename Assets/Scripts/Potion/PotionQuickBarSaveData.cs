using System;
using System.Collections.Generic;

[Serializable]
/// <summary>
/// 포션 퀵바의 저장 데이터를 보관하는 컨테이너입니다.
/// </summary>
public class PotionQuickBarSave
{
    public List<PotionSlotEntry> slots = new(); // 슬롯 정보를 순서대로 보관합니다.
}

[Serializable]
/// <summary>
/// 각 포션 슬롯의 저장 정보를 표현합니다.
/// </summary>
public class PotionSlotEntry
{
    public int index;
    public string uniqueId;     // 인벤토리 아이템 고유 ID입니다.
    public int itemId;          // DataManager에서 사용되는 아이템 ID입니다.
    public string iconPath;     // Resources 경로의 아이콘 정보입니다.
    public string prefabPath;   // 프리팹 경로 정보입니다.
    public float hp;
    public float mp;

    // 저장된 수량입니다.
    public int qty;
}
