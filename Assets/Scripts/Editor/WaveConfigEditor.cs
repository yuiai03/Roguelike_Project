using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor cho WaveConfig - Visualize spawn points trong Scene view
/// </summary>
[CustomEditor(typeof(WaveConfig))]
public class WaveConfigEditor : Editor
{
    private int selectedWaveIndex = 0;
    private int selectedGroupIndex = -1;
    private bool showPreview = true;

    public override void OnInspectorGUI()
    {
        WaveConfig config = (WaveConfig)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Spawn Point Visualizer", EditorStyles.boldLabel);

        // Wave selector
        if (config.waves.Count > 0)
        {
            selectedWaveIndex = EditorGUILayout.IntSlider("Preview Wave", selectedWaveIndex, 0, config.waves.Count - 1);

            showPreview = EditorGUILayout.Toggle("Show Preview in Scene", showPreview);

            if (showPreview)
            {
                // Repaint scene view để update gizmos
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Select this WaveConfig in Inspector to see spawn points in Scene view.\n" +
                "Click on a spawn point in Scene view to select and move it.",
                MessageType.Info
            );

            // Group selector cho editing
            if (config.waves[selectedWaveIndex].enemyGroups.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Wave {selectedWaveIndex + 1} has {config.waves[selectedWaveIndex].enemyGroups.Count} groups", EditorStyles.miniLabel);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No waves configured. Add waves to visualize spawn points.", MessageType.Warning);
        }
    }

    private void OnSceneGUI()
    {
        WaveConfig config = (WaveConfig)target;

        if (!showPreview || config.waves.Count == 0 || selectedWaveIndex >= config.waves.Count)
            return;

        SimpleWaveData wave = config.waves[selectedWaveIndex];
        if (wave == null || wave.enemyGroups.Count == 0)
            return;

        // Draw all spawn points for selected wave
        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup group = wave.enemyGroups[i];
            DrawEnemyGroupGizmo(group, i, selectedGroupIndex == i);

            // Position handle để kéo thả
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(group.spawnPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Move Spawn Point");
                group.spawnPosition = newPosition;
                EditorUtility.SetDirty(config);
            }

            // Click để select group
            if (Handles.Button(group.spawnPosition, Quaternion.identity, 0.5f, 0.5f, Handles.SphereHandleCap))
            {
                selectedGroupIndex = i;
                Repaint();
            }
        }
    }

    private void DrawEnemyGroupGizmo(EnemyGroup group, int groupIndex, bool isSelected)
    {
        Color groupColor = Color.HSVToRGB((groupIndex * 0.2f) % 1f, 0.8f, 1f);

        if (isSelected)
        {
            groupColor = Color.yellow;
        }

        // Draw center sphere
        Handles.color = groupColor;
        Handles.DrawWireDisc(group.spawnPosition, Vector3.up, 0.5f);
        Handles.DrawLine(group.spawnPosition, group.spawnPosition + Vector3.up * 2f);

        // Draw spread radius
        Handles.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
        Handles.DrawWireDisc(group.spawnPosition, Vector3.up, group.spreadRadius);

        // Draw filled circle at spread radius
        Handles.DrawSolidDisc(group.spawnPosition, Vector3.up, 0.3f);

        // Draw preview spawn positions
        Handles.color = groupColor;
        int previewCount = Mathf.Min(group.enemyCount, 8);
        for (int i = 0; i < previewCount; i++)
        {
            float angle = (360f / previewCount) * i * Mathf.Deg2Rad;
            float dist = group.spreadRadius * 0.6f;
            Vector3 pos = group.spawnPosition + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Handles.DrawWireDisc(pos, Vector3.up, 0.3f);
        }

        // Draw label
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = isSelected ? Color.yellow : Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        Handles.Label(
            group.spawnPosition + Vector3.up * 2.5f,
            $"Group {groupIndex + 1}\n{group.enemyCount}x {group.enemyPoolType}\nDelay: {group.spawnDelay}s",
            style
        );
    }
}
