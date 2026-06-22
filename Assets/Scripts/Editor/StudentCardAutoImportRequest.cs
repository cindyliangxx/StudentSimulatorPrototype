using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StudentCardAutoImportRequest
{
    private const string RequestPath = "Tools/CardConfigTool/unity_import_request.txt";
    private const string ResultPath = "Tools/CardConfigTool/unity_import_result.txt";

    static StudentCardAutoImportRequest()
    {
        EditorApplication.delayCall += ProcessRequest;
    }

    public static void ProcessPendingImport()
    {
        ProcessRequest();
    }

    public static void FixGameplayHud()
    {
        const string scenePath = "Assets/Kings/Game.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var gameCanvas = FindSceneObject("GameCanvas");
        var gamePanel = FindSceneObject("GamePanel");
        var topPanel = FindSceneObject("TopPanel");
        var bottomMenuPanel = FindSceneObject("BottomMenuPanel");

        if (gameCanvas == null || gamePanel == null || topPanel == null || bottomMenuPanel == null)
        {
            throw new InvalidOperationException("Could not find required gameplay HUD objects.");
        }

        gamePanel.transform.SetAsLastSibling();
        topPanel.transform.SetAsLastSibling();

        bottomMenuPanel.transform.SetParent(gamePanel.transform, false);
        bottomMenuPanel.SetActive(true);
        var bottomRect = bottomMenuPanel.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0.5f);
        bottomRect.anchoredPosition = new Vector2(0f, 60f);
        bottomRect.sizeDelta = new Vector2(-80f, 120f);
        bottomMenuPanel.transform.SetAsLastSibling();

        var canvas = gameCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 0;
        }

        EditorUtility.SetDirty(gamePanel);
        EditorUtility.SetDirty(topPanel);
        EditorUtility.SetDirty(bottomMenuPanel);
        if (canvas != null)
        {
            EditorUtility.SetDirty(canvas);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject FindSceneObject(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == objectName && go.scene.IsValid());
    }

    private static void ProcessRequest()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var requestFullPath = Path.Combine(projectRoot, RequestPath);
        if (!File.Exists(requestFullPath))
        {
            return;
        }

        var resultFullPath = Path.Combine(projectRoot, ResultPath);
        try
        {
            var lines = File.ReadAllLines(requestFullPath);
            var tablePath = lines.Length > 0 ? lines[0].Trim() : "";
            var limit = lines.Length > 1 && int.TryParse(lines[1].Trim(), out var parsedLimit) ? parsedLimit : 10;
            if (string.IsNullOrWhiteSpace(tablePath) || !File.Exists(tablePath))
            {
                throw new FileNotFoundException("Card table not found.", tablePath);
            }

            var report = StudentCardExcelImporter.ImportForAutomation(tablePath, limit);
            var configured = ConfigureGameScene(limit);
            File.WriteAllText(resultFullPath, "OK\n" + report + "\n" + configured);
        }
        catch (Exception ex)
        {
            File.WriteAllText(resultFullPath, "ERROR\n" + ex);
            Debug.LogException(ex);
        }
        finally
        {
            File.Delete(requestFullPath);
            AssetDatabase.Refresh();
        }
    }

    private static string ConfigureGameScene(int limit)
    {
        const string scenePath = "Assets/Kings/Game.unity";
        const string cardFolder = "Assets/StudentSimulator/Cards/Student";
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { cardFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Take(limit)
            .ToList();
        var prefabs = prefabPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
            .Where(prefab => prefab != null)
            .ToList();
        if (prefabs.Count == 0)
        {
            return "Linked cards to Game.unity CardStack: 0";
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var stack = UnityEngine.Object.FindObjectOfType<CardStack>();
        if (stack == null)
        {
            throw new InvalidOperationException("CardStack not found in " + scenePath);
        }

        var serializedObject = new SerializedObject(stack);
        var allCards = serializedObject.FindProperty("allCards");
        allCards.arraySize = 1;

        var group = allCards.GetArrayElementAtIndex(0);
        group.FindPropertyRelative("groupName").stringValue = "Student";
        group.FindPropertyRelative("subStackCondition").arraySize = 0;
        var groupCards = group.FindPropertyRelative("groupCards");
        groupCards.arraySize = prefabs.Count;
        for (var i = 0; i < prefabs.Count; i++)
        {
            groupCards.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        serializedObject.FindProperty("fallBackCard").objectReferenceValue = prefabs[0];
        serializedObject.FindProperty("spawnedCard").objectReferenceValue = null;
        serializedObject.FindProperty("followUpCard").objectReferenceValue = null;
        serializedObject.FindProperty("availableCards").arraySize = 0;
        serializedObject.FindProperty("highPriorityCards").arraySize = 0;
        serializedObject.FindProperty("cardDrawCount").FindPropertyRelative("cnt").arraySize = 0;
        serializedObject.FindProperty("cardBlockCount").FindPropertyRelative("cnt").arraySize = 0;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(stack);
        var configuredValues = ConfigureStudentValueScripts();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Linked cards to Game.unity CardStack: " + prefabs.Count
            + "\nCardStack groups kept: 1"
            + "\nConfigured student value scripts: " + configuredValues;
    }

    private static int ConfigureStudentValueScripts()
    {
        var studentValues = new[]
        {
            valueDefinitions.values.army,
            valueDefinitions.values.religion,
            valueDefinitions.values.people,
            valueDefinitions.values.money
        };

        var configured = 0;
        foreach (var valueScript in UnityEngine.Object.FindObjectsOfType<ValueScript>(true))
        {
            if (!studentValues.Contains(valueScript.valueType))
            {
                continue;
            }

            valueScript.limits.min = 0f;
            valueScript.limits.max = 100f;
            valueScript.limits.randomMin = 50f;
            valueScript.limits.randomMax = 50f;
            valueScript.limits.roundToWholeNumbers = true;
            valueScript.value = 50f;
            valueScript.UserInterface.lerpedValue = 50f;
            valueScript.keepValue = false;
            EditorUtility.SetDirty(valueScript);
            configured++;
        }

        return configured;
    }
}
