using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ItemStatCompare
{
    private const string POS = "#35C759";
    private const string NEG = "#FF3B30";
    private const string ZERO = "#A1A1A6";
    private const string LABEL = "#DADADA";
    private const string MAX = "#49DDDF";
    private const float EPS = 0.0001f;

    /// <summary>
    /// 스탯 키에 맞는 표시 라벨을 반환합니다.
    /// </summary>
    /// <param name="key">라벨을 확인할 스탯 키입니다.</param>
    /// <returns>UI에 사용할 라벨 문자열입니다.</returns>
    private static string LabelFor(string key) => key switch
    {
        "hp" => "HP",
        "mp" => "MP",
        "atk" => "데미지",
        "def" => "방어력",
        "dex" => "민첩성",
        "As" => "공격 속도",
        "cc" => "치명타 확률",
        "cd" => "치명타 데미지",
        _ => key.ToUpper()
    };

    /// <summary>
    /// 스탯 값에 맞는 문자열을 생성합니다.
    /// </summary>
    /// <param name="key">스탯 키입니다.</param>
    /// <param name="v">표시할 값입니다.</param>
    /// <returns>형식화된 값 문자열입니다.</returns>
    private static string FormatValue(string key, float v)
    {
        if (key == "cc") return $"{v * 100f:0.##}%";
        if (key == "cd") return $"x{v:0.##}";
        return $"{v:0.##}";
    }

    /// <summary>
    /// 차이를 색상 태그와 함께 표시할 문자열로 변환합니다.
    /// </summary>
    /// <param name="diff">인벤토리와 장착 아이템의 차이 값입니다.</param>
    /// <returns>색상이 적용된 차이 문자열입니다.</returns>
    private static string ColorizeDiff(float diff)
    {
        string color = diff > EPS ? POS : (diff < -EPS ? NEG : ZERO);
        string val = diff.ToString(diff == Mathf.RoundToInt(diff) ? "+0;-0;0" : "+0.##;-0.##;0");
        return $"<color={color}>{val}</color>";
    }

    /// <summary>
    /// 굴려진 옵션 여부에 따라 사용할 값을 결정합니다.
    /// </summary>
    /// <param name="baseVal">기본 스탯 값입니다.</param>
    /// <param name="hasRolled">해당 스탯이 무작위로 설정되었는지 여부입니다.</param>
    /// <param name="rolledVal">무작위로 설정된 값입니다.</param>
    /// <returns>표시 및 비교에 사용할 최종 값입니다.</returns>
    private static float Eff(float baseVal, bool hasRolled, float rolledVal)
        => hasRolled ? rolledVal : baseVal;

    /// <summary>
    /// 인벤토리 아이템에서 각 스탯 값을 추출합니다.
    /// </summary>
    private static void Take(InventoryItem item,
        out float hp, out float mp, out float atk, out float def, out float dex, out float AS, out float cc, out float cd)
    {
        if (item.rolled != null)
        {
            hp = Eff(item.data.hp, item.rolled.hasHp, item.rolled.hp);
            mp = Eff(item.data.mp, item.rolled.hasMp, item.rolled.mp);
            atk = Eff(item.data.atk, item.rolled.hasAtk, item.rolled.atk);
            def = Eff(item.data.def, item.rolled.hasDef, item.rolled.def);
            dex = Eff(item.data.dex, item.rolled.hasDex, item.rolled.dex);
            AS = Eff(item.data.As, item.rolled.hasAs, item.rolled.As);
            cc = Eff(item.data.cc, item.rolled.hasCc, item.rolled.cc);
            cd = Eff(item.data.cd, item.rolled.hasCd, item.rolled.cd);
        }
        else
        {
            hp = item.data.hp; mp = item.data.mp; atk = item.data.atk; def = item.data.def;
            dex = item.data.dex; AS = item.data.As; cc = item.data.cc; cd = item.data.cd;
        }
    }

    /// <summary>
    /// 실제로 값이 존재하는 스탯만 모아 딕셔너리로 반환합니다.
    /// </summary>
    /// <param name="item">대상 인벤토리 아이템입니다.</param>
    /// <returns>값이 0이 아닌 스탯과 값을 담은 사전입니다.</returns>
    private static Dictionary<string, float> GatherNonZeroStats(InventoryItem item)
    {
        Take(item, out var hp, out var mp, out var atk, out var def, out var dex, out var AS, out var cc, out var cd);
        var d = new Dictionary<string, float>(8);
        if (Mathf.Abs(hp) > EPS) d["hp"] = hp;
        if (Mathf.Abs(mp) > EPS) d["mp"] = mp;
        if (Mathf.Abs(atk) > EPS) d["atk"] = atk;
        if (Mathf.Abs(def) > EPS) d["def"] = def;
        if (Mathf.Abs(dex) > EPS) d["dex"] = dex;
        if (Mathf.Abs(AS) > EPS) d["As"] = AS;
        if (Mathf.Abs(cc) > EPS) d["cc"] = cc;
        if (Mathf.Abs(cd) > EPS) d["cd"] = cd;
        return d;
    }

    /// <summary>
    /// 인벤토리 아이템과 장착 아이템의 스탯을 비교하여 설명 문자열을 생성합니다.
    /// </summary>
    /// <param name="inv">비교할 인벤토리 아이템입니다.</param>
    /// <param name="eq">현재 장착 중인 아이템입니다.</param>
    /// <param name="showEquippedValues">장착 값을 기준으로 표시할지 여부입니다.</param>
    /// <returns>비교 결과가 포함된 문자열입니다.</returns>
    public static string BuildCompareLines(InventoryItem inv, InventoryItem eq, bool showEquippedValues)
    {
        bool invGem = inv?.data?.type != null && inv.data.type.Equals("gem", StringComparison.OrdinalIgnoreCase);
        bool eqGem = eq?.data?.type != null && eq.data.type.Equals("gem", StringComparison.OrdinalIgnoreCase);
        bool isGemMode = invGem || eqGem;

        var invStats = GatherNonZeroStats(inv);
        var eqStats = GatherNonZeroStats(eq);

        string[] ORDER = { "hp", "mp", "atk", "def", "dex", "As", "cc", "cd" };
        Func<string, int> idx = k =>
        {
            int i = Array.IndexOf(ORDER, k);
            return i < 0 ? 999 : i;
        };

        var keySet = new HashSet<string>(invStats.Keys);
        if (isGemMode) foreach (var k in eqStats.Keys) keySet.Add(k);

        var keys = new List<string>(keySet);
        keys.Sort((a, b) =>
        {
            int ia = idx(a), ib = idx(b);
            if (ia != ib) return ia.CompareTo(ib);
            return string.Compare(a, b, StringComparison.Ordinal);
        });

        var bothLines = new List<string>();
        var removedLines = new List<string>();
        var newLines = new List<string>();

        foreach (var key in keys)
        {
            invStats.TryGetValue(key, out var invV);
            eqStats.TryGetValue(key, out var eqV);
            bool invHas = invStats.ContainsKey(key);
            bool eqHas = eqStats.ContainsKey(key);

            float shown = showEquippedValues
                ? (eqHas ? eqV : invV)
                : (invHas ? invV : eqV);

            bool isMax = false;
            if (inv != null && eq != null)
            {
                var shownItem = showEquippedValues ? (eqHas ? eq : inv) : (invHas ? inv : eq);
                if (shownItem != null && shownItem.data != null &&
                    !string.Equals(shownItem.data.type, "potion", StringComparison.OrdinalIgnoreCase))
                {
                    isMax = ItemRoller.IsMaxRoll(shownItem.id, key, shown);
                }
            }

            string valueStr = FormatValue(key, shown);
            if (isMax) valueStr = $"<color={MAX}>{valueStr}</color>";

            string line;
            if (invHas && eqHas)
            {
                string diffStr = ColorizeDiff(invV - eqV);
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {valueStr}  ({diffStr})";
                bothLines.Add(line);
            }
            else if (!invHas && eqHas)
            {
                string baseVal = FormatValue(key, eqV);
                string shownRemoved = (key == "cd") ? baseVal : "-" + baseVal;
                string diffStr = $"<color={NEG}>{shownRemoved}</color> <size=11><color={ZERO}>(기존 옵션)</color></size>";
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {diffStr}";
                removedLines.Add(line);
            }
            else
            {
                string addVal = FormatValue(key, invV);
                string shownAdded = (key == "cd") ? addVal : "+" + addVal;
                string diffStr = $"<color={POS}>{shownAdded}</color> <size=11><color={ZERO}>(새 옵션)</color></size>";
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {diffStr}";
                newLines.Add(line);
            }
        }

        var sb = new StringBuilder();
        foreach (var l in bothLines) sb.AppendLine(l);
        foreach (var l in removedLines) sb.AppendLine(l);
        foreach (var l in newLines) sb.AppendLine(l);

        if (sb.Length == 0) sb.Append("표시할 옵션이 없습니다");
        return sb.ToString();
    }
}
