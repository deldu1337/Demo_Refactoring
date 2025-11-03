//using System;
//using System.Text;
//using UnityEngine;

//public static class ItemStatCompare
//{
//    // UI 색상 (Unity Text 리치텍스트)
//    private const string POS = "#35C759"; // 초록
//    private const string NEG = "#FF3B30"; // 빨강
//    private const string ZERO = "#A1A1A6"; // 회색
//    private const string LABEL = "#DADADA"; // 라벨 텍스트 기본색
//    private const string MAX = "#49DDDF";  // 극옵 하이라이트(원패널과 동일 톤)

//    // inv > eq 이면 초록 +, inv < eq 이면 빨강 -, 0이면 회색 0
//    private static string ColorizeDiff(float diff, bool signed = true)
//    {
//        string sign = diff > 0.0001f ? "+" : (diff < -0.0001f ? "" : "");
//        string color = diff > 0.0001f ? POS : (diff < -0.0001f ? NEG : ZERO);
//        string val = diff.ToString(diff == Mathf.RoundToInt(diff) ? "+0;-#;0" : "+0.##;-0.##;0");
//        if (!signed) val = diff.ToString(diff == Mathf.RoundToInt(diff) ? "0;-#;0" : "0.##;-0.##;0");
//        return $"<color={color}>{(signed ? val : sign + val)}</color>";
//    }

//    private static float Eff(float baseVal, bool hasRolled, float rolledVal)
//        => hasRolled ? rolledVal : baseVal;

//    private static void Take(InventoryItem item, out float hp, out float mp, out float atk, out float def, out float dex, out float AS, out float cc, out float cd)
//    {
//        // rolled가 있으면 has* 플래그를 따라 우선 사용
//        if (item.rolled != null)
//        {
//            hp = Eff(item.data.hp, item.rolled.hasHp, item.rolled.hp);
//            mp = Eff(item.data.mp, item.rolled.hasMp, item.rolled.mp);
//            atk = Eff(item.data.atk, item.rolled.hasAtk, item.rolled.atk);
//            def = Eff(item.data.def, item.rolled.hasDef, item.rolled.def);
//            dex = Eff(item.data.dex, item.rolled.hasDex, item.rolled.dex);
//            AS = Eff(item.data.As, item.rolled.hasAs, item.rolled.As);
//            cc = Eff(item.data.cc, item.rolled.hasCc, item.rolled.cc);
//            cd = Eff(item.data.cd, item.rolled.hasCd, item.rolled.cd);
//        }
//        else
//        {
//            hp = item.data.hp; mp = item.data.mp; atk = item.data.atk; def = item.data.def;
//            dex = item.data.dex; AS = item.data.As; cc = item.data.cc; cd = item.data.cd;
//        }
//    }

//    public static string BuildSingleTooltip(InventoryItem item, string tier = null)
//    {
//        Take(item, out var hp, out var mp, out var atk, out var def, out var dex, out var AS, out var cc, out var cd);

//        string TierLine = string.IsNullOrEmpty(tier) ? "" : $"\n<color=#FFD60A>[{tier}]</color>";
//        return
//$@"<b>{item.data.name}</b>{TierLine}
//<color={LABEL}>종류</color>  {item.data.type}
//<color={LABEL}>레벨</color>  {Mathf.Max(1, item.data.level)}

//<color={LABEL}>HP</color>  {hp}
//<color={LABEL}>MP</color>  {mp}
//<color={LABEL}>ATK</color> {atk}
//<color={LABEL}>DEF</color> {def}
//<color={LABEL}>DEX</color> {dex}
//<color={LABEL}>AS</color>  {AS}
//<color={LABEL}>CC</color>  {cc}
//<color={LABEL}>CD</color>  {cd}";
//    }

//    // inv 아이템이 해당 스탯 "옵션을 갖고 있는지" 판정 (원래 패널과 동일한 철학)
//    private static bool HasOption(InventoryItem item, string key, float baseVal, out float effective)
//    {
//        effective = baseVal;
//        var r = item.rolled;
//        bool hasRolled =
//            r != null && r.TryGet(key, out effective); // 롤링 값 우선

//        // 유효성: 롤링 값이 있으면 그 값, 없으면 base 값이 0이 아닌지로 판단
//        const float EPS = 0.0001f;
//        float v = hasRolled ? effective : baseVal;
//        return Mathf.Abs(v) > EPS;
//    }

//    private static string FormatValue(string key, float v)
//    {
//        if (key == "cc") return $"{v * 100f:0.##}%";
//        if (key == "cd") return $"{v:0.##}";
//        return $"{v:0.##}";
//    }

//    /// <summary>
//    /// 인벤 아이템이 가진 옵션만 출력.
//    /// showEquippedValues=true 면 라벨 옆 값은 "장착(eq) 값"을 표시하고,
//    /// 괄호에는 항상 (인벤 - 장착) 증감색을 표시.
//    /// </summary>
//    public static string BuildCompareLines(InventoryItem inv, InventoryItem eq, bool showEquippedValues)
//    {
//        // 최종 효과값(rolled 반영) 가져오기
//        Take(inv, out var hp1, out var mp1, out var atk1, out var def1, out var dex1, out var AS1, out var cc1, out var cd1);
//        Take(eq, out var hp0, out var mp0, out var atk0, out var def0, out var dex0, out var AS0, out var cc0, out var cd0);

//        string Line(string label, string key, float invV, float eqV)
//        {
//            float diff = invV - eqV;                         // (인벤 - 장착)
//            float shown = showEquippedValues ? eqV : invV;   // 표시값 선택

//            // 극옵 판정은 "표시 주체" 아이템에서 수행 (원패널과 동일한 철학)
//            InventoryItem shownItem = showEquippedValues ? eq : inv;
//            bool isMax = false;
//            if (shownItem != null && shownItem.data != null && shownItem.data.type != "potion")
//            {
//                // key와 shown값으로 극옵 판정
//                isMax = ItemRoller.IsMaxRoll(shownItem.id, key, shown);
//            }

//            string valueStr = FormatValue(key, shown);
//            if (isMax) valueStr = $"<color={MAX}>{valueStr}</color>";

//            return $"<color={LABEL}>{label}</color>  {valueStr}  ({ColorizeDiff(diff)})";
//        }

//        var sb = new StringBuilder();
//        float _;
//        if (HasOption(inv, "hp", inv.data.hp, out _)) sb.AppendLine(Line("HP", "hp", hp1, hp0));
//        if (HasOption(inv, "mp", inv.data.mp, out _)) sb.AppendLine(Line("MP", "mp", mp1, mp0));
//        if (HasOption(inv, "atk", inv.data.atk, out _)) sb.AppendLine(Line("데미지", "atk", atk1, atk0));
//        if (HasOption(inv, "def", inv.data.def, out _)) sb.AppendLine(Line("방어력", "def", def1, def0));
//        if (HasOption(inv, "dex", inv.data.dex, out _)) sb.AppendLine(Line("민첩성", "dex", dex1, dex0));
//        if (HasOption(inv, "As", inv.data.As, out _)) sb.AppendLine(Line("공격 속도", "As", AS1, AS0));
//        if (HasOption(inv, "cc", inv.data.cc, out _)) sb.AppendLine(Line("치명타 확률", "cc", cc1, cc0));
//        if (HasOption(inv, "cd", inv.data.cd, out _)) sb.AppendLine(Line("치명타 데미지", "cd", cd1, cd0));

//        if (sb.Length == 0) sb.Append("표시할 옵션 없음");
//        return sb.ToString();
//    }
//}
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

    private static string FormatValue(string key, float v)
    {
        if (key == "cc") return $"{v * 100f:0.##}%";
        if (key == "cd") return $"x{v:0.##}";   // ★ cd는 x 접두사
        return $"{v:0.##}";
    }

    private static string ColorizeDiff(float diff)
    {
        string color = diff > EPS ? POS : (diff < -EPS ? NEG : ZERO);
        string val = diff.ToString(diff == Mathf.RoundToInt(diff) ? "+0;-#;0" : "+0.##;-0.##;0");
        return $"<color={color}>{val}</color>";
    }

    private static float Eff(float baseVal, bool hasRolled, float rolledVal)
        => hasRolled ? rolledVal : baseVal;

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

    // ★ 아이템이 실제로 가지고 있는(0이 아닌) 옵션만 딕셔너리로 수집
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

    public static string BuildCompareLines(InventoryItem inv, InventoryItem eq, bool showEquippedValues)
    {
        bool invGem = inv?.data?.type != null && inv.data.type.Equals("gem", StringComparison.OrdinalIgnoreCase);
        bool eqGem = eq?.data?.type != null && eq.data.type.Equals("gem", StringComparison.OrdinalIgnoreCase);
        bool isGemMode = invGem || eqGem;

        var invStats = GatherNonZeroStats(inv);
        var eqStats = GatherNonZeroStats(eq);

        // 키 정렬(보기 좋게 고정 순서)
        string[] ORDER = { "hp", "mp", "atk", "def", "dex", "As", "cc", "cd" };
        System.Func<string, int> idx = k => {
            int i = System.Array.IndexOf(ORDER, k);
            return i < 0 ? 999 : i;
        };

        var keySet = isGemMode
            ? new System.Collections.Generic.HashSet<string>(invStats.Keys)
            : new System.Collections.Generic.HashSet<string>(invStats.Keys);
        if (isGemMode) foreach (var k in eqStats.Keys) keySet.Add(k);

        var keys = new System.Collections.Generic.List<string>(keySet);
        keys.Sort((a, b) => {
            int ia = idx(a), ib = idx(b);
            if (ia != ib) return ia.CompareTo(ib);
            return string.Compare(a, b, System.StringComparison.Ordinal);
        });

        // 라인 버킷
        var bothLines = new System.Collections.Generic.List<string>(); // 둘 다 옵션 O
        var removedLines = new System.Collections.Generic.List<string>(); // 장착만 O → (사라짐)
        var newLines = new System.Collections.Generic.List<string>(); // 인벤만 O → (새 옵션)

        foreach (var key in keys)
        {
            invStats.TryGetValue(key, out var invV);
            eqStats.TryGetValue(key, out var eqV);
            bool invHas = invStats.ContainsKey(key);
            bool eqHas = eqStats.ContainsKey(key);

            // 표기값: 한쪽만 있을 때도 0이 아니라 가진 쪽의 값으로
            float shown = showEquippedValues
                ? (eqHas ? eqV : invV)
                : (invHas ? invV : eqV);

            // 극옵 하이라이트는 표기 주체가 해당 키를 실제로 가질 때만
            bool isMax = false;
            if (inv != null && eq != null)
            {
                var shownItem = (showEquippedValues ? (eqHas ? eq : inv) : (invHas ? inv : eq));
                if (shownItem != null && shownItem.data != null &&
                    !string.Equals(shownItem.data.type, "potion", System.StringComparison.OrdinalIgnoreCase))
                {
                    isMax = ItemRoller.IsMaxRoll(shownItem.id, key, shown);
                }
            }

            string valueStr = FormatValue(key, shown);
            if (isMax) valueStr = $"<color={MAX}>{valueStr}</color>";

            string line;
            if (invHas && eqHas)
            {
                // 정상 ± 비교
                string diffStr = ColorizeDiff(invV - eqV);
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {valueStr}  ({diffStr})";
                bothLines.Add(line);
            }
            else if (!invHas && eqHas)
            {
                // 사라짐 (장착에만 있던 옵션)
                string baseVal = FormatValue(key, eqV);

                // cd는 - 붙이지 않고 x표기 그대로, 그 외는 - 접두사
                string shownRemoved = (key == "cd") ? baseVal : "-" + baseVal;

                string diffStr = $"<color={NEG}>{shownRemoved}</color> <size=11><color={ZERO}>(기존 옵션)</color></size>";
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {diffStr}";
                removedLines.Add(line);
            }
            else // invHas && !eqHas
            {
                // 새 옵션 (인벤에만 있는 옵션)
                string addVal = FormatValue(key, invV);

                // cd는 + 붙이지 않고 x표기 그대로, 그 외는 + 접두사
                string shownAdded = (key == "cd") ? addVal : "+" + addVal;

                string diffStr = $"<color={POS}>{shownAdded}</color> <size=11><color={ZERO}>(새 옵션)</color></size>";
                line = $"<color={LABEL}>{LabelFor(key)}</color>  {diffStr}";
                newLines.Add(line);
            }

        }

        // 출력 순서: 기존 공통 → ★사라짐 → 새 옵션 (요청사항)
        var sb = new StringBuilder();
        foreach (var l in bothLines) sb.AppendLine(l);
        foreach (var l in removedLines) sb.AppendLine(l);  // ← 사라짐을 새 옵션보다 위로
        foreach (var l in newLines) sb.AppendLine(l);

        if (sb.Length == 0) sb.Append("표시할 옵션 없음");
        return sb.ToString();
    }
}
