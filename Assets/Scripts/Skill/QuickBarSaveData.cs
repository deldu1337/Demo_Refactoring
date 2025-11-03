// QuickBarSaveData.cs
using System;
using System.Collections.Generic;

[Serializable]
public class QuickBarSave
{
    public List<SlotEntry> slots = new(); // index, skillId
}

[Serializable]
public class SlotEntry
{
    public int index;
    public string skillId;
}
