// QuickBarSaveData.cs
using System;
using System.Collections.Generic;

/// <summary>
/// 퀵바 슬롯 정보를 직렬화하기 위한 컨테이너입니다.
/// </summary>
[Serializable]
public class QuickBarSave
{
    public List<SlotEntry> slots = new();
}

/// <summary>
/// 슬롯 인덱스와 스킬 아이디를 한 쌍으로 저장합니다.
/// </summary>
[Serializable]
public class SlotEntry
{
    public int index;
    public string skillId;
}
