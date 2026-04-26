using UnityEngine;
using System.Collections.Generic;

public class OrbitingBallManager : MonoBehaviour
{
    [Header("Ball Prefab")]
    [Tooltip("Prefab của quả cầu. Phải có: Collider (Is Trigger = true) + OrbitingBall script.")]
    [SerializeField] private GameObject ballPrefab;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float baseOrbitSpeed = 120f;
    [SerializeField] private float heightOffset = 1f;

    private readonly List<GameObject> balls = new List<GameObject>();
    private float masterAngle = 0f;   
    private PlayerData playerData;

    private void Awake()
    {
        playerData = GetComponent<PlayerData>();
    }

    private void Update()
    {
        CleanupInactiveBalls();

        int activeBallCount = balls.Count;
        if (activeBallCount == 0) return;

        masterAngle -= baseOrbitSpeed * Time.deltaTime;
        if (masterAngle < 0f) masterAngle += 360f;

        float step = 360f / activeBallCount;

        for (int i = 0; i < activeBallCount; i++)
        {
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

    public void AddBall(float atkMultiplier = 1f)
    {
        GameObject ball = ObjectPool.Instance.Spawn(PoolType.OrbitingBall, transform.position);
        if (ball == null)
        {

            ball = CreateBall();
        }

        OrbitingBall script = ball.GetComponent<OrbitingBall>();
        if (script != null)
        {
            if (playerData == null)
            {
                playerData = GetComponent<PlayerData>();
            }

            script.Initialize(playerData, atkMultiplier);
        }

        balls.Add(ball);
    }

    public void RemoveBall()
    {
        CleanupInactiveBalls();
        if (balls.Count == 0) return;
        int last = balls.Count - 1;
        if (balls[last] != null)
            ObjectPool.Instance.Despawn(balls[last], PoolType.OrbitingBall);
        balls.RemoveAt(last);
    }

    public void RemoveAll()
    {
        CleanupInactiveBalls();

        foreach (GameObject b in balls)
            if (b != null)
                ObjectPool.Instance.Despawn(b, PoolType.OrbitingBall);
        balls.Clear();
    }

    public int GetBallCount()
    {
        CleanupInactiveBalls();
        return balls.Count;
    }

    private GameObject CreateBall()
    {
        if (ballPrefab != null)
            return Instantiate(ballPrefab);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "OrbitingBall";
        go.transform.localScale = Vector3.one * 0.45f;

        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer rend = go.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard")); 
        mat.color = new Color(0.2f, 0.6f, 1f);
        rend.material = mat;

        go.AddComponent<OrbitingBall>();

        return go;
    }

    private void CleanupInactiveBalls()
    {
        balls.RemoveAll(ball => ball == null || !ball.activeInHierarchy);
    }

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
