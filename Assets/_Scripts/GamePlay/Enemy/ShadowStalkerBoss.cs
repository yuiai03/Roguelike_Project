using UnityEngine;
using System.Collections;

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

        if (currentPhase == 1 && dashTimer <= 0f && player != null)
        {
            StartCoroutine(DashToPlayer());
        }

        if (currentPhase == 2 && dashTimer <= 0f && player != null)
        {
            StartCoroutine(DashToPlayer());
        }

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

            isInvisible = true;
            if (bossRenderer != null) bossRenderer.enabled = false;
            Debug.Log("[ShadowStalker] Tàng hình...");
            yield return new WaitForSeconds(invisDuration);

            if (player != null && !isDead)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                transform.position = player.position + offset;
                Debug.Log("[ShadowStalker] Teleport cạnh player!");
            }

            if (bossRenderer != null) bossRenderer.enabled = true;
            isInvisible = false;
        }
    }

    protected override void OnPhase3()
    {
        if (cloneSpawned) return;
        cloneSpawned = true;

        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
            ObjectPool.Instance?.Spawn(PoolType.MeleeEnemy, transform.position + offset, Quaternion.identity);
        }
        Debug.Log("[ShadowStalker] Phase 3: Clone + bắn 4 hướng!");
    }
}
