using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class StudentCardQuickConfigurator : EditorWindow
{
    private const string StatBody = "\u8eab\u5fc3";
    private const string StatAcademic = "\u5b66\u4e1a";
    private const string StatSocial = "\u4eba\u9645";
    private const string StatMoney = "\u7ecf\u6d4e";

    private static readonly string[] OutputHeaders =
    {
        "GroupName",
        "CardName",
        "StyleName",
        "EventScript.titleText",
        "EventScript.questionText",
        "EventScript.answerLeft",
        "EventScript.answerRight",
        "ImageFile",
        "EventScript.cardPropability",
        "EventScript.redrawBlockCnt",
        "EventScript.maxDraws",
        "StudentCard.unique",
        "Left_" + StatBody,
        "Left_" + StatAcademic,
        "Left_" + StatSocial,
        "Left_" + StatMoney,
        "Right_" + StatBody,
        "Right_" + StatAcademic,
        "Right_" + StatSocial,
        "Right_" + StatMoney
    };

    private static readonly string[] StatNames = { StatBody, StatAcademic, StatSocial, StatMoney };
    private static readonly int[] ValueChoices = { 15, 10, 5, -15, -10, -5 };

    private string sourcePath = "";
    private string outputCsvPath = "Assets/StudentSimulator/Data/ConfiguredCards.csv";
    private string groupName = "Student";
    private string styleName = "cs_default";
    private int exportLimit = 10;

    private List<Dictionary<string, string>> sourceRows = new List<Dictionary<string, string>>();
    private List<CardConfig> cards = new List<CardConfig>();
    private List<string> headers = new List<string>();
    private int currentIndex;
    private int idHeaderIndex;
    private int titleHeaderIndex;
    private int questionHeaderIndex;
    private int leftHeaderIndex;
    private int rightHeaderIndex;
    private int imageHeaderIndex = -1;
    private int activeChoice = -1;
    private int activeStat = -1;
    private Vector2 scroll;
    private string report = "";

    [MenuItem("Tools/Student Simulator/Quick Card Configurator")]
    public static void Open()
    {
        GetWindow<StudentCardQuickConfigurator>("Quick Card Config");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Quick Card Configurator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Load a story table, review one card row at a time, click value buttons, then export a CSV compatible with the card importer.", MessageType.Info);

        DrawFilePicker("Source Excel/CSV", ref sourcePath, "xlsx,csv");
        outputCsvPath = EditorGUILayout.TextField("Output CSV", outputCsvPath);
        groupName = EditorGUILayout.TextField("GroupName", groupName);
        styleName = EditorGUILayout.TextField("StyleName", styleName);
        exportLimit = EditorGUILayout.IntField("Export Limit", exportLimit);

        if (GUILayout.Button("Load Source Table"))
        {
            LoadSource();
        }

        if (sourceRows.Count > 0)
        {
            DrawHeaderMapping();
            DrawCurrentCard();
            DrawNavigation();
            DrawExportButtons();
        }

        if (!string.IsNullOrWhiteSpace(report))
        {
            EditorGUILayout.Space();
            EditorGUILayout.TextArea(report, GUILayout.MinHeight(90));
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

    private void LoadSource()
    {
        report = "";
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            report = "Source table does not exist.";
            return;
        }

        try
        {
            sourceRows = StudentCardSheetReader.Read(sourcePath);
            headers = sourceRows.Count > 0 ? sourceRows[0].Keys.ToList() : new List<string>();
            cards = sourceRows.Select((row, index) => new CardConfig { SourceRow = row, SourceIndex = index }).ToList();
            currentIndex = 0;
            activeChoice = -1;
            activeStat = -1;

            idHeaderIndex = GuessHeaderIndex("ID", "Id", "id", "\u7f16\u53f7", "\u4e8b\u4ef6ID", "\u4e8b\u4ef6 ID");
            titleHeaderIndex = GuessHeaderIndex("\u4e8b\u4ef6\u6807\u9898", "\u6807\u9898", "Title", "title", "EventScript.titleText");
            questionHeaderIndex = GuessHeaderIndex("\u5361\u9762\u6587\u672c", "\u5361\u7247\u5185\u5bb9", "\u5185\u5bb9", "\u63cf\u8ff0", "\u4e8b\u4ef6\u5185\u5bb9", "\u6b63\u6587", "Question", "question", "EventScript.questionText");
            leftHeaderIndex = GuessHeaderIndex("\u5de6\u6ed1\u9009\u62e9", "\u5de6\u9009\u9879", "\u9009\u9879A", "\u9009\u9879 A", "\u9009\u98791", "\u9009\u9879 1", "AnswerLeft", "EventScript.answerLeft");
            rightHeaderIndex = GuessHeaderIndex("\u53f3\u6ed1\u9009\u62e9", "\u53f3\u9009\u9879", "\u9009\u9879B", "\u9009\u9879 B", "\u9009\u98792", "\u9009\u9879 2", "AnswerRight", "EventScript.answerRight");
            imageHeaderIndex = GuessOptionalHeaderIndex("ImageFile", "\u56fe\u7247", "\u56fe\u7247\u6587\u4ef6", "\u5361\u56fe", "\u56fe\u7247\u540d");

            report = "Loaded rows: " + sourceRows.Count + "\nHeaders: " + string.Join(", ", headers);
            if (headers.Count <= 1)
            {
                report += "\nWarning: only one header was detected. Please confirm the source workbook has been saved with card text columns.";
            }
        }
        catch (Exception ex)
        {
            report = ex.GetType().Name + ": " + ex.Message;
        }
    }

    private int GuessHeaderIndex(params string[] candidates)
    {
        var guessed = GuessOptionalHeaderIndex(candidates);
        return guessed >= 0 ? guessed : headers.Count > 0 ? 0 : -1;
    }

    private int GuessOptionalHeaderIndex(params string[] candidates)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            foreach (var candidate in candidates)
            {
                if (string.Equals(headers[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private void DrawHeaderMapping()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Column Mapping", EditorStyles.boldLabel);
        var options = headers.Count > 0 ? headers.ToArray() : new[] { "<none>" };
        idHeaderIndex = EditorGUILayout.Popup("ID / CardName", ClampIndex(idHeaderIndex), options);
        titleHeaderIndex = EditorGUILayout.Popup("Title", ClampIndex(titleHeaderIndex), options);
        questionHeaderIndex = EditorGUILayout.Popup("Card Content", ClampIndex(questionHeaderIndex), options);
        leftHeaderIndex = EditorGUILayout.Popup("Left Option", ClampIndex(leftHeaderIndex), options);
        rightHeaderIndex = EditorGUILayout.Popup("Right Option", ClampIndex(rightHeaderIndex), options);
        imageHeaderIndex = DrawOptionalPopup("ImageFile", imageHeaderIndex);
    }

    private int DrawOptionalPopup(string label, int index)
    {
        var options = new[] { "<none>" }.Concat(headers).ToArray();
        var selected = index < 0 ? 0 : index + 1;
        selected = EditorGUILayout.Popup(label, Mathf.Clamp(selected, 0, options.Length - 1), options);
        return selected == 0 ? -1 : selected - 1;
    }

    private int ClampIndex(int index)
    {
        if (headers.Count == 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, headers.Count - 1);
    }

    private void DrawCurrentCard()
    {
        var card = cards[currentIndex];
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card " + (currentIndex + 1) + " / " + cards.Count, EditorStyles.boldLabel);

        EditorGUILayout.LabelField("ID", GetMapped(card, idHeaderIndex));
        EditorGUILayout.LabelField("Title", GetMapped(card, titleHeaderIndex));

        EditorGUILayout.LabelField("Content");
        EditorGUILayout.TextArea(GetMapped(card, questionHeaderIndex), GUILayout.MinHeight(70));

        EditorGUILayout.BeginHorizontal();
        DrawOptionPanel(card, 0, "Left Option", GetMapped(card, leftHeaderIndex));
        DrawOptionPanel(card, 1, "Right Option", GetMapped(card, rightHeaderIndex));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOptionPanel(CardConfig card, int choiceIndex, string label, string text)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(300));
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.TextArea(text, GUILayout.MinHeight(48));

        var values = choiceIndex == 0 ? card.LeftValues : card.RightValues;
        for (var stat = 0; stat < StatNames.Length; stat++)
        {
            DrawStatSelector(values, choiceIndex, stat);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatSelector(int[] values, int choiceIndex, int statIndex)
    {
        EditorGUILayout.BeginHorizontal();

        var oldColor = GUI.backgroundColor;
        if (values[statIndex] != 0)
        {
            GUI.backgroundColor = values[statIndex] > 0 ? new Color(0.55f, 0.85f, 0.6f) : new Color(0.95f, 0.62f, 0.58f);
        }
        else if (activeChoice == choiceIndex && activeStat == statIndex)
        {
            GUI.backgroundColor = new Color(0.7f, 0.82f, 1f);
        }

        if (GUILayout.Button(StatNames[statIndex] + ": " + values[statIndex], GUILayout.Width(90)))
        {
            activeChoice = choiceIndex;
            activeStat = statIndex;
        }

        GUI.backgroundColor = oldColor;

        if (activeChoice == choiceIndex && activeStat == statIndex)
        {
            foreach (var value in ValueChoices)
            {
                if (GUILayout.Button(FormatValue(value), GUILayout.Width(48)))
                {
                    values[statIndex] = value;
                }
            }

            if (GUILayout.Button("0", GUILayout.Width(36)))
            {
                values[statIndex] = 0;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private static string FormatValue(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    private void DrawNavigation()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = currentIndex > 0;
        if (GUILayout.Button("Previous"))
        {
            currentIndex--;
            activeChoice = -1;
            activeStat = -1;
        }

        GUI.enabled = currentIndex < cards.Count - 1;
        if (GUILayout.Button("Next"))
        {
            currentIndex++;
            activeChoice = -1;
            activeStat = -1;
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawExportButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Compatible CSV"))
        {
            ExportCsv();
        }

        if (GUILayout.Button("Open Card Importer"))
        {
            StudentCardExcelImporter.Open();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ExportCsv()
    {
        if (exportLimit <= 0)
        {
            report = "Export Limit must be greater than 0.";
            return;
        }

        try
        {
            var rowsToExport = cards.Take(Mathf.Min(exportLimit, cards.Count)).ToList();
            var assetPath = outputCsvPath.Replace("\\", "/");
            var absolutePath = ToAbsoluteProjectPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            var lines = new List<string> { ToCsvLine(OutputHeaders) };
            for (var i = 0; i < rowsToExport.Count; i++)
            {
                lines.Add(ToCsvLine(BuildOutputRow(rowsToExport[i], i)));
            }

            File.WriteAllLines(absolutePath, lines, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
            report = "Exported compatible CSV:\n" + assetPath + "\nRows: " + rowsToExport.Count;
        }
        catch (Exception ex)
        {
            report = ex.GetType().Name + ": " + ex.Message;
        }
    }

    private string[] BuildOutputRow(CardConfig card, int index)
    {
        var id = GetMapped(card, idHeaderIndex);
        var cardName = string.IsNullOrWhiteSpace(id) ? "E" + (index + 1).ToString("000") : SanitizeFileName(id);
        var title = GetMapped(card, titleHeaderIndex);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = cardName;
        }

        return new[]
        {
            groupName,
            cardName,
            styleName,
            title,
            GetMapped(card, questionHeaderIndex),
            GetMapped(card, leftHeaderIndex),
            GetMapped(card, rightHeaderIndex),
            GetMapped(card, imageHeaderIndex),
            "1",
            "0",
            "100",
            "false",
            card.LeftValues[0].ToString(),
            card.LeftValues[1].ToString(),
            card.LeftValues[2].ToString(),
            card.LeftValues[3].ToString(),
            card.RightValues[0].ToString(),
            card.RightValues[1].ToString(),
            card.RightValues[2].ToString(),
            card.RightValues[3].ToString()
        };
    }

    private string GetMapped(CardConfig card, int headerIndex)
    {
        if (headers.Count == 0 || headerIndex < 0 || headerIndex >= headers.Count)
        {
            return "";
        }

        return card.SourceRow.TryGetValue(headers[headerIndex], out var value) ? value : "";
    }

    private static string ToCsvLine(IEnumerable<string> values)
    {
        return string.Join(",", values.Select(EscapeCsv));
    }

    private static string EscapeCsv(string value)
    {
        value = value ?? "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Unnamed" : sanitized;
    }

    private static string ToAbsoluteProjectPath(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Output CSV must be under Assets/.");
        }

        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    private sealed class CardConfig
    {
        public Dictionary<string, string> SourceRow;
        public int SourceIndex;
        public int[] LeftValues = new int[4];
        public int[] RightValues = new int[4];
    }
}
