using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuffCardConfig))]
public class BuffCardConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Card Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Buff Settings", EditorStyles.boldLabel);
        SerializedProperty buffTypeProp = serializedObject.FindProperty("buffType");
        EditorGUILayout.PropertyField(buffTypeProp);
        BuffType currentType = (BuffType)buffTypeProp.enumValueIndex;

        if (UsesAttackDamageMultiplier(currentType))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackDamageMultiplier"));
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("value"));
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLevel"));

        EditorGUILayout.Space();

        switch (currentType)
        {
            case BuffType.MultiShot:
                EditorGUILayout.LabelField("MultiShot Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK cho mỗi tia  |  Shot Count = Số tia cộng thêm mỗi lần pick", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shotCount"));
                break;

            case BuffType.AoEExplosion:
                EditorGUILayout.LabelField("AoEExplosion Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK cho sát thương nổ  |  AoE Radius = Phạm vi nổ", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeRadius"));
                break;

            case BuffType.OrbitingBall:
                EditorGUILayout.LabelField("OrbitingBall Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK cho mỗi bóng  |  Ball Count = Số bóng spawn thêm", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ballCount"));
                break;

            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
                EditorGUILayout.LabelField("Spirit Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK cho đạn tinh linh.", MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static bool UsesAttackDamageMultiplier(BuffType buffType)
    {
        switch (buffType)
        {
            case BuffType.MultiShot:
            case BuffType.AoEExplosion:
            case BuffType.OrbitingBall:
            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
                return true;

            default:
                return false;
        }
    }
}
