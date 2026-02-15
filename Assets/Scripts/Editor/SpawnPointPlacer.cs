using UnityEngine;
using UnityEditor;

/// <summary>
/// Scene tool để tạo spawn points bằng cách click vào Scene view
/// </summary>
public class SpawnPointPlacer : EditorWindow
{
    private WaveConfig targetConfig;
    private int targetWaveIndex = 0;
    private PoolType enemyType = PoolType.MeleeEnemy;
    private int enemyCount = 5;
    private float spreadRadius = 3f;
    private float spawnDelay = 0f;
    private bool placementMode = false;

    [MenuItem("Tools/Wave/Spawn Point Placer")]
    public static void ShowWindow()
    {
        SpawnPointPlacer window = GetWindow<SpawnPointPlacer>("Spawn Point Placer");
        window.minSize = new Vector2(300, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spawn Point Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "1. Select a WaveConfig\n" +
            "2. Choose wave and settings\n" +
            "3. Enable Placement Mode\n" +
            "4. Click in Scene view to place spawn points",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Config selector
        targetConfig = (WaveConfig)EditorGUILayout.ObjectField("Target Wave Config", targetConfig, typeof(WaveConfig), false);

        if (targetConfig == null)
        {
            EditorGUILayout.HelpBox("Select a WaveConfig to start placing spawn points.", MessageType.Warning);
            return;
        }

        // Wave selector
        if (targetConfig.waves.Count > 0)
        {
            targetWaveIndex = EditorGUILayout.IntSlider("Target Wave", targetWaveIndex, 0, targetConfig.waves.Count - 1);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("New Spawn Point Settings:", EditorStyles.boldLabel);

            enemyType = (PoolType)EditorGUILayout.EnumPopup("Enemy Type", enemyType);
            enemyCount = EditorGUILayout.IntField("Enemy Count", enemyCount);
            spreadRadius = EditorGUILayout.FloatField("Spread Radius", spreadRadius);
            spawnDelay = EditorGUILayout.FloatField("Spawn Delay", spawnDelay);

            EditorGUILayout.Space(10);

            // Placement mode toggle
            Color originalColor = GUI.backgroundColor;
            if (placementMode)
            {
                GUI.backgroundColor = Color.green;
            }

            if (GUILayout.Button(placementMode ? "⬤ Placement Mode ACTIVE" : "○ Enable Placement Mode", GUILayout.Height(30)))
            {
                placementMode = !placementMode;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = originalColor;

            if (placementMode)
            {
                EditorGUILayout.HelpBox("Click in Scene view to place spawn points. Click again to disable.", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Current wave info
            SimpleWaveData wave = targetConfig.waves[targetWaveIndex];
            EditorGUILayout.LabelField($"Wave {targetWaveIndex + 1} - Current Groups: {wave.enemyGroups.Count}", EditorStyles.miniLabel);

            // Quick remove last
            if (wave.enemyGroups.Count > 0)
            {
                if (GUILayout.Button("Remove Last Spawn Point"))
                {
                    Undo.RecordObject(targetConfig, "Remove Spawn Point");
                    wave.enemyGroups.RemoveAt(wave.enemyGroups.Count - 1);
                    EditorUtility.SetDirty(targetConfig);
                    SceneView.RepaintAll();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("This WaveConfig has no waves. Add waves first.", MessageType.Warning);
        }
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        placementMode = false;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!placementMode || targetConfig == null || targetWaveIndex >= targetConfig.waves.Count)
            return;

        // Draw cursor preview
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Draw preview at mouse position
            Handles.color = new Color(0, 1, 0, 0.5f);
            Handles.DrawWireDisc(hit.point, Vector3.up, spreadRadius);
            Handles.DrawSolidDisc(hit.point, Vector3.up, 0.3f);

            Handles.color = Color.green;
            Handles.Label(hit.point + Vector3.up * 2f, $"Click to place\n{enemyCount}x {enemyType}", EditorStyles.whiteLargeLabel);

            // Handle click
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PlaceSpawnPoint(hit.point);
                e.Use();
            }
        }

        // Draw instruction
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Box("PLACEMENT MODE ACTIVE\nClick in Scene to place spawn point\nPress ESC to cancel", GUILayout.Width(300));
        GUILayout.EndArea();
        Handles.EndGUI();

        // ESC to cancel
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            placementMode = false;
            e.Use();
            Repaint();
        }

        SceneView.RepaintAll();
    }

    private void PlaceSpawnPoint(Vector3 position)
    {
        if (targetConfig == null || targetWaveIndex >= targetConfig.waves.Count)
            return;

        Undo.RecordObject(targetConfig, "Add Spawn Point");

        EnemyGroup newGroup = new EnemyGroup
        {
            enemyPoolType = enemyType,
            enemyCount = enemyCount,
            spawnPosition = position,
            spreadRadius = spreadRadius,
            spawnDelay = spawnDelay
        };

        targetConfig.waves[targetWaveIndex].enemyGroups.Add(newGroup);
        EditorUtility.SetDirty(targetConfig);

        Debug.Log($"✓ Added spawn point at {position} to Wave {targetWaveIndex + 1}");
        SceneView.RepaintAll();
    }
}
