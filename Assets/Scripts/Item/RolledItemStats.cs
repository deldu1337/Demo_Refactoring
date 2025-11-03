using System;
using UnityEngine;

[Serializable]
public class RolledItemStats
{
    public float hp, mp, atk, def, dex, As, cc, cd;
    public bool hasHp, hasMp, hasAtk, hasDef, hasDex, hasAs, hasCc, hasCd;

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
                Debug.LogWarning($"[RolledItemStats] Unknown stat: {stat}");
                break;
        }
    }

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
