using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public static class MasterDataExcelImporter
{
    private const string ExcelPath = "Assets/Data/Excel/MasterData.xlsx";
    private const string SkillAssetPath = "Assets/Resources/DataAssets/SkillDatabase.asset";
    private const string EnemyAssetPath = "Assets/Resources/DataAssets/EnemyDatabase.asset";
    private const string ItemAssetPath = "Assets/Resources/DataAssets/ItemDatabase.asset";

    [MenuItem("Tools/Data/Import Master Data From Excel")]
    public static void Import()
    {
        if (!File.Exists(ExcelPath))
        {
            Debug.LogError($"[MasterDataExcelImporter] Excel file not found: {ExcelPath}");
            return;
        }

        Dictionary<string, List<Dictionary<string, string>>> workbook = SimpleXlsxReader.ReadWorkbook(ExcelPath);

        ImportSkills(workbook);
        ImportEnemies(workbook);
        ImportItems(workbook);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MasterDataExcelImporter] Master data import completed.");
    }

    private static void ImportSkills(Dictionary<string, List<Dictionary<string, string>>> workbook)
    {
        SkillDatabaseAsset asset = LoadOrCreate<SkillDatabaseAsset>(SkillAssetPath);
        List<Dictionary<string, string>> rows = GetRows(workbook, "Skills");

        SkillData[] ToSkills(string className)
        {
            return rows
                .Where(row => Get(row, "class") == className)
                .Select(row => new SkillData
                {
                    id = Get(row, "id"),
                    name = Get(row, "name"),
                    cooldown = GetFloat(row, "cooldown"),
                    damage = GetFloat(row, "damage"),
                    mpCost = GetFloat(row, "mpCost"),
                    range = GetFloat(row, "range"),
                    impactDelay = GetFloat(row, "impactDelay"),
                    type = Get(row, "type"),
                    animation = Get(row, "animation")
                })
                .ToArray();
        }

        asset.warrior = ToSkills("warrior");
        asset.mage = ToSkills("mage");
        asset.rogue = ToSkills("rogue");
        EditorUtility.SetDirty(asset);
    }

    private static void ImportEnemies(Dictionary<string, List<Dictionary<string, string>>> workbook)
    {
        EnemyDatabaseAsset asset = LoadOrCreate<EnemyDatabaseAsset>(EnemyAssetPath);
        asset.enemies = GetRows(workbook, "Enemies")
            .Select(row => new EnemyData
            {
                id = Get(row, "id"),
                name = Get(row, "name"),
                hp = GetFloat(row, "hp"),
                atk = GetFloat(row, "atk"),
                def = GetFloat(row, "def"),
                dex = GetFloat(row, "dex"),
                As = GetFloat(row, "As"),
                exp = GetFloat(row, "exp"),
                unlockStage = GetInt(row, "unlockStage", 1),
                isBoss = GetBool(row, "isBoss"),
                weight = GetFloat(row, "weight", 1f),
                minStage = GetInt(row, "minStage"),
                maxStage = GetInt(row, "maxStage")
            })
            .ToArray();
        EditorUtility.SetDirty(asset);
    }

    private static void ImportItems(Dictionary<string, List<Dictionary<string, string>>> workbook)
    {
        ItemDatabaseAsset asset = LoadOrCreate<ItemDatabaseAsset>(ItemAssetPath);
        asset.items = GetRows(workbook, "Items")
            .Select(row => new ItemData
            {
                id = GetInt(row, "id"),
                name = Get(row, "name"),
                uniqueName = Get(row, "uniqueName"),
                tier = Get(row, "tier"),
                level = GetInt(row, "level"),
                hp = GetFloat(row, "hp"),
                mp = GetFloat(row, "mp"),
                atk = GetFloat(row, "atk"),
                def = GetFloat(row, "def"),
                dex = GetFloat(row, "dex"),
                As = GetFloat(row, "As"),
                cc = GetFloat(row, "cc"),
                cd = GetFloat(row, "cd"),
                type = Get(row, "type")
            })
            .ToArray();

        asset.itemRanges = GetRows(workbook, "ItemRanges")
            .Select(row => new ItemRangeEntry
            {
                id = GetInt(row, "id"),
                hp = GetRange(row, "hp"),
                mp = GetRange(row, "mp"),
                atk = GetRange(row, "atk"),
                def = GetRange(row, "def"),
                dex = GetRange(row, "dex"),
                As = GetRange(row, "As"),
                cc = GetRange(row, "cc"),
                cd = GetRange(row, "cd")
            })
            .ToArray();

        EditorUtility.SetDirty(asset);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static List<Dictionary<string, string>> GetRows(Dictionary<string, List<Dictionary<string, string>>> workbook, string sheetName)
    {
        return workbook.TryGetValue(sheetName, out List<Dictionary<string, string>> rows)
            ? rows
            : new List<Dictionary<string, string>>();
    }

    private static string Get(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        return row.TryGetValue(key, out string value) ? value : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> row, string key, int defaultValue = 0)
    {
        return int.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : defaultValue;
    }

    private static float GetFloat(Dictionary<string, string> row, string key, float defaultValue = 0f)
    {
        return float.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : defaultValue;
    }

    private static bool GetBool(Dictionary<string, string> row, string key)
    {
        string value = Get(row, key).Trim();
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static ItemStatRange GetRange(Dictionary<string, string> row, string stat)
    {
        string minKey = $"{stat}Min";
        string maxKey = $"{stat}Max";
        if (!row.ContainsKey(minKey) && !row.ContainsKey(maxKey))
            return null;

        string min = Get(row, minKey);
        string max = Get(row, maxKey);
        if (string.IsNullOrWhiteSpace(min) && string.IsNullOrWhiteSpace(max))
            return null;

        return new ItemStatRange
        {
            min = GetFloat(row, minKey),
            max = GetFloat(row, maxKey)
        };
    }
}

internal static class SimpleXlsxReader
{
    private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static Dictionary<string, List<Dictionary<string, string>>> ReadWorkbook(string path)
    {
        using ZipArchive archive = ZipFile.OpenRead(path);
        List<string> sharedStrings = ReadSharedStrings(archive);
        Dictionary<string, string> sheetTargets = ReadSheetTargets(archive);
        Dictionary<string, List<Dictionary<string, string>>> result = new();

        ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
        if (workbookEntry == null)
            return result;

        XDocument workbook = LoadXml(workbookEntry);
        foreach (XElement sheet in workbook.Descendants(MainNs + "sheet"))
        {
            string name = (string)sheet.Attribute("name");
            string relId = (string)sheet.Attribute(RelNs + "id");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relId))
                continue;

            if (!sheetTargets.TryGetValue(relId, out string target))
                continue;

            string entryPath = "xl/" + target.Replace("\\", "/").TrimStart('/');
            ZipArchiveEntry sheetEntry = archive.GetEntry(entryPath);
            if (sheetEntry == null)
                continue;

            result[name] = ReadSheet(sheetEntry, sharedStrings);
        }

        return result;
    }

    private static List<Dictionary<string, string>> ReadSheet(ZipArchiveEntry sheetEntry, List<string> sharedStrings)
    {
        XDocument sheetDoc = LoadXml(sheetEntry);
        List<List<string>> rows = new();

        foreach (XElement row in sheetDoc.Descendants(MainNs + "row"))
        {
            List<string> values = new();
            int currentColumn = 0;

            foreach (XElement cell in row.Elements(MainNs + "c"))
            {
                string reference = (string)cell.Attribute("r");
                int columnIndex = GetColumnIndex(reference);
                while (currentColumn < columnIndex)
                {
                    values.Add(string.Empty);
                    currentColumn++;
                }

                values.Add(ReadCellValue(cell, sharedStrings));
                currentColumn++;
            }

            if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
                rows.Add(values);
        }

        if (rows.Count == 0)
            return new List<Dictionary<string, string>>();

        List<string> headers = rows[0].Select(header => header.Trim()).ToList();
        List<Dictionary<string, string>> result = new();

        for (int r = 1; r < rows.Count; r++)
        {
            Dictionary<string, string> rowMap = new(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < headers.Count && c < rows[r].Count; c++)
            {
                if (!string.IsNullOrWhiteSpace(headers[c]))
                    rowMap[headers[c]] = rows[r][c];
            }

            if (rowMap.Count > 0)
                result.Add(rowMap);
        }

        return result;
    }

    private static Dictionary<string, string> ReadSheetTargets(ZipArchive archive)
    {
        ZipArchiveEntry relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        Dictionary<string, string> targets = new();
        if (relsEntry == null)
            return targets;

        XDocument rels = LoadXml(relsEntry);
        foreach (XElement rel in rels.Descendants(PackageRelNs + "Relationship"))
        {
            string id = (string)rel.Attribute("Id");
            string target = (string)rel.Attribute("Target");
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                targets[id] = target;
        }

        return targets;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return new List<string>();

        XDocument doc = LoadXml(entry);
        return doc.Descendants(MainNs + "si")
            .Select(si => string.Concat(si.Descendants(MainNs + "t").Select(t => t.Value)))
            .ToList();
    }

    private static string ReadCellValue(XElement cell, List<string> sharedStrings)
    {
        string type = (string)cell.Attribute("t");
        XElement valueElement = cell.Element(MainNs + "v");

        if (type == "inlineStr")
            return string.Concat(cell.Descendants(MainNs + "t").Select(t => t.Value));

        if (valueElement == null)
            return string.Empty;

        string value = valueElement.Value;
        if (type == "s" && int.TryParse(value, out int sharedStringIndex) &&
            sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
            return sharedStrings[sharedStringIndex];

        return value;
    }

    private static int GetColumnIndex(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return 0;

        int index = 0;
        foreach (char ch in reference)
        {
            if (!char.IsLetter(ch))
                break;

            index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return Math.Max(0, index - 1);
    }

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        return XDocument.Load(stream);
    }
}
