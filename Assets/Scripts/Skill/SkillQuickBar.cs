// SkillQuickBar.cs
// SkillQuickBar.cs (추가/수정)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillQuickBar : MonoBehaviour
{
    public SkillSlotUI[] slots;

    public event Action OnChanged; // ← 슬롯 구성 바뀔 때 저장 트리거

    public void AutoWireSlots()
    {
        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<SkillSlotUI>(true);

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s) continue;

            s.index = i;

            // 아이콘 자동 연결
            if (!s.icon)
            {
                var iconTr = s.transform.Find("A");
                var img = iconTr ? iconTr.GetComponent<Image>() : null;
                if (!img) img = s.GetComponentInChildren<Image>(true);
                if (img) { s.icon = img; s.icon.raycastTarget = false; }
            }

            // 쿨다운 자동 연결
            if (!s.cooldownUI)
            {
                var cui = s.GetComponent<SkillCooldownUI>();
                if (!cui) cui = s.gameObject.AddComponent<SkillCooldownUI>();
                s.cooldownUI = cui;
            }
            var mask = s.transform.Find("MaskArea");
            var overlay = mask ? mask.Find("CooldownOverlay") : null;
            var overlayImg = overlay ? overlay.GetComponent<Image>() : null;
            if (overlayImg) s.cooldownUI.BindOverlay(overlayImg);
        }
    }

    public void Assign(int index, string skillId, Sprite icon)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index].SetSkill(skillId, icon);
        OnChanged?.Invoke(); // 저장 트리거
    }

    public bool AssignToFirstEmpty(string skillId, Sprite icon)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (string.IsNullOrEmpty(slots[i].SkillId))
            {
                Assign(i, skillId, icon);
                return true;
            }
        }
        return false;
    }

    public void Swap(int a, int b)
    {
        if (a == b) return;
        if (a < 0 || b < 0 || a >= slots.Length || b >= slots.Length) return;
        var tmp = slots[a].GetData();
        slots[a].ApplyData(slots[b].GetData());
        slots[b].ApplyData(tmp);
        OnChanged?.Invoke(); // 저장 트리거
    }

    public string GetSkillAt(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index].SkillId;
    }

    // ==== JSON으로 내보내기/불러오기 ====
    public QuickBarSave ToSaveData()
    {
        var data = new QuickBarSave();
        for (int i = 0; i < slots.Length; i++)
        {
            var id = slots[i].SkillId;
            if (!string.IsNullOrEmpty(id))
                data.slots.Add(new SlotEntry { index = i, skillId = id });
        }
        return data;
    }

    /// <summary>
    /// 저장 데이터를 적용. iconResolver로 아이콘 주입.
    /// canUse(skillId)로 언락 여부 검증(언락 안 된건 무시).
    /// </summary>
    public void ApplySaveData(
        QuickBarSave save,
        Func<string, Sprite> iconResolver,
        Func<string, bool> canUse)
    {
        if (save == null) return;

        // 전체 초기화(원하면 유지 가능)
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSkill(null, null);

        foreach (var e in save.slots)
        {
            if (e.index < 0 || e.index >= slots.Length) continue;
            if (string.IsNullOrEmpty(e.skillId)) continue;

            // 언락 검증(치트/구버전 보호)
            if (canUse != null && !canUse(e.skillId)) continue;

            var sp = iconResolver?.Invoke(e.skillId);
            slots[e.index].SetSkill(e.skillId, sp);
        }

        OnChanged?.Invoke(); // 적용 후 저장(선택)
    }

    public SkillSlotUI GetSlot(int index)
    {
        if (index < 0 || index >= (slots?.Length ?? 0)) return null;
        return slots[index];
    }
}
