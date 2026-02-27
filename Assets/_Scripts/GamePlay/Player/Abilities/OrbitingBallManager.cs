using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý các quả cầu xoay quanh player.
/// - Gọi AddBall() để thêm quả cầu mới (từ buff card).
/// - Các quả cầu tự động phân bố đều nhau theo công thức 360 / số quả cầu.
/// - Nên đặt component này trên GameObject của Player.
/// </summary>
public class OrbitingBallManager : MonoBehaviour
{
    [Header("Ball Prefab")]
    [Tooltip("Prefab của quả cầu. Phải có: Collider (Is Trigger = true) + OrbitingBall script.")]
    [SerializeField] private GameObject ballPrefab;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 120f;   // độ / giây
    [SerializeField] private float heightOffset = 1f;   // độ cao so với pivot player

    [Header("Ball Stats")]
    [SerializeField] private float damage = 20f;

    // ─────────────────────────────────────────────────────────
    private readonly List<GameObject> balls = new List<GameObject>();
    private float masterAngle = 0f;   // góc quay chung, tăng liên tục

    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (balls.Count == 0) return;

        masterAngle -= orbitSpeed * Time.deltaTime;
        if (masterAngle < 0f) masterAngle += 360f;

        float step = 360f / balls.Count;

        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i] == null) continue;

            float angleDeg = masterAngle + i * step;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * orbitRadius,
                heightOffset,
                Mathf.Sin(angleRad) * orbitRadius
            );

            balls[i].transform.position = transform.position + offset;
        }
    }

    // ─────────────────────────────────────────────────────────

    /// <summary>Thêm 1 quả cầu. Tự động phân bố lại vị trí đều nhau.</summary>
    public void AddBall(float damageOverride = -1f)
    {
        GameObject ball = ObjectPool.Instance.Spawn(PoolType.OrbitingBall, transform.position);
        if (ball == null)
        {
            // fallback nếu pool chưa được setup
            ball = CreateBall();
        }

        OrbitingBall script = ball.GetComponent<OrbitingBall>();
        if (script != null)
        {
            float dmg = damageOverride > 0f ? damageOverride : damage;
            script.Initialize(dmg);
        }

        balls.Add(ball);
    }

    /// <summary>Xoá quả cầu cuối cùng.</summary>
    public void RemoveBall()
    {
        if (balls.Count == 0) return;
        int last = balls.Count - 1;
        if (balls[last] != null)
            ObjectPool.Instance.Despawn(balls[last], PoolType.OrbitingBall);
        balls.RemoveAt(last);
    }

    /// <summary>Xoá tất cả quả cầu.</summary>
    public void RemoveAll()
    {
        foreach (GameObject b in balls)
            if (b != null)
                ObjectPool.Instance.Despawn(b, PoolType.OrbitingBall);
        balls.Clear();
    }

    public int GetBallCount() => balls.Count;

    // ─────────────────────────────────────────────────────────

    private GameObject CreateBall()
    {
        if (ballPrefab != null)
            return Instantiate(ballPrefab);

        // Fallback: tạo sphere primitive khi chưa gán prefab
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "OrbitingBall";
        go.transform.localScale = Vector3.one * 0.45f;

        // Collider phải là trigger
        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        // Thêm Rigidbody kinematic: bắt buộc để OnTriggerEnter/Stay hoạt động
        // (Unity yêu cầu ít nhất 1 trong 2 object có Rigidbody cho trigger callbacks)
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Tô màu xanh lam
        Renderer rend = go.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard")); // fallback nếu không dùng URP
        mat.color = new Color(0.2f, 0.6f, 1f);
        rend.material = mat;

        // Thêm OrbitingBall script
        go.AddComponent<OrbitingBall>();

        return go;
    }

    // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * heightOffset;
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);

        const int seg = 64;
        Vector3 prev = center + new Vector3(orbitRadius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float rad = (360f / seg * i) * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * orbitRadius, 0f, Mathf.Sin(rad) * orbitRadius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
