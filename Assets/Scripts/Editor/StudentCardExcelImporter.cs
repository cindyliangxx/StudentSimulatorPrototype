using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class StudentCardExcelImporter : EditorWindow
{
    private const string DefaultStyleListPath = "Assets/Kings/cards/_templates/CardStyle_List.asset";
    private const string DefaultOutputFolder = "Assets/StudentSimulator/Cards";
    private const string DefaultImageAssetFolder = "Assets/StudentSimulator/CardImages";
    private const string DefaultGroupName = "Student";
    private const string DefaultStyleName = "cs_default";

    private readonly string[] requiredHeaders =
    {
        "GroupName",
        "CardName",
        "StyleName",
        "EventScript.titleText",
        "EventScript.questionText",
        "EventScript.answerLeft",
        "EventScript.answerRight"
    };

    private string excelPath = "";
    private string sourceImageFolder = "";
    private string outputFolder = DefaultOutputFolder;
    private string imageAssetFolder = DefaultImageAssetFolder;
    private KingsCardStyleList styleDefinitions;
    private int importLimit = 10;
    private bool overwriteExistingCards = true;
    private Vector2 scroll;
    private string lastReport = "";

    [MenuItem("Tools/Student Simulator/Card Excel Importer")]
    public static void Open()
    {
        GetWindow<StudentCardExcelImporter>("Student Card Importer");
    }

    public static string ImportForAutomation(string tablePath, int limit)
    {
        var importer = CreateInstance<StudentCardExcelImporter>();
        importer.excelPath = tablePath;
        importer.sourceImageFolder = "";
        importer.outputFolder = DefaultOutputFolder;
        importer.imageAssetFolder = DefaultImageAssetFolder;
        importer.styleDefinitions = AssetDatabase.LoadAssetAtPath<KingsCardStyleList>(DefaultStyleListPath);
        importer.importLimit = limit;
        importer.overwriteExistingCards = true;
        importer.ValidateAndMaybeImport(true);
        var report = importer.lastReport;
        DestroyImmediate(importer);
        return report;
    }

    private void OnEnable()
    {
        if (styleDefinitions == null)
        {
            styleDefinitions = AssetDatabase.LoadAssetAtPath<KingsCardStyleList>(DefaultStyleListPath);
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Student Simulator Card Excel Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Imports Excel rows into existing Kings-compatible card prefabs. Runtime code is not changed.", MessageType.Info);

        DrawFilePicker("Card Table File", ref excelPath, "xlsx,csv");
        DrawFolderPicker("Source Image Folder", ref sourceImageFolder);

        styleDefinitions = (KingsCardStyleList)EditorGUILayout.ObjectField("Style Definitions", styleDefinitions, typeof(KingsCardStyleList), false);
        outputFolder = EditorGUILayout.TextField("Card Prefab Output", outputFolder);
        imageAssetFolder = EditorGUILayout.TextField("Image Asset Output", imageAssetFolder);
        importLimit = EditorGUILayout.IntField("Import Limit", importLimit);
        overwriteExistingCards = EditorGUILayout.Toggle("Overwrite Existing Cards", overwriteExistingCards);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Only"))
        {
            ValidateAndMaybeImport(false);
        }
        if (GUILayout.Button("Import Cards"))
        {
            ValidateAndMaybeImport(true);
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(lastReport))
        {
            EditorGUILayout.Space();
            EditorGUILayout.TextArea(lastReport, GUILayout.MinHeight(180));
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawFilePicker(string label, ref string value, string extension)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            var selected = EditorUtility.OpenFilePanel(label, "", extension);
            if (!string.IsNullOrEmpty(selected))
            {
                value = selected;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawFolderPicker(string label, ref string value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            var selected = EditorUtility.OpenFolderPanel(label, "", "");
            if (!string.IsNullOrEmpty(selected))
            {
                value = selected;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ValidateAndMaybeImport(bool executeImport)
    {
        var report = new List<string>();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
        {
            errors.Add("Excel file does not exist.");
        }

        if (styleDefinitions == null)
        {
            errors.Add("Style Definitions is missing.");
        }

        if (importLimit <= 0)
        {
            errors.Add("Import Limit must be greater than 0.");
        }

        if (errors.Count == 0)
        {
            try
            {
                var rows = StudentCardSheetReader.Read(excelPath);
                ValidateRows(rows, errors, warnings, out var cardRows);

                report.Add("Rows found: " + cardRows.Count);
                report.Add("Import limit: " + importLimit);

                if (errors.Count == 0 && executeImport)
                {
                    var imported = ImportRows(cardRows.Take(importLimit).ToList(), warnings);
                    report.Add("Imported cards: " + imported);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.GetType().Name + ": " + ex.Message);
            }
        }

        if (errors.Count > 0)
        {
            report.Add("Errors:");
            report.AddRange(errors.Select(e => "- " + e));
        }

        if (warnings.Count > 0)
        {
            report.Add("Warnings:");
            report.AddRange(warnings.Select(w => "- " + w));
        }

        if (errors.Count == 0 && !executeImport)
        {
            report.Add("Validation passed. No files were written.");
        }

        lastReport = string.Join(Environment.NewLine, report);
        Debug.Log(lastReport);
    }

    private void ValidateRows(List<Dictionary<string, string>> rows, List<string> errors, List<string> warnings, out List<Dictionary<string, string>> cardRows)
    {
        cardRows = rows.Where(r => r.Values.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();
        if (cardRows.Count == 0)
        {
            errors.Add("Excel sheet has no card rows.");
            return;
        }

        var headers = new HashSet<string>(cardRows[0].Keys);
        foreach (var header in requiredHeaders)
        {
            if (!headers.Contains(header))
            {
                errors.Add("Missing required header: " + header);
            }
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < cardRows.Count; i++)
        {
            var rowNumber = i + 2;
            var row = cardRows[i];
            var cardName = Get(row, "CardName");
            if (string.IsNullOrWhiteSpace(cardName))
            {
                errors.Add("Row " + rowNumber + ": CardName is required.");
            }
            else if (!names.Add(cardName))
            {
                errors.Add("Row " + rowNumber + ": duplicated CardName '" + cardName + "'.");
            }

            var styleName = GetOrDefault(row, "StyleName", DefaultStyleName);
            if (styleDefinitions != null && !styleDefinitions.HasStyle(styleName))
            {
                errors.Add("Row " + rowNumber + ": unknown StyleName '" + styleName + "'.");
            }

            ValidateNumber(row, rowNumber, "EventScript.cardPropability", 1f, errors, 0f, 1f);
            ValidateInt(row, rowNumber, "EventScript.redrawBlockCnt", 0, errors, 0);
            ValidateInt(row, rowNumber, "EventScript.maxDraws", 100, errors, 1);

            foreach (var valueHeader in StatHeaders())
            {
                var raw = Get(row, valueHeader);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (!TryParseFloat(raw, out var parsed) || !IsAllowedTier(parsed))
                {
                    errors.Add("Row " + rowNumber + ": " + valueHeader + " must be one of -15, -10, -5, 0, 5, 10, 15.");
                }
            }

            var imageFile = Get(row, "ImageFile");
            if (!string.IsNullOrWhiteSpace(imageFile) && string.IsNullOrWhiteSpace(sourceImageFolder) && !imageFile.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("Row " + rowNumber + ": ImageFile is set but Source Image Folder is empty.");
            }
        }
    }

    private int ImportRows(List<Dictionary<string, string>> rows, List<string> warnings)
    {
        EnsureAssetFolder(outputFolder);
        EnsureAssetFolder(imageAssetFolder);

        var imported = 0;
        foreach (var row in rows)
        {
            var groupName = SanitizePathPart(GetOrDefault(row, "GroupName", DefaultGroupName));
            var cardName = SanitizePathPart(Get(row, "CardName"));
            var styleName = GetOrDefault(row, "StyleName", DefaultStyleName);
            var style = styleDefinitions.GetStyle(styleName);
            var groupFolder = CombineAssetPath(outputFolder, groupName);
            EnsureAssetFolder(groupFolder);

            var prefabPath = CombineAssetPath(groupFolder, cardName + ".prefab");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null && !overwriteExistingCards)
            {
                warnings.Add("Skipped existing card: " + prefabPath);
                continue;
            }

            var sourcePrefab = existing != null ? existing : style.prefab;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            instance.name = cardName;

            ApplyCardStyle(instance, style);
            ApplyEventScript(instance, row);
            ApplyImage(instance, row, warnings);

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);
            imported++;
        }

        return imported;
    }

    private static void ApplyCardStyle(GameObject instance, KingsCardStyle style)
    {
        var cardStyle = instance.GetComponent<CardStyle>();
        if (cardStyle != null)
        {
            cardStyle.SetStyle(style);
            cardStyle.Refresh();
        }
    }

    private static void ApplyEventScript(GameObject instance, Dictionary<string, string> row)
    {
        var eventScript = instance.GetComponent<EventScript>();
        if (eventScript == null)
        {
            throw new InvalidOperationException("Prefab '" + instance.name + "' has no EventScript.");
        }

        SetEventText(eventScript.textFields.titleText, Get(row, "EventScript.titleText"));
        SetEventText(eventScript.textFields.questionText, Get(row, "EventScript.questionText"));
        SetEventText(eventScript.textFields.answerLeft, Get(row, "EventScript.answerLeft"));
        SetEventText(eventScript.textFields.answerRight, Get(row, "EventScript.answerRight"));
        SetEventText(eventScript.textFields.answerUp, "");
        SetEventText(eventScript.textFields.answerDown, "");

        eventScript.cardPropability = GetFloat(row, "EventScript.cardPropability", 1f);
        eventScript.redrawBlockCnt = GetInt(row, "EventScript.redrawBlockCnt", 0);
        eventScript.maxDraws = GetBool(row, "StudentCard.unique", false) ? 1 : GetInt(row, "EventScript.maxDraws", 100);
        eventScript.isDrawable = true;
        eventScript.swipeType = EventScript.E_SwipeType.LeftRight;

        eventScript.Results.resultLeft = BuildSimpleResult(row, "Left");
        eventScript.Results.resultRight = BuildSimpleResult(row, "Right");
    }

    private static EventScript.result BuildSimpleResult(Dictionary<string, string> row, string prefix)
    {
        var result = new EventScript.result
        {
            resultType = EventScript.resultTypes.simple,
            modifiers = BuildModifierGroup(row, prefix),
            conditions = Array.Empty<EventScript.condition>(),
            modifiersTrue = EmptyModifierGroup(),
            modifiersFalse = EmptyModifierGroup(),
            randomModifiers = Array.Empty<EventScript.modifierGroup>()
        };

        return result;
    }

    private static EventScript.modifierGroup BuildModifierGroup(Dictionary<string, string> row, string prefix)
    {
        var changes = new List<EventScript.resultModifier>();
        AddStatModifier(changes, row, prefix + "_身心", valueDefinitions.values.army);
        AddStatModifier(changes, row, prefix + "_学业", valueDefinitions.values.religion);
        AddStatModifier(changes, row, prefix + "_人际", valueDefinitions.values.people);
        AddStatModifier(changes, row, prefix + "_经济", valueDefinitions.values.money);

        var group = EmptyModifierGroup();
        group.valueChanges = changes.ToArray();
        return group;
    }

    private static EventScript.modifierGroup EmptyModifierGroup()
    {
        return new EventScript.modifierGroup
        {
            valueChanges = Array.Empty<EventScript.resultModifier>(),
            extras = Array.Empty<EventScript.C_AdditionalModifiers>(),
            followUpCard = null,
            followUpDelay = new EventScript.C_intRange { min = 0, max = 0 }
        };
    }

    private static void AddStatModifier(List<EventScript.resultModifier> changes, Dictionary<string, string> row, string header, valueDefinitions.values target)
    {
        var amount = GetFloat(row, header, 0f);
        if (Mathf.Approximately(amount, 0f))
        {
            return;
        }

        changes.Add(new EventScript.resultModifier
        {
            modificationType = EventScript.E_ModificationType.add,
            modifier = target,
            valueAdd = amount,
            valueSet = 0f,
            rndRangeAdd = new EventScript.C_RndRange(),
            rndRangeSet = new EventScript.C_RndRange()
        });
    }

    private void ApplyImage(GameObject instance, Dictionary<string, string> row, List<string> warnings)
    {
        var imageFile = Get(row, "ImageFile");
        if (string.IsNullOrWhiteSpace(imageFile))
        {
            return;
        }

        var imageAssetPath = ResolveImageAsset(imageFile, warnings);
        if (string.IsNullOrEmpty(imageAssetPath))
        {
            return;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imageAssetPath);
        if (sprite == null)
        {
            warnings.Add("Could not load sprite: " + imageAssetPath);
            return;
        }

        var icon = FindChild(instance.transform, "Icon");
        if (icon == null)
        {
            warnings.Add("Card '" + instance.name + "' has no Icon child.");
            return;
        }

        var image = icon.GetComponent<Image>();
        if (image == null)
        {
            warnings.Add("Card '" + instance.name + "' Icon has no Image component.");
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private string ResolveImageAsset(string imageFile, List<string> warnings)
    {
        if (imageFile.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureSpriteImporter(imageFile);
            return imageFile;
        }

        if (string.IsNullOrWhiteSpace(sourceImageFolder))
        {
            warnings.Add("Source Image Folder is empty for image: " + imageFile);
            return "";
        }

        var sourcePath = Path.Combine(sourceImageFolder, imageFile);
        if (!File.Exists(sourcePath))
        {
            warnings.Add("Image file not found: " + sourcePath);
            return "";
        }

        EnsureAssetFolder(imageAssetFolder);
        var destinationPath = CombineAssetPath(imageAssetFolder, Path.GetFileName(imageFile));
        File.Copy(sourcePath, ToAbsoluteProjectPath(destinationPath), true);
        AssetDatabase.ImportAsset(destinationPath);
        ConfigureSpriteImporter(destinationPath);
        AssetDatabase.ImportAsset(destinationPath);
        return destinationPath;
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void SetEventText(EventScript.eventText eventText, string value)
    {
        eventText.textContent = value ?? "";
        if (eventText.textField != null)
        {
            eventText.textField.text = eventText.textContent;
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static IEnumerable<string> StatHeaders()
    {
        yield return "Left_身心";
        yield return "Left_学业";
        yield return "Left_人际";
        yield return "Left_经济";
        yield return "Right_身心";
        yield return "Right_学业";
        yield return "Right_人际";
        yield return "Right_经济";
    }

    private static void ValidateNumber(Dictionary<string, string> row, int rowNumber, string header, float defaultValue, List<string> errors, float min, float max)
    {
        var raw = Get(row, header);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        if (!TryParseFloat(raw, out var value) || value < min || value > max)
        {
            errors.Add("Row " + rowNumber + ": " + header + " must be between " + min + " and " + max + ". Default is " + defaultValue + ".");
        }
    }

    private static void ValidateInt(Dictionary<string, string> row, int rowNumber, string header, int defaultValue, List<string> errors, int min)
    {
        var raw = Get(row, header);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < min)
        {
            errors.Add("Row " + rowNumber + ": " + header + " must be an integer >= " + min + ". Default is " + defaultValue + ".");
        }
    }

    private static bool IsAllowedTier(float value)
    {
        var rounded = Mathf.RoundToInt(value);
        return Mathf.Approximately(value, rounded) && (rounded == -15 || rounded == -10 || rounded == -5 || rounded == 0 || rounded == 5 || rounded == 10 || rounded == 15);
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value.Trim() : "";
    }

    private static string GetOrDefault(Dictionary<string, string> row, string key, string defaultValue)
    {
        var value = Get(row, key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static float GetFloat(Dictionary<string, string> row, string key, float defaultValue)
    {
        var raw = Get(row, key);
        return TryParseFloat(raw, out var value) ? value : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> row, string key, int defaultValue)
    {
        var raw = Get(row, key);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
    }

    private static bool GetBool(Dictionary<string, string> row, string key, bool defaultValue)
    {
        var raw = Get(row, key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Equals("yes", StringComparison.OrdinalIgnoreCase) || raw == "1" || raw == "是";
    }

    private static bool TryParseFloat(string raw, out float value)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string SanitizePathPart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrEmpty(sanitized) ? "Unnamed" : sanitized;
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        var parts = assetFolder.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static string CombineAssetPath(string left, string right)
    {
        return (left.TrimEnd('/') + "/" + right.TrimStart('/')).Replace("\\", "/");
    }

    private static string ToAbsoluteProjectPath(string assetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    private static class XlsxReader
    {
        public static List<Dictionary<string, string>> ReadFirstSheet(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry == null)
                {
                    throw new InvalidOperationException("Could not find first worksheet in xlsx.");
                }

                var rows = ReadSheetRows(sheetEntry, sharedStrings);
                if (rows.Count == 0)
                {
                    return new List<Dictionary<string, string>>();
                }

                var headers = rows[0];
                var result = new List<Dictionary<string, string>>();
                for (var r = 1; r < rows.Count; r++)
                {
                    var dict = new Dictionary<string, string>();
                    for (var c = 0; c < headers.Count; c++)
                    {
                        var header = headers[c];
                        if (string.IsNullOrWhiteSpace(header))
                        {
                            continue;
                        }
                        dict[header] = c < rows[r].Count ? rows[r][c] : "";
                    }
                    result.Add(dict);
                }

                return result;
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            var result = new List<string>();
            if (entry == null)
            {
                return result;
            }

            var doc = LoadXml(entry);
            var namespaceManager = CreateNamespaceManager(doc);
            foreach (XmlNode node in doc.SelectNodes("//x:si", namespaceManager))
            {
                var texts = node.SelectNodes(".//x:t", namespaceManager);
                result.Add(string.Concat(texts.Cast<XmlNode>().Select(n => n.InnerText)));
            }

            return result;
        }

        private static List<List<string>> ReadSheetRows(ZipArchiveEntry entry, List<string> sharedStrings)
        {
            var doc = LoadXml(entry);
            var namespaceManager = CreateNamespaceManager(doc);
            var output = new List<List<string>>();

            foreach (XmlNode rowNode in doc.SelectNodes("//x:sheetData/x:row", namespaceManager))
            {
                var row = new List<string>();
                foreach (XmlNode cellNode in rowNode.SelectNodes("x:c", namespaceManager))
                {
                    var cellReference = cellNode.Attributes["r"] != null ? cellNode.Attributes["r"].Value : "";
                    var columnIndex = GetColumnIndex(cellReference);
                    while (row.Count <= columnIndex)
                    {
                        row.Add("");
                    }

                    row[columnIndex] = ReadCellValue(cellNode, sharedStrings, namespaceManager);
                }
                output.Add(row);
            }

            return output;
        }

        private static string ReadCellValue(XmlNode cellNode, List<string> sharedStrings, XmlNamespaceManager namespaceManager)
        {
            var type = cellNode.Attributes["t"] != null ? cellNode.Attributes["t"].Value : "";
            if (type == "inlineStr")
            {
                var inlineText = cellNode.SelectSingleNode(".//x:t", namespaceManager);
                return inlineText != null ? inlineText.InnerText : "";
            }

            var valueNode = cellNode.SelectSingleNode("x:v", namespaceManager);
            if (valueNode == null)
            {
                return "";
            }

            if (type == "s" && int.TryParse(valueNode.InnerText, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedIndex];
            }

            return valueNode.InnerText;
        }

        private static XmlDocument LoadXml(ZipArchiveEntry entry)
        {
            var doc = new XmlDocument();
            using (var stream = entry.Open())
            {
                doc.Load(stream);
            }
            return doc;
        }

        private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
        {
            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            return namespaceManager;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
            if (string.IsNullOrEmpty(letters))
            {
                return 0;
            }

            var index = 0;
            foreach (var letter in letters.ToUpperInvariant())
            {
                index = index * 26 + (letter - 'A' + 1);
            }
            return index - 1;
        }
    }
}
