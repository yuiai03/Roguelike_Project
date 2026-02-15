using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ExpDropper))]
[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Model Reference")]
    [SerializeField] protected Transform modelTransform;

    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected GameObject projectilePrefab;

    [Header("Ground Check")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected LayerMask groundMask;

    [Header("Visual Feedback")]
    [SerializeField] protected Color damageFlashColor = Color.red;
    [SerializeField] protected float flashDuration = 0.1f;

    [Header("Player Detection")]
    [SerializeField] protected LayerMask playerLayer;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamageEvent;
    public UnityEvent OnAttack;
    public UnityEvent<float, float> OnHealthChanged;

    // Pool management
    private PoolType poolType = PoolType.None;

    protected Renderer enemyRenderer;
    protected EnemyData enemyData;
    protected Transform player;
    protected Material enemyMaterial;
    protected Color originalColor;
    protected bool isDead;
    protected bool isGrounded;
    protected float attackTimer;
    protected float knockbackTimer;
    protected Vector3 knockbackVelocity;
    protected Vector3 velocity;
    protected CharacterController controller;

    protected EnemyState currentState = EnemyState.Idle;

    protected virtual void Awake()
    {
        controller = GetComponent<CharacterController>();
        enemyData = GetComponent<EnemyData>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        if (modelTransform == null)
        {
            Renderer childRenderer = GetComponentInChildren<Renderer>();
            if (childRenderer != null && childRenderer.transform != transform)
            {
                modelTransform = childRenderer.transform;
            }
        }

        if (enemyRenderer == null)
        {
            enemyRenderer = modelTransform.GetComponentInChildren<Renderer>();
            enemyMaterial = enemyRenderer.material;
            originalColor = enemyMaterial.color;
        }

        if (firePoint == null)
            firePoint = transform;

        enemyData.ResetHealth();
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        OnHealthChanged?.Invoke(enemyData.currentHealth, enemyData.maxHealth);
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, enemyData.groundDistance, groundMask);
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            Vector3 knockbackMove = knockbackVelocity * Time.deltaTime;
            knockbackMove.y = 0f;
            if (controller != null && controller.enabled)
            {
                controller.Move(knockbackMove);
            }
            else
            {
                transform.position += knockbackMove;
            }
        }
        else
        {
            UpdateAI();
        }

        velocity.y += enemyData.gravity * Time.deltaTime;
        if (controller != null && controller.enabled)
        {
            controller.Move(velocity * Time.deltaTime);
        }
    }

    protected virtual void UpdateAI()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool hasLineOfSight = CheckLineOfSight();

        switch (enemyData.enemyType)
        {
            case EnemyType.Melee:
                UpdateMeleeAI(distanceToPlayer, hasLineOfSight);
                break;
            case EnemyType.Ranged:
                UpdateRangedAI(distanceToPlayer, hasLineOfSight);
                break;
        }
    }

    protected virtual void UpdateMeleeAI(float distanceToPlayer, bool hasLineOfSight)
    {
        if (!hasLineOfSight)
        {
            currentState = EnemyState.Idle;
            return;
        }

        if (distanceToPlayer <= enemyData.detectionRange)
        {
            if (distanceToPlayer > enemyData.attackRange)
            {
                currentState = EnemyState.Chasing;
                MoveTowardsPlayer();
            }
            else
            {
                currentState = EnemyState.Attacking;
            }
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    protected virtual void UpdateRangedAI(float distanceToPlayer, bool hasLineOfSight)
    {
        if (!hasLineOfSight)
        {
            currentState = EnemyState.Idle;
            return;
        }

        float optimalRange = enemyData.attackRange * 0.7f;

        if (distanceToPlayer <= enemyData.detectionRange)
        {
            LookAtPlayer();

            if (distanceToPlayer < optimalRange)
            {
                currentState = EnemyState.Idle;
                MoveAwayFromPlayer();
            }
            else if (distanceToPlayer > enemyData.attackRange)
            {
                currentState = EnemyState.Chasing;
                MoveTowardsPlayer();
            }
            else
            {
                currentState = EnemyState.Attacking;
                if (attackTimer <= 0f)
                {
                    ShootProjectile();
                }
            }
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    protected virtual void UpdateTankAI(float distanceToPlayer, bool hasLineOfSight)
    {
        UpdateMeleeAI(distanceToPlayer, hasLineOfSight);
    }

    protected virtual void UpdateFastAI(float distanceToPlayer, bool hasLineOfSight)
    {
        UpdateMeleeAI(distanceToPlayer, hasLineOfSight);
    }

    protected virtual void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
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

        LookAtPlayer();
    }

    protected virtual void MoveAwayFromPlayer()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        direction.y = 0f;
        Vector3 moveVector = direction * enemyData.moveSpeed * 0.5f * Time.deltaTime;

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
        }
        else
        {
            transform.position += moveVector;
        }

        LookAtPlayer();
    }

    protected virtual void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            if (modelTransform != null)
            {
                modelTransform.rotation = Quaternion.Slerp(
                    modelTransform.rotation,
                    targetRotation,
                    enemyData.rotationSpeed * Time.deltaTime
                );
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    enemyData.rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    protected virtual bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;
        Vector3 startPoint = transform.position;

        bool hasHit = Physics.Raycast(startPoint, direction.normalized, out RaycastHit hit, distance, playerLayer);

        if (hasHit)
        {
            bool canSeePlayer = hit.collider.CompareTag("Player");
            Debug.DrawRay(startPoint, direction.normalized * hit.distance, canSeePlayer ? Color.green : Color.red);
            return canSeePlayer;
        }

        Debug.DrawRay(startPoint, direction.normalized * distance, Color.yellow);
        return false;
    }

    protected virtual void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        attackTimer = enemyData.shootCooldown;
        OnAttack?.Invoke();

        Vector3 direction = (player.position - firePoint.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(enemyData.projectileDamage, enemyData.projectileSpeed,
                enemyData.projectileLifetime, direction, playerLayer, gameObject);
        }
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        if (isDead || attackTimer > 0f) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (enemyData.enemyType == EnemyType.Melee)
            {
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead())
                {
                    Vector3 hitDirection = (collision.transform.position - transform.position).normalized;
                    damageable.TakeDamage(enemyData.contactDamage, collision.GetContact(0).point, hitDirection);
                    attackTimer = enemyData.attackCooldown;
                    OnAttack?.Invoke();
                }
            }
        }
    }

    public virtual void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        enemyData.TakeDamage(damage);
        OnTakeDamageEvent?.Invoke();
        OnHealthChanged?.Invoke(enemyData.currentHealth, enemyData.maxHealth);

        ApplyKnockback(hitDirection);
        StartCoroutine(FlashDamage());

        if (enemyData.IsDead())
        {
            Die();
        }
    }

    protected virtual void ApplyKnockback(Vector3 direction)
    {
        direction.y = 0f;
        knockbackVelocity = direction.normalized * enemyData.knockbackForce;
        knockbackTimer = enemyData.knockbackDuration;
    }

    protected virtual System.Collections.IEnumerator FlashDamage()
    {
        if (enemyRenderer != null)
        {
            enemyMaterial.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            if (!isDead)
                enemyMaterial.color = originalColor;
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = EnemyState.Dead;
        OnDeath?.Invoke();

        // Return to pool sau 0.5s để animation chạy xong
        if (poolType != PoolType.None && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.DespawnAfterDelay(gameObject, poolType, 0.5f);
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    /// <summary>
    /// Set pool type cho enemy (gọi bởi spawner)
    /// </summary>
    public void SetPoolType(PoolType type)
    {
        poolType = type;
    }

    /// <summary>
    /// Reset enemy khi spawn từ pool
    /// </summary>
    private void OnEnable()
    {
        isDead = false;
        currentState = EnemyState.Idle;
        knockbackTimer = 0f;
        attackTimer = 0f;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetCurrentHealth()
    {
        return enemyData.currentHealth;
    }

    public float GetMaxHealth()
    {
        return enemyData.maxHealth;
    }

    public EnemyData GetEnemyData()
    {
        return enemyData;
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    public Transform GetModelTransform()
    {
        return modelTransform;
    }

    protected virtual void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            if (enemyData != null)
            {
                Gizmos.DrawWireSphere(groundCheck.position, enemyData.groundDistance);
            }
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
    }
}

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    Dead
}
