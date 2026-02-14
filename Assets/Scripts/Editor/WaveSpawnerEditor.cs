using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaveSpawner))]
public class WaveSpawnerEditor : Editor
{
    private WaveSpawner spawner;
    private WaveConfig config;
    private int previewWave = 1;

    void OnEnable()
    {
        spawner = (WaveSpawner)target;

        SerializedProperty configProp = serializedObject.FindProperty("waveConfig");
        if (configProp.objectReferenceValue != null)
        {
            config = (WaveConfig)configProp.objectReferenceValue;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Preview controls
        if (config != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Wave Preview", EditorStyles.boldLabel);

            previewWave = EditorGUILayout.IntSlider("Preview Wave", previewWave, 1, config.waves.Count);

            if (GUILayout.Button("Refresh Gizmos"))
            {
                SceneView.RepaintAll();
            }

            SimpleWaveData wave = config.GetWave(previewWave);
            if (wave != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Wave {previewWave}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Groups: {wave.enemyGroups.Count}");

                int totalEnemies = 0;
                foreach (var group in wave.enemyGroups)
                {
                    totalEnemies += group.enemyCount;
                }
                EditorGUILayout.LabelField($"Total Enemies: {totalEnemies}");
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Current Wave: {spawner.GetCurrentWave()} / {spawner.GetTotalWaves()}");
            EditorGUILayout.LabelField($"Enemies: {spawner.GetActiveEnemyCount()} / {spawner.GetTotalEnemies()}");
            EditorGUILayout.LabelField($"Wave Active: {spawner.IsWaveActive()}");

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Force Next Wave", GUILayout.Height(30)))
            {
                spawner.ForceNextWave();
            }

            if (GUILayout.Button("Kill All Enemies", GUILayout.Height(25)))
            {
                spawner.KillAllEnemies();
            }

            EditorGUILayout.EndVertical();
        }
    }

    void OnSceneGUI()
    {
        if (config == null) return;

        SimpleWaveData wave = config.GetWave(previewWave);
        if (wave == null) return;

        // Draw moveable handles for group positions
        int groupIndex = 0;
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            Color groupColor = Color.HSVToRGB((groupIndex * 0.2f) % 1f, 0.8f, 1f);
            Handles.color = groupColor;

            // Draw position handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(group.spawnPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Move Group Position");
                group.spawnPosition = newPos;
                EditorUtility.SetDirty(config);
            }

            // Draw sphere
            Handles.SphereHandleCap(0, group.spawnPosition, Quaternion.identity, 1f, EventType.Repaint);

            // Draw radius handle
            Handles.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
            EditorGUI.BeginChangeCheck();
            float newRadius = Handles.RadiusHandle(Quaternion.identity, group.spawnPosition, group.spreadRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Group Radius");
                group.spreadRadius = Mathf.Max(0f, newRadius);
                EditorUtility.SetDirty(config);
            }

            groupIndex++;
        }
    }
}
