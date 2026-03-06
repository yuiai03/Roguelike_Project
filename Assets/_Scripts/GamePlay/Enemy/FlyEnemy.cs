using UnityEngine;
using System.Collections;

public class FlyEnemy : Enemy
{
    [Header("Fly Visuals")]
    [SerializeField] private Transform propellerTransform;
    [SerializeField] private float propellerSpeed = 720f;
    
    private int currentBurstCount = 0;
    private bool isShootingBurst = false;
    private float burstTimer = 0f;
    private float bobbingTimer = 0f;
    private bool isPreparingToShoot = false;
    private Vector3 initialModelLocalPos;
    private Vector3 initialModelScale = Vector3.one;
    
    protected override void Awake()
    {
        base.Awake();
        if (modelTransform != null)
        {
            initialModelLocalPos = modelTransform.localPosition;
            initialModelScale = modelTransform.localScale;
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (isDead) return;
        
        if (propellerTransform != null)
        {
            float currentPropellerSpeed = (isPreparingToShoot || isShootingBurst) ? propellerSpeed * 3f : propellerSpeed;
            propellerTransform.Rotate(Vector3.up * currentPropellerSpeed * Time.deltaTime, Space.Self);
        }
        
        if (currentState == EnemyState.Chasing || currentState == EnemyState.Retreating)
        {
            bobbingTimer += Time.deltaTime * enemyData.bobFrequency;
            if (modelTransform != null)
            {
                float newY = initialModelLocalPos.y + Mathf.Sin(bobbingTimer) * enemyData.bobAmplitude;
                modelTransform.localPosition = new Vector3(initialModelLocalPos.x, newY, initialModelLocalPos.z);
            }
        }
        else if (modelTransform != null)
        {
            modelTransform.localPosition = Vector3.Lerp(modelTransform.localPosition, initialModelLocalPos, Time.deltaTime * 5f);
        }
    }
    
    protected override void UpdateAI()
    {
        if (player == null) return;
        
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            if (currentState != EnemyState.Idle) currentState = EnemyState.Idle;
            return;
        }
        
        offsetChangeTimer -= Time.deltaTime;
        if (offsetChangeTimer <= 0f)
        {
            moveOffset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            offsetChangeTimer = Random.Range(1.5f, 3f);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        LookAtPlayer();

        if (isPreparingToShoot) return;

        if (isShootingBurst)
        {
            currentState = EnemyState.Attacking;
            
            burstTimer -= Time.deltaTime;
            if (burstTimer <= 0f)
            {
                ShootProjectile();
                currentBurstCount++;
                
                if (currentBurstCount >= enemyData.burstCount)
                {
                    isShootingBurst = false;
                    attackTimer = enemyData.shootCooldown;
                }
                else
                {
                    burstTimer = enemyData.burstDelay;
                }
            }
        }
        else
        {
            if (distanceToPlayer > enemyData.stopDistance)
            {
                currentState = EnemyState.Chasing;
                MoveTowardsPlayer();
            }
            else
            {
                currentState = EnemyState.Idle;
                if (attackTimer <= 0f)
                {
                    StartCoroutine(PrepareToBurst());
                }
            }
        }
    }
    
    private IEnumerator PrepareToBurst()
    {
        isPreparingToShoot = true;
        currentState = EnemyState.Attacking;

        float prepDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < prepDuration)
        {
            if (isDead) yield break;
            
            elapsed += Time.deltaTime;
            float scaleMultiplier = 1f + (elapsed / prepDuration) * 0.2f;
            if (modelTransform != null)
            {
                modelTransform.localScale = initialModelScale * scaleMultiplier;
            }
            
            yield return null;
        }

        if (isDead) yield break;

        if (modelTransform != null)
        {
            modelTransform.localScale = initialModelScale;
        }

        isPreparingToShoot = false;
        StartBurst();
    }

    private void StartBurst()
    {
        isShootingBurst = true;
        currentBurstCount = 0;
        burstTimer = 0f;
    }

    protected override void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        OnAttack?.Invoke();

        Vector3 spawnPosition = firePoint.position + Vector3.up * 1f;
        Vector3 targetPosition = player.position + Vector3.up * 1f + moveOffset * 0.3f;
        Vector3 rawDir = targetPosition - spawnPosition;
        rawDir.y = 0f;
        Vector3 direction = rawDir.normalized;
        GameObject projectile = ObjectPool.Instance.Spawn(PoolType.EnemyProjectile, spawnPosition, Quaternion.LookRotation(direction));

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(enemyData.projectileDamage, enemyData.projectileSpeed,
                enemyData.projectileLifetime, direction, playerLayer, gameObject);
        }
    }

    protected override void MoveTowardsPlayer()
    {
        Vector3 targetPos = player.position + moveOffset;
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0f; 
        Vector3 moveVector = direction * enemyData.moveSpeed * Time.deltaTime;

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
        }
        else
        {
            transform.position += moveVector;
        }
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        isShootingBurst = false;
        isPreparingToShoot = false;
        currentBurstCount = 0;
        burstTimer = 0f;
        bobbingTimer = 0f;
        
        if (modelTransform != null && initialModelLocalPos != Vector3.zero)
        {
            modelTransform.localPosition = initialModelLocalPos;
            modelTransform.localScale = initialModelScale;
        }
    }
}
