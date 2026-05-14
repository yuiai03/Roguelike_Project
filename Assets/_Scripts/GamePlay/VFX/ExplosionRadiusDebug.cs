using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExplosionRadiusDebug : MonoBehaviour
{
    [SerializeField, Min(0f)] private float radius = 3f;
    [SerializeField] private Vector3 centerOffset = Vector3.zero;
    [SerializeField] private Color color = new Color(1f, 0.55f, 0f, 0.8f);
    [SerializeField] private bool drawLabel = true;
    [SerializeField] private bool drawSphere = true;
    [SerializeField, Min(12)] private int circleSegments = 64;

    public float Radius => radius;

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position + centerOffset;
        Color previousColor = Gizmos.color;
        Gizmos.color = color;

        DrawCircle(center, radius, circleSegments);

        if (drawSphere)
        {
            Gizmos.DrawWireSphere(center, radius);
        }

        Gizmos.DrawLine(center + Vector3.left * radius, center + Vector3.right * radius);
        Gizmos.DrawLine(center + Vector3.forward * radius, center + Vector3.back * radius);

        Gizmos.color = previousColor;

#if UNITY_EDITOR
        if (drawLabel)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color;
            Handles.Label(center + Vector3.up * 0.4f, $"Explosion Radius: {radius:F1}", style);
        }
#endif
    }

    private static void DrawCircle(Vector3 center, float circleRadius, int segments)
    {
        if (circleRadius <= 0f || segments < 3)
        {
            return;
        }

        Vector3 previousPoint = center + Vector3.right * circleRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * circleRadius, 0f, Mathf.Sin(angle) * circleRadius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
