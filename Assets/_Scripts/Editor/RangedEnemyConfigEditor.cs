using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RangedEnemyConfig))]
public class RangedEnemyConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Ranged Enemy Configuration\n\n" +
            "• Shoots projectiles at player\n" +
            "• Moves to get into attack range then shoots\n" +
            "• projectileDamage: Damage per projectile\n" +
            "• shootCooldown: Time between shots\n" +
            "• projectileSpeed: How fast projectiles travel\n" +
            "• projectileLifetime: How long projectiles live",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}
