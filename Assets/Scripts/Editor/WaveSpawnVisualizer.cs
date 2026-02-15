using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window để visualize và edit spawn points của tất cả waves
/// </summary>
public class WaveSpawnVisualizer : EditorWindow
{
    private WaveConfig waveConfig;
    private int selectedWave = 0;
    private Vector2 scrollPosition;
    private bool showAllWaves = false;

    [MenuItem("Tools/Wave/Spawn Point Visualizer")]
    public static void ShowWindow()
    {
        WaveSpawnVisualizer window = GetWindow<WaveSpawnVisualizer>("Wave Spawn Visualizer");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Wave Spawn Point Visualizer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Wave Config selector
        WaveConfig newConfig = (WaveConfig)EditorGUILayout.ObjectField("Wave Config", waveConfig, typeof(WaveConfig), false);

        if (newConfig != waveConfig)
        {
            waveConfig = newConfig;
            selectedWave = 0;
            SceneView.RepaintAll();
        }

        if (waveConfig == null)
        {
            EditorGUILayout.HelpBox("Select a WaveConfig to visualize spawn points.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);

        // Wave selector
        if (waveConfig.waves.Count > 0)
        {
            selectedWave = EditorGUILayout.IntSlider("Selected Wave", selectedWave, 0, waveConfig.waves.Count - 1);

            showAllWaves = EditorGUILayout.Toggle("Show All Waves", showAllWaves);

            EditorGUILayout.Space(5);

            // Wave info
            SimpleWaveData wave = waveConfig.waves[selectedWave];
            EditorGUILayout.LabelField($"Wave {selectedWave + 1} Info:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Groups: {wave.enemyGroups.Count}");
            EditorGUILayout.LabelField($"Preparation Time: {wave.preparationTime}s");

            EditorGUILayout.Space(10);

            // Groups list
            EditorGUILayout.LabelField("Enemy Groups:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < wave.enemyGroups.Count; i++)
            {
                EnemyGroup group = wave.enemyGroups[i];

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Group {i + 1}", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                group.enemyPoolType = (PoolType)EditorGUILayout.EnumPopup("Enemy Type", group.enemyPoolType);
                group.enemyCount = EditorGUILayout.IntField("Count", group.enemyCount);
                group.spawnPosition = EditorGUILayout.Vector3Field("Position", group.spawnPosition);
                group.spreadRadius = EditorGUILayout.FloatField("Spread Radius", group.spreadRadius);
                group.spawnDelay = EditorGUILayout.FloatField("Spawn Delay", group.spawnDelay);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(waveConfig);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Focus in Scene"))
                {
                    FocusSceneViewOnPosition(group.spawnPosition);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Buttons
            if (GUILayout.Button("Focus on All Spawn Points"))
            {
                FocusOnAllSpawnPoints();
            }

            if (GUILayout.Button("Refresh Scene View"))
            {
                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("This WaveConfig has no waves configured.", MessageType.Warning);
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
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (waveConfig == null) return;

        if (showAllWaves)
        {
            // Draw all waves
            for (int w = 0; w < waveConfig.waves.Count; w++)
            {
                DrawWaveGizmos(waveConfig.waves[w], w, w == selectedWave);
            }
        }
        else if (selectedWave < waveConfig.waves.Count)
        {
            // Draw selected wave only
            DrawWaveGizmos(waveConfig.waves[selectedWave], selectedWave, true);
        }
    }

    private void DrawWaveGizmos(SimpleWaveData wave, int waveIndex, bool isSelected)
    {
        if (wave == null || wave.enemyGroups.Count == 0) return;

        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup group = wave.enemyGroups[i];
            Color groupColor = Color.HSVToRGB((i * 0.2f) % 1f, 0.8f, isSelected ? 1f : 0.5f);

            // Draw spawn point
            Handles.color = groupColor;
            Handles.DrawWireDisc(group.spawnPosition, Vector3.up, group.spreadRadius);
            Handles.DrawSolidDisc(group.spawnPosition, Vector3.up, 0.3f);

            // Label
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = isSelected ? Color.white : new Color(1, 1, 1, 0.5f);
            style.fontStyle = FontStyle.Bold;

            Handles.Label(
                group.spawnPosition + Vector3.up * 2f,
                $"W{waveIndex + 1}-G{i + 1}: {group.enemyCount}x {group.enemyPoolType}",
                style
            );
        }
    }

    private void FocusSceneViewOnPosition(Vector3 position)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = position;
            sceneView.size = 15f;
            sceneView.Repaint();
        }
    }

    private void FocusOnAllSpawnPoints()
    {
        if (waveConfig == null || waveConfig.waves.Count == 0) return;

        Bounds bounds = new Bounds();
        bool first = true;

        foreach (var wave in waveConfig.waves)
        {
            foreach (var group in wave.enemyGroups)
            {
                if (first)
                {
                    bounds = new Bounds(group.spawnPosition, Vector3.one);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(group.spawnPosition);
                }
            }
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.Frame(bounds, false);
            sceneView.Repaint();
        }
    }
}
