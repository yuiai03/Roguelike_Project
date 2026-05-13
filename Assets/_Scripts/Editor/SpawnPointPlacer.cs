using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public enum PlacementPattern
{
    Single,
    Circle,
    Line,
    FourCorners
}

public class SpawnPointPlacer : EditorWindow
{
    private static GUIStyle sceneDelayLabelStyle;
    private static GUIStyle sceneMainLabelStyle;

    private WaveConfig targetConfig;
    private int targetWaveIndex = 0;
    private int selectedGroupIndex = -1; // -1 = không chọn cái nào

    // Pattern settings
    private PlacementPattern pattern = PlacementPattern.Single;
    private int patternCount = 8;
    private float patternRadius = 10f;
    private float lineSpacing = 3f;

    // Enemy settings
    private PoolType enemyType = PoolType.Enemy_Melee;
    private int enemyCount = 5;
    private float spreadRadius = 3f;
    private float spawnDelay = 0f;

    // Randomize settings
    private bool randomizeMode = false;
    private bool allowMelee = true;
    private bool allowRanged = true;
    private bool allowFly = false;
    private bool allowRandomBoss = false;

    private bool placementMode = false;

    private Vector2 scrollPos;
    private Vector2 listScrollPos;

    [MenuItem("Tools/Wave/Spawn Point Placer")]
    public static void ShowWindow()
    {
        SpawnPointPlacer window = GetWindow<SpawnPointPlacer>("Spawn Point Placer");
        window.minSize = new Vector2(320, 500);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        placementMode = false;
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Spawn Point Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        targetConfig = (WaveConfig)EditorGUILayout.ObjectField("Target Wave Config", targetConfig, typeof(WaveConfig), false);

        if (targetConfig == null)
        {
            EditorGUILayout.HelpBox("Select a WaveConfig to start placing spawn points.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (targetConfig.waves.Count == 0)
        {
            EditorGUILayout.HelpBox("This WaveConfig has no waves. Add waves first.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // Wave selector
        targetWaveIndex = EditorGUILayout.IntSlider("Target Wave", targetWaveIndex, 0, targetConfig.waves.Count - 1);
        SimpleWaveData wave = targetConfig.waves[targetWaveIndex];
        EditorGUILayout.LabelField($"Wave {targetWaveIndex + 1} — Current Groups: {wave.enemyGroups.Count}", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        // ─── ENEMY SETTINGS ───────────────────────────────────────────
        EditorGUILayout.LabelField("Enemy Settings", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Randomize toggle
            randomizeMode = EditorGUILayout.Toggle("Randomize Enemy Type", randomizeMode);

            if (randomizeMode)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Allow Types", EditorStyles.miniLabel);
                allowMelee  = EditorGUILayout.Toggle("Melee Enemy", allowMelee);
                allowRanged = EditorGUILayout.Toggle("Ranged Enemy", allowRanged);
                allowFly    = EditorGUILayout.Toggle("Fly Enemy", allowFly);
                allowRandomBoss = EditorGUILayout.Toggle("Random Boss", allowRandomBoss);
                EditorGUI.indentLevel--;
            }
            else
            {
                enemyType = (PoolType)EditorGUILayout.EnumPopup("Enemy Type", enemyType);
                EditorGUILayout.HelpBox("Hotkeys in Scene: [1] Melee  [2] Ranged  [3] Fly", MessageType.None);
            }

            enemyCount   = EditorGUILayout.IntField("Enemy Count", enemyCount);
            spreadRadius = EditorGUILayout.FloatField("Spread Radius", spreadRadius);
            spawnDelay   = EditorGUILayout.FloatField("Spawn Delay", spawnDelay);
        }

        EditorGUILayout.Space(8);

        // ─── PATTERN SETTINGS ─────────────────────────────────────────
        EditorGUILayout.LabelField("Placement Pattern", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            pattern = (PlacementPattern)EditorGUILayout.EnumPopup("Pattern", pattern);

            switch (pattern)
            {
                case PlacementPattern.Single:
                    EditorGUILayout.HelpBox("Click to place one spawn point at a time.", MessageType.None);
                    break;
                case PlacementPattern.Circle:
                    patternCount  = EditorGUILayout.IntSlider("Points on Circle", patternCount, 2, 16);
                    patternRadius = EditorGUILayout.FloatField("Circle Radius", patternRadius);
                    EditorGUILayout.HelpBox($"Places {patternCount} spawn groups evenly around clicked point.", MessageType.None);
                    break;
                case PlacementPattern.Line:
                    patternCount = EditorGUILayout.IntSlider("Points in Line", patternCount, 2, 10);
                    lineSpacing  = EditorGUILayout.FloatField("Spacing", lineSpacing);
                    EditorGUILayout.HelpBox($"Places {patternCount} spawn groups in a row.", MessageType.None);
                    break;
                case PlacementPattern.FourCorners:
                    patternRadius = EditorGUILayout.FloatField("Distance from Center", patternRadius);
                    EditorGUILayout.HelpBox("Places 4 spawn groups at the 4 corners from clicked point.", MessageType.None);
                    break;
            }
        }

        EditorGUILayout.Space(10);

        // ─── PLACEMENT BUTTON ─────────────────────────────────────────
        Color originalBg = GUI.backgroundColor;
        GUI.backgroundColor = placementMode ? Color.green : originalBg;

        if (GUILayout.Button(placementMode ? "⬤ Placement Mode ACTIVE" : "○ Enable Placement Mode", GUILayout.Height(30)))
        {
            placementMode = !placementMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = originalBg;

        if (placementMode)
        {
            EditorGUILayout.HelpBox("Click in Scene view to place spawn points.\nPress ESC to cancel.", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // ─── Spawn Point List with Delete ─────────────────────────────
        EditorGUILayout.LabelField("Current Spawn Points", EditorStyles.boldLabel);

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, GUILayout.MaxHeight(180));
        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup g = wave.enemyGroups[i];
            Color rowBg = GUI.backgroundColor;
            if (selectedGroupIndex == i)
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"[{i+1}] {g.enemyCount}x {g.enemyPoolType}  pos={g.spawnPosition:F1}  r={g.spreadRadius:F1}  d={g.spawnDelay:F1}s", EditorStyles.miniLabel);

            if (GUILayout.Button("⊙", GUILayout.Width(28)))
            {
                selectedGroupIndex = i;
                FocusSceneViewOnPosition(g.spawnPosition);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(28)))
            {
                Undo.RecordObject(targetConfig, "Remove Spawn Point");
                wave.enemyGroups.RemoveAt(i);
                if (selectedGroupIndex >= wave.enemyGroups.Count)
                    selectedGroupIndex = wave.enemyGroups.Count - 1;
                EditorUtility.SetDirty(targetConfig);
                SceneView.RepaintAll();
                EditorGUILayout.EndHorizontal();
                break;
            }
            GUI.backgroundColor = rowBg;

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (wave.enemyGroups.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (selectedGroupIndex >= 0 && selectedGroupIndex < wave.enemyGroups.Count)
            {
                if (GUILayout.Button($"Delete Selected (Group {selectedGroupIndex + 1})"))
                {
                    Undo.RecordObject(targetConfig, "Remove Spawn Point");
                    wave.enemyGroups.RemoveAt(selectedGroupIndex);
                    selectedGroupIndex = Mathf.Clamp(selectedGroupIndex - 1, -1, wave.enemyGroups.Count - 1);
                    EditorUtility.SetDirty(targetConfig);
                    SceneView.RepaintAll();
                }
            }
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All Spawn Points",
                    $"Remove all {wave.enemyGroups.Count} spawn points from Wave {targetWaveIndex + 1}?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(targetConfig, "Clear All Spawn Points");
                    wave.enemyGroups.Clear();
                    selectedGroupIndex = -1;
                    EditorUtility.SetDirty(targetConfig);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndScrollView();
    }

    // ─── SCENE GUI ────────────────────────────────────────────────────
    private void OnSceneGUI(SceneView sceneView)
    {
        // Show existing spawn groups in both normal and placement modes.
        Event e = Event.current;
        if (targetConfig != null && targetWaveIndex < targetConfig.waves.Count)
        {
            DrawExistingSpawnGroups(targetConfig.waves[targetWaveIndex], !placementMode);
        }

        if (!placementMode || targetConfig == null || targetWaveIndex >= targetConfig.waves.Count) return;

        e = Event.current;

        // Hotkeys: 1=Melee, 2=Ranged, 3=Fly
        if (!randomizeMode && e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1) { enemyType = PoolType.Enemy_Melee;  e.Use(); Repaint(); }
            if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2) { enemyType = PoolType.Enemy_Ranged; e.Use(); Repaint(); }
            if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3) { enemyType = PoolType.Enemy_Fly;    e.Use(); Repaint(); }
        }

        // ESC to cancel
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            placementMode = false;
            e.Use();
            Repaint();
            return;
        }

        // Raycast to get mouse world position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) { DrawOverlay(e); return; }

        Vector3 center = hit.point;

        // Preview
        List<Vector3> previewPoints = GetPatternPositions(center);
        PoolType displayType = randomizeMode ? PoolType.Enemy_Melee : enemyType;
        string spawnDelayText = FormatSpawnDelay(spawnDelay);

        Handles.color = new Color(0, 1, 0, 0.6f);
        float delayLabelHeight = pattern == PlacementPattern.Single ? 1.8f : 1.2f;
        foreach (var p in previewPoints)
        {
            Handles.DrawWireDisc(p, Vector3.up, spreadRadius);
            Handles.DrawSolidDisc(p, Vector3.up, 0.3f);
            Handles.Label(
                p + Vector3.up * delayLabelHeight,
                $"Delay: {spawnDelayText}",
                GetSceneDelayLabelStyle());
        }

        Handles.color = Color.green;
        Handles.Label(center + Vector3.up * 2.5f,
            $"[{pattern}] Click to place\n{enemyCount}x {(randomizeMode ? "Random" : displayType.ToString())}\nDelay: {spawnDelayText}",
            GetSceneMainLabelStyle());

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            PlacePattern(center);
            e.Use();
        }

        DrawOverlay(e);
    }

    private void DrawExistingSpawnGroups(SimpleWaveData wave, bool allowSelection)
    {
        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup group = wave.enemyGroups[i];

            if (allowSelection)
            {
                float handleSize = HandleUtility.GetHandleSize(group.spawnPosition) * 0.5f;
                if (Handles.Button(group.spawnPosition, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                {
                    selectedGroupIndex = i;
                    Repaint();
                }
            }

            Color c = selectedGroupIndex == i ? Color.yellow : Color.HSVToRGB((i * 0.15f) % 1f, 0.8f, 1f);
            Handles.color = c;
            Handles.DrawWireDisc(group.spawnPosition, Vector3.up, group.spreadRadius);
            Handles.DrawSolidDisc(group.spawnPosition, Vector3.up, 0.35f);
            Handles.Label(
                group.spawnPosition + Vector3.up * 2f,
                $"W{targetWaveIndex + 1}-G{i + 1}: {group.enemyCount}x {group.enemyPoolType} | Delay: {FormatSpawnDelay(group.spawnDelay)}",
                GetSceneMainLabelStyle());
        }
    }

    private void DrawOverlay(Event e)
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 340, 82));
        GUILayout.Box(
            $"PLACEMENT MODE | Pattern: {pattern}" +
            (randomizeMode ? " | Randomize ON" : $" | Type: {enemyType}") +
            $"\nDelay: {FormatSpawnDelay(spawnDelay)}" +
            "\n[1] Melee  [2] Ranged  [3] Fly  |  ESC to cancel",
            GUILayout.Width(340)
        );
        GUILayout.EndArea();
        Handles.EndGUI();
        SceneView.RepaintAll();
    }

    // ─── PATTERN LOGIC ────────────────────────────────────────────────
    private List<Vector3> GetPatternPositions(Vector3 center)
    {
        var positions = new List<Vector3>();
        switch (pattern)
        {
            case PlacementPattern.Single:
                positions.Add(center);
                break;
            case PlacementPattern.Circle:
                for (int i = 0; i < patternCount; i++)
                {
                    float angle = (360f / patternCount) * i * Mathf.Deg2Rad;
                    positions.Add(center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * patternRadius);
                }
                break;
            case PlacementPattern.Line:
                float startOffset = -(patternCount - 1) * lineSpacing / 2f;
                for (int i = 0; i < patternCount; i++)
                    positions.Add(center + Vector3.right * (startOffset + i * lineSpacing));
                break;
            case PlacementPattern.FourCorners:
                positions.Add(center + new Vector3( patternRadius, 0,  patternRadius));
                positions.Add(center + new Vector3(-patternRadius, 0,  patternRadius));
                positions.Add(center + new Vector3( patternRadius, 0, -patternRadius));
                positions.Add(center + new Vector3(-patternRadius, 0, -patternRadius));
                break;
        }
        return positions;
    }

    private void PlacePattern(Vector3 center)
    {
        if (targetConfig == null || targetWaveIndex >= targetConfig.waves.Count) return;

        Undo.RecordObject(targetConfig, "Add Spawn Point(s)");

        List<Vector3> positions = GetPatternPositions(center);

        foreach (var pos in positions)
        {
            PoolType type = randomizeMode ? GetRandomAllowedType() : enemyType;

            targetConfig.waves[targetWaveIndex].enemyGroups.Add(new EnemyGroup
            {
                enemyPoolType = type,
                enemyCount    = enemyCount,
                spawnPosition = pos,
                spreadRadius  = spreadRadius,
                spawnDelay    = spawnDelay // Giữ nguyên delay theo setting, không tự động cộng thêm
            });
        }

        EditorUtility.SetDirty(targetConfig);
        Debug.Log($"✓ Added {positions.Count} spawn point(s) [{pattern}] to Wave {targetWaveIndex + 1}");
        SceneView.RepaintAll();
    }

    private PoolType GetRandomAllowedType()
    {
        return PickRandomAllowedType(allowMelee, allowRanged, allowFly, allowRandomBoss);
    }

    private static string FormatSpawnDelay(float delay)
    {
        return $"{delay:F1}s";
    }

    private static GUIStyle GetSceneDelayLabelStyle()
    {
        if (sceneDelayLabelStyle == null)
        {
            sceneDelayLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            sceneDelayLabelStyle.normal.textColor = Color.yellow;
        }

        return sceneDelayLabelStyle;
    }

    private static GUIStyle GetSceneMainLabelStyle()
    {
        if (sceneMainLabelStyle == null)
        {
            sceneMainLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            sceneMainLabelStyle.normal.textColor = Color.white;
        }

        return sceneMainLabelStyle;
    }

    internal static List<PoolType> BuildRandomAllowedTypes(bool allowMelee, bool allowRanged, bool allowFly, bool allowRandomBoss)
    {
        var allowed = new List<PoolType>();
        if (allowMelee)  allowed.Add(PoolType.Enemy_Melee);
        if (allowRanged) allowed.Add(PoolType.Enemy_Ranged);
        if (allowFly)    allowed.Add(PoolType.Enemy_Fly);
        if (allowRandomBoss)
        {
            allowed.Add(PoolType.Boss_Geo);
            allowed.Add(PoolType.Boss_Pyro);
            allowed.Add(PoolType.Boss_Electro);
        }

        return allowed;
    }

    internal static PoolType PickRandomAllowedType(bool allowMelee, bool allowRanged, bool allowFly, bool allowRandomBoss)
    {
        List<PoolType> allowed = BuildRandomAllowedTypes(allowMelee, allowRanged, allowFly, allowRandomBoss);

        if (allowed.Count == 0) return PoolType.Enemy_Melee;
        return allowed[Random.Range(0, allowed.Count)];
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
}
