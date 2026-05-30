using UnityEditor;
using UnityEngine;

public class DamageTextPoolTestWindow : EditorWindow
{
    private DamageTextManager damageTextManager;
    private Transform target;
    private bool useTargetTransform = true;
    private Vector3 worldPosition = Vector3.zero;
    private int spawnCount = 100;
    private float interval = 0.01f;
    private int minDamage = 10;
    private int maxDamage = 999;
    private int maxBurstPerEditorUpdate = 100;
    private Color textColor = Color.red;
    private DamageTextManager.DamageTextTarget damageTextTarget = DamageTextManager.DamageTextTarget.Enemy;

    private bool isRunning;
    private int emittedCount;
    private double nextEmitTime;

    [MenuItem("Window/Analysis/Damage Text Pool Test")]
    public static void OpenFromWindowMenu()
    {
        Open();
    }

    [MenuItem("Tools/Tests/Damage Text Pool Test")]
    public static void Open()
    {
        GetWindow<DamageTextPoolTestWindow>("Damage Text Pool Test");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        FindManager();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Damage Text Pool Test", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter Play Mode before running the test. Use the Profiler while this tool emits repeated damage text events.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            damageTextManager = (DamageTextManager)EditorGUILayout.ObjectField("Manager", damageTextManager, typeof(DamageTextManager), true);
            if (GUILayout.Button("Find", GUILayout.Width(64)))
                FindManager();
        }

        useTargetTransform = EditorGUILayout.Toggle("Use Target Transform", useTargetTransform);
        using (new EditorGUI.DisabledScope(!useTargetTransform))
        {
            target = (Transform)EditorGUILayout.ObjectField("Target", target, typeof(Transform), true);
            if (GUILayout.Button("Use Selected Transform"))
                target = Selection.activeTransform;
        }

        using (new EditorGUI.DisabledScope(useTargetTransform))
        {
            worldPosition = EditorGUILayout.Vector3Field("World Position", worldPosition);
        }

        EditorGUILayout.Space(6f);
        spawnCount = Mathf.Max(1, EditorGUILayout.IntField("Spawn Count", spawnCount));
        interval = Mathf.Max(0f, EditorGUILayout.FloatField("Interval Seconds", interval));
        maxBurstPerEditorUpdate = Mathf.Max(1, EditorGUILayout.IntField("Max Burst Per Update", maxBurstPerEditorUpdate));
        minDamage = EditorGUILayout.IntField("Min Damage", minDamage);
        maxDamage = EditorGUILayout.IntField("Max Damage", Mathf.Max(minDamage, maxDamage));
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
        damageTextTarget = (DamageTextManager.DamageTextTarget)EditorGUILayout.EnumPopup("Damage Text Target", damageTextTarget);

        EditorGUILayout.Space(8f);
        DrawPoolStats();

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(isRunning || !EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Run Test", GUILayout.Height(28)))
                    StartTest();
            }

            using (new EditorGUI.DisabledScope(!isRunning))
            {
                if (GUILayout.Button("Stop", GUILayout.Height(28)))
                    StopTest("Stopped.");
            }
        }

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("The test is disabled outside Play Mode because DamageText uses runtime Update and Canvas state.", MessageType.Warning);

        if (isRunning)
        {
            float progress = spawnCount > 0 ? (float)emittedCount / spawnCount : 0f;
            Rect rect = GUILayoutUtility.GetRect(18f, 18f);
            EditorGUI.ProgressBar(rect, progress, $"{emittedCount}/{spawnCount}");
        }
    }

    private void DrawPoolStats()
    {
        MonoBehaviour pool = FindPoolManager();
        GameObject prefab = damageTextManager != null ? damageTextManager.damageTextPrefab : null;

        EditorGUILayout.LabelField("Pool Stats", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("ObjectPoolManager", pool != null ? pool.name : "Not Found");
        EditorGUILayout.LabelField("Damage Text Prefab", prefab != null ? prefab.name : "Not Assigned");

        if (pool == null || prefab == null) return;

        EditorGUILayout.LabelField("Tracked", GetPoolCount(pool, "GetTrackedCount", prefab));
        EditorGUILayout.LabelField("Active", GetPoolCount(pool, "GetActiveCount", prefab));
        EditorGUILayout.LabelField("Inactive", GetPoolCount(pool, "GetInactiveCount", prefab));
    }

    private void OnEditorUpdate()
    {
        if (!isRunning) return;

        if (!EditorApplication.isPlaying)
        {
            StopTest("Play Mode ended.");
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        if (interval > 0f && now < nextEmitTime) return;

        int emittedThisUpdate = 0;
        while (isRunning && emittedCount < spawnCount && emittedThisUpdate < maxBurstPerEditorUpdate)
        {
            EmitDamageText();
            emittedCount++;
            emittedThisUpdate++;

            if (interval > 0f)
            {
                nextEmitTime = EditorApplication.timeSinceStartup + interval;
                break;
            }
        }

        if (emittedCount >= spawnCount)
            StopTest($"Completed. Emitted {emittedCount} damage texts.");

        Repaint();
    }

    private void StartTest()
    {
        if (!ValidateInput()) return;

        emittedCount = 0;
        isRunning = true;
        nextEmitTime = EditorApplication.timeSinceStartup;
        Debug.Log($"[DamageTextPoolTest] Started. Count={spawnCount}, Interval={interval}");
    }

    private void StopTest(string message)
    {
        isRunning = false;
        Debug.Log($"[DamageTextPoolTest] {message}");
        Repaint();
    }

    private bool ValidateInput()
    {
        if (damageTextManager == null)
            FindManager();

        if (damageTextManager == null)
        {
            Debug.LogWarning("[DamageTextPoolTest] DamageTextManager was not found.");
            return false;
        }

        if (damageTextManager.damageTextPrefab == null)
        {
            Debug.LogWarning("[DamageTextPoolTest] DamageTextManager.damageTextPrefab is not assigned.");
            return false;
        }

        if (useTargetTransform && target == null)
        {
            Debug.LogWarning("[DamageTextPoolTest] Target Transform is not assigned.");
            return false;
        }

        return true;
    }

    private void EmitDamageText()
    {
        int damage = Random.Range(minDamage, maxDamage + 1);

        if (useTargetTransform)
        {
            damageTextManager.ShowDamage(target, damage, textColor, damageTextTarget);
            return;
        }

        damageTextManager.ShowDamage(worldPosition, damage, textColor, damageTextTarget);
    }

    private void FindManager()
    {
        damageTextManager = Object.FindAnyObjectByType<DamageTextManager>();
    }

    private static MonoBehaviour FindPoolManager()
    {
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour != null && behaviour.GetType().Name == "ObjectPoolManager")
                return behaviour;
        }

        return null;
    }

    private static string GetPoolCount(MonoBehaviour pool, string methodName, GameObject prefab)
    {
        var method = pool.GetType().GetMethod(methodName);
        if (method == null) return "N/A";

        object value = method.Invoke(pool, new object[] { prefab });
        return value != null ? value.ToString() : "N/A";
    }
}
