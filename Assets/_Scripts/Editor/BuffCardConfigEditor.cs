using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor cho BuffCardConfig.
/// Hiển thị field phụ tuỳ theo buffType được chọn.
/// </summary>
[CustomEditor(typeof(BuffCardConfig))]
public class BuffCardConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Card Info ────────────────────────────────────────────
        EditorGUILayout.LabelField("Card Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));

        EditorGUILayout.Space();

        // ── Buff Settings ─────────────────────────────────────────
        EditorGUILayout.LabelField("Buff Settings", EditorStyles.boldLabel);
        SerializedProperty buffTypeProp = serializedObject.FindProperty("buffType");
        EditorGUILayout.PropertyField(buffTypeProp);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("value"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));

        EditorGUILayout.Space();

        // ── Extra Settings theo loại buff ─────────────────────────
        BuffType currentType = (BuffType)buffTypeProp.enumValueIndex;

        switch (currentType)
        {
            case BuffType.MultiShot:
                EditorGUILayout.LabelField("MultiShot Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Value = Damage mỗi đạn  |  Shot Count = Số đạn bắn thêm", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shotCount"));
                break;

            case BuffType.AoEExplosion:
                EditorGUILayout.LabelField("AoEExplosion Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Value = Damage AoE (0 = dùng % mặc định)  |  AoE Radius = Phạm vi nổ", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeRadius"));
                break;

            case BuffType.OrbitingBall:
                EditorGUILayout.LabelField("OrbitingBall Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Value = Damage mỗi bóng  |  Ball Count = Số bóng spawn thêm", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ballCount"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
