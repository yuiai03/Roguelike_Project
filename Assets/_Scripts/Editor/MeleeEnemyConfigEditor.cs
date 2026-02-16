using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor cho MeleeEnemyConfig
/// </summary>
[CustomEditor(typeof(MeleeEnemyConfig))]
public class MeleeEnemyConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Melee Enemy Configuration\n\n" +
            "• Attacks by contact with player\n" +
            "• Chases player to get in attack range\n" +
            "• Use contactDamage for damage amount\n" +
            "• attackCooldown controls time between attacks",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}
