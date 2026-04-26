using UnityEditor;
using UnityEngine;

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
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK for each extra shot. Shot Count = number of extra shots per pick.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shotCount"));
                break;

            case BuffType.AoEExplosion:
                EditorGUILayout.LabelField("AoEExplosion Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK for the explosion. AoE Radius = explosion size.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeRadius"));
                break;

            case BuffType.OrbitingBall:
                EditorGUILayout.LabelField("OrbitingBall Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK for each orb. Ball Count = extra orbs per pick.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ballCount"));
                break;

            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
                EditorGUILayout.LabelField("Spirit Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK used by the spirit projectile.", MessageType.Info);
                break;

            case BuffType.SpiritHealing:
                EditorGUILayout.LabelField("Spirit Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK converted into healing per tick.", MessageType.Info);
                break;

            case BuffType.SpiritTripleShot:
                EditorGUILayout.LabelField("Spirit Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Attack Damage Multiplier = % ATK used by each projectile in the triple-shot volley.", MessageType.Info);
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
            case BuffType.SpiritHealing:
            case BuffType.SpiritTripleShot:
                return true;

            default:
                return false;
        }
    }
}
