using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀵 슬롯 UI를 관리하며 스킬 배치와 저장 기능을 제공합니다.
/// </summary>
public class SkillQuickBar : MonoBehaviour
{
    public SkillSlotUI[] slots;

    public event Action OnChanged;

    /// <summary>
    /// 자식 객체에서 슬롯과 부가 컴포넌트를 자동으로 연결합니다.
    /// </summary>
    public void AutoWireSlots()
    {
        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<SkillSlotUI>(true);

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (!s) continue;

            s.index = i;

            if (!s.icon)
            {
                var iconTr = s.transform.Find("A");
                var img = iconTr ? iconTr.GetComponent<Image>() : null;
                if (!img) img = s.GetComponentInChildren<Image>(true);
                if (img) { s.icon = img; s.icon.raycastTarget = false; }
            }

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

    /// <summary>
    /// 특정 슬롯에 스킬을 설정합니다.
    /// </summary>
    public void Assign(int index, string skillId, Sprite icon)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index].SetSkill(skillId, icon);
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 첫 번째 빈 슬롯에 스킬을 배치합니다.
    /// </summary>
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

    /// <summary>
    /// 두 슬롯의 스킬 정보를 교환합니다.
    /// </summary>
    public void Swap(int a, int b)
    {
        if (a == b) return;
        if (a < 0 || b < 0 || a >= slots.Length || b >= slots.Length) return;
        var tmp = slots[a].GetData();
        slots[a].ApplyData(slots[b].GetData());
        slots[b].ApplyData(tmp);
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 지정된 인덱스의 스킬 아이디를 반환합니다.
    /// </summary>
    public string GetSkillAt(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index].SkillId;
    }

    /// <summary>
    /// 퀵바 상태를 저장용 데이터로 변환합니다.
    /// </summary>
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
    /// 저장된 데이터를 적용하며 사용할 수 없는 스킬은 건너뜁니다.
    /// </summary>
    public void ApplySaveData(
        QuickBarSave save,
        Func<string, Sprite> iconResolver,
        Func<string, bool> canUse)
    {
        if (save == null) return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSkill(null, null);

        foreach (var e in save.slots)
        {
            if (e.index < 0 || e.index >= slots.Length) continue;
            if (string.IsNullOrEmpty(e.skillId)) continue;

            if (canUse != null && !canUse(e.skillId)) continue;

            var sp = iconResolver?.Invoke(e.skillId);
            slots[e.index].SetSkill(e.skillId, sp);
        }

        OnChanged?.Invoke();
    }

    /// <summary>
    /// 인덱스에 해당하는 슬롯을 반환합니다.
    /// </summary>
    public SkillSlotUI GetSlot(int index)
    {
        if (index < 0 || index >= (slots?.Length ?? 0)) return null;
        return slots[index];
    }
}
