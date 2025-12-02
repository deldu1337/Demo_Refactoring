using System;
using UnityEngine;

[Serializable]
public class RolledItemStats
{
    public float hp, mp, atk, def, dex, As, cc, cd;
    public bool hasHp, hasMp, hasAtk, hasDef, hasDex, hasAs, hasCc, hasCd;

    /// <summary>
    /// 지정된 스탯 키에 값을 설정하고 보유 여부를 표시합니다.
    /// </summary>
    /// <param name="stat">설정할 스탯 키입니다.</param>
    /// <param name="value">저장할 값입니다.</param>
    public void Set(string stat, float value)
    {
        switch (stat)
        {
            case "hp": hp = value; hasHp = true; break;
            case "mp": mp = value; hasMp = true; break;
            case "atk": atk = value; hasAtk = true; break;
            case "def": def = value; hasDef = true; break;
            case "dex": dex = value; hasDex = true; break;
            case "As": As = value; hasAs = true; break;
            case "cc": cc = value; hasCc = true; break;
            case "cd": cd = value; hasCd = true; break;
            default:
                Debug.LogWarning($"[RolledItemStats] 알 수 없는 스탯입니다: {stat}");
                break;
        }
    }

    /// <summary>
    /// 요청한 스탯 값을 반환하며 보유 여부를 알려 줍니다.
    /// </summary>
    /// <param name="stat">조회할 스탯 키입니다.</param>
    /// <param name="value">반환할 값을 담을 변수입니다.</param>
    /// <returns>스탯이 설정되어 있을 때 참을 반환합니다.</returns>
    public bool TryGet(string stat, out float value)
    {
        switch (stat)
        {
            case "hp": value = hp; return hasHp;
            case "mp": value = mp; return hasMp;
            case "atk": value = atk; return hasAtk;
            case "def": value = def; return hasDef;
            case "dex": value = dex; return hasDex;
            case "As": value = As; return hasAs;
            case "cc": value = cc; return hasCc;
            case "cd": value = cd; return hasCd;
        }
        value = 0f; return false;
    }
}
