using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Wave 20 — Shadow Stalker.
/// P1: dash + melee nhanh
/// P2 (60%): tàng hình 2s → teleport cạnh player
/// P3 (30%): 2 clone giả + bắn 4 hướng
/// </summary>
public class ShadowStalkerBoss : BossEnemy
{
    [Header("Shadow Stalker")]
    [SerializeField] private float dashSpeed     = 20f;
    [SerializeField] private float dashCooldown  = 2.5f;
    [SerializeField] private float invisDuration = 2f;

    private float dashTimer;
    private bool isInvisible;
    private bool cloneSpawned;
    private Renderer bossRenderer;

    protected override void Awake()
    {
        base.Awake();
        bossRenderer = GetComponentInChildren<Renderer>();
        dashTimer = dashCooldown;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || isInvisible) return;

        dashTimer -= Time.deltaTime;

        // P1: dash về phía player + melee
        if (currentPhase == 1 && dashTimer <= 0f && player != null)
        {
            StartCoroutine(DashToPlayer());
        }

        // P2: dash nhanh hơn
        if (currentPhase == 2 && dashTimer <= 0f && player != null)
        {
            StartCoroutine(DashToPlayer());
        }

        // P3: bắn 4 hướng
        if (currentPhase == 3 && bossShootTimer <= 0f)
        {
            ShootRadial(4);
        }
    }

    private IEnumerator DashToPlayer()
    {
        dashTimer = dashCooldown * (currentPhase == 2 ? 0.6f : 1f);
        if (player == null) yield break;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        float elapsed = 0f;
        float dashTime = 0.3f;

        while (elapsed < dashTime && !isDead)
        {
            elapsed += Time.deltaTime;
            Vector3 move = dir * dashSpeed * Time.deltaTime;
            if (controller != null && controller.enabled)
                controller.Move(move);
            else
                transform.position += move;
            yield return null;
        }
    }

    protected override void OnPhase2()
    {
        StartCoroutine(StealthTeleportLoop());
    }

    private IEnumerator StealthTeleportLoop()
    {
        while (currentPhase >= 2 && !isDead)
        {
            yield return new WaitForSeconds(invisDuration + 1f);
            if (isDead) yield break;

            // Tàng hình
            isInvisible = true;
            if (bossRenderer != null) bossRenderer.enabled = false;
            Debug.Log("[ShadowStalker] Tàng hình...");
            yield return new WaitForSeconds(invisDuration);

            // Teleport cạnh player
            if (player != null && !isDead)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                transform.position = player.position + offset;
                Debug.Log("[ShadowStalker] Teleport cạnh player!");
            }

            // Hiện ra
            if (bossRenderer != null) bossRenderer.enabled = true;
            isInvisible = false;
        }
    }

    protected override void OnPhase3()
    {
        if (cloneSpawned) return;
        cloneSpawned = true;
        // Tạo 2 clone giả bằng cách spawn dummy enemy
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
            ObjectPool.Instance?.Spawn(PoolType.MeleeEnemy, transform.position + offset, Quaternion.identity);
        }
        Debug.Log("[ShadowStalker] Phase 3: Clone + bắn 4 hướng!");
    }
}
