using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WaveSpawnVisualizer : EditorWindow
{
    private WaveConfig waveConfig;
    private int selectedWave = 0;
    private Vector2 scrollPosition;
    private bool showAllWaves = false;

    // Copy/Paste
    private SimpleWaveData copiedWave = null;

    // Endless Preview
    private int endlessPreviewWave = 11;
    private bool showEndlessPreview = false;

    [MenuItem("Tools/Wave/Spawn Point Visualizer")]
    public static void ShowWindow()
    {
        WaveSpawnVisualizer window = GetWindow<WaveSpawnVisualizer>("Wave Spawn Visualizer");
        window.minSize = new Vector2(420, 400);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnSelectionChanged()
    {
        WaveConfig selectedConfig = Selection.activeObject as WaveConfig;
        if (selectedConfig != null)
        {
            waveConfig = selectedConfig;
            selectedWave = 0;
            Repaint();
            SceneView.RepaintAll();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Wave Spawn Point Visualizer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        WaveConfig newConfig = (WaveConfig)EditorGUILayout.ObjectField("Wave Config", waveConfig, typeof(WaveConfig), false);
        if (newConfig != waveConfig)
        {
            waveConfig = newConfig;
            selectedWave = 0;
            SceneView.RepaintAll();
        }

        if (waveConfig == null)
        {
            EditorGUILayout.HelpBox("Select a WaveConfig or click a WaveConfig asset in the Project window.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(8);

        if (waveConfig.waves.Count > 0)
        {
            // ─── Wave Selector ─────────────────────────────────────────
            selectedWave = EditorGUILayout.IntSlider("Selected Wave", selectedWave, 0, waveConfig.waves.Count - 1);
            showAllWaves = EditorGUILayout.Toggle("Show All Waves", showAllWaves);

            EditorGUILayout.Space(5);

            SimpleWaveData wave = waveConfig.waves[selectedWave];
            EditorGUILayout.LabelField($"Wave {selectedWave + 1} Info:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Groups: {wave.enemyGroups.Count}  |  Prep Time: {wave.preparationTime}s  |  Boss: {wave.isBossWave}");

            EditorGUILayout.Space(5);

            // ─── Copy / Paste ──────────────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Wave"))
                {
                    copiedWave = DeepCopyWave(wave);
                    Debug.Log($"Copied Wave {selectedWave + 1}");
                }

                GUI.enabled = copiedWave != null;
                if (GUILayout.Button("Paste to This Wave"))
                {
                    Undo.RecordObject(waveConfig, "Paste Wave");
                    waveConfig.waves[selectedWave] = DeepCopyWave(copiedWave);
                    EditorUtility.SetDirty(waveConfig);
                    SceneView.RepaintAll();
                    Debug.Log($"Pasted to Wave {selectedWave + 1}");
                }
                GUI.enabled = true;
            }

            if (copiedWave != null)
                EditorGUILayout.LabelField($"Clipboard: {copiedWave.enemyGroups.Count} groups", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // ─── Enemy Groups List ─────────────────────────────────────
            EditorGUILayout.LabelField("Enemy Groups:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));

            for (int i = 0; i < wave.enemyGroups.Count; i++)
            {
                EnemyGroup group = wave.enemyGroups[i];

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Group {i + 1}", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                group.enemyPoolType = (PoolType)EditorGUILayout.EnumPopup("Enemy Type", group.enemyPoolType);
                group.enemyCount    = EditorGUILayout.IntField("Count", group.enemyCount);
                group.spawnPosition = EditorGUILayout.Vector3Field("Position", group.spawnPosition);
                group.spreadRadius  = EditorGUILayout.FloatField("Spread Radius", group.spreadRadius);
                group.spawnDelay    = EditorGUILayout.FloatField("Spawn Delay", group.spawnDelay);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(waveConfig);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Focus in Scene"))
                    FocusSceneViewOnPosition(group.spawnPosition);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Focus on All Spawn Points"))
                    FocusOnAllSpawnPoints();

                if (GUILayout.Button("Refresh Scene View"))
                    SceneView.RepaintAll();
            }

            EditorGUILayout.Space(12);

            // ─── Endless Wave Preview ──────────────────────────────────
            EditorGUILayout.LabelField("Endless Wave Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                showEndlessPreview = EditorGUILayout.Toggle("Show Endless Preview", showEndlessPreview);

                if (showEndlessPreview)
                {
                    int configCount = waveConfig.waves.Count;
                    endlessPreviewWave = EditorGUILayout.IntField("Simulate Wave #", endlessPreviewWave);
                    endlessPreviewWave = Mathf.Max(configCount + 1, endlessPreviewWave);

                    int baseIndex = (endlessPreviewWave - 1) % configCount;
                    int loopCount = (endlessPreviewWave - 1) / configCount;
                    int extraEnemies = loopCount;
                    float extraRadius = loopCount * 1f;

                    SimpleWaveData baseWave = waveConfig.waves[baseIndex];

                    EditorGUILayout.LabelField($"→ Loops: {loopCount}  |  Based on Wave {baseIndex + 1}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"→ Extra enemies per group: +{extraEnemies}  |  Extra radius: +{extraRadius:F1}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"→ Groups: {baseWave.enemyGroups.Count}", EditorStyles.miniLabel);

                    for (int i = 0; i < baseWave.enemyGroups.Count; i++)
                    {
                        var g = baseWave.enemyGroups[i];
                        EditorGUILayout.LabelField($"   Group {i+1}: {g.enemyCount + extraEnemies}x {g.enemyPoolType} | r={g.spreadRadius + extraRadius:F1}", EditorStyles.miniLabel);
                    }

                    if (GUILayout.Button("Show in Scene View"))
                        SceneView.RepaintAll();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("This WaveConfig has no waves configured.", MessageType.Warning);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (waveConfig == null) return;

        if (showAllWaves)
        {
            for (int w = 0; w < waveConfig.waves.Count; w++)
                DrawWaveGizmos(waveConfig.waves[w], w, w == selectedWave);
        }
        else if (selectedWave < waveConfig.waves.Count)
        {
            DrawWaveGizmos(waveConfig.waves[selectedWave], selectedWave, true);
        }

        // Draw endless preview
        if (showEndlessPreview)
        {
            int configCount = waveConfig.waves.Count;
            if (endlessPreviewWave > configCount && configCount > 0)
            {
                int baseIndex = (endlessPreviewWave - 1) % configCount;
                int loopCount = (endlessPreviewWave - 1) / configCount;
                int extraEnemies = loopCount;
                float extraRadius = loopCount * 1f;

                SimpleWaveData baseWave = waveConfig.waves[baseIndex];
                for (int i = 0; i < baseWave.enemyGroups.Count; i++)
                {
                    var g = baseWave.enemyGroups[i];
                    Color c = new Color(1f, 0.5f, 0f, 0.8f); // orange for endless
                    Handles.color = c;
                    Handles.DrawWireDisc(g.spawnPosition, Vector3.up, g.spreadRadius + extraRadius);
                    Handles.DrawSolidDisc(g.spawnPosition, Vector3.up, 0.4f);

                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        normal = { textColor = Color.yellow },
                        fontStyle = FontStyle.Bold
                    };
                    Handles.Label(g.spawnPosition + Vector3.up * 3f,
                        $"Wave {endlessPreviewWave} Preview\n{g.enemyCount + extraEnemies}x {g.enemyPoolType}", style);
                }
            }
        }
    }

    private void DrawWaveGizmos(SimpleWaveData wave, int waveIndex, bool isSelected)
    {
        if (wave == null || wave.enemyGroups.Count == 0) return;

        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup group = wave.enemyGroups[i];
            Color groupColor = Color.HSVToRGB((i * 0.2f) % 1f, 0.8f, isSelected ? 1f : 0.5f);

            Handles.color = groupColor;
            Handles.DrawWireDisc(group.spawnPosition, Vector3.up, group.spreadRadius);
            Handles.DrawSolidDisc(group.spawnPosition, Vector3.up, 0.3f);

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = isSelected ? Color.white : new Color(1, 1, 1, 0.5f) },
                fontStyle = FontStyle.Bold
            };

            Handles.Label(
                group.spawnPosition + Vector3.up * 2f,
                $"W{waveIndex + 1}-G{i + 1}: {group.enemyCount}x {group.enemyPoolType}",
                style
            );
        }
    }

    // ─── Copy / Paste Helpers ──────────────────────────────────────────
    private SimpleWaveData DeepCopyWave(SimpleWaveData source)
    {
        SimpleWaveData copy = new SimpleWaveData
        {
            preparationTime = source.preparationTime,
            isBossWave      = source.isBossWave,
            bossPoolType    = source.bossPoolType,
            enemyGroups     = new List<EnemyGroup>()
        };

        foreach (var g in source.enemyGroups)
        {
            copy.enemyGroups.Add(new EnemyGroup
            {
                enemyPoolType = g.enemyPoolType,
                enemyCount    = g.enemyCount,
                spawnPosition = g.spawnPosition,
                spreadRadius  = g.spreadRadius,
                spawnDelay    = g.spawnDelay
            });
        }
        return copy;
    }

    private void FocusSceneViewOnPosition(Vector3 position)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = position;
            sceneView.size  = 15f;
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
                if (first) { bounds = new Bounds(group.spawnPosition, Vector3.one); first = false; }
                else bounds.Encapsulate(group.spawnPosition);
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
