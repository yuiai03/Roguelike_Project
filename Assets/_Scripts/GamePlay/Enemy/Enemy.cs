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
    
    // AI Randomization
    protected Vector3 moveOffset;
    protected float offsetChangeTimer;

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

        // Nếu Player chết thì khóa vận tốc đi lại ngang, chỉ cho rớt thẳng đứng hoặc knockback
        if (currentState == EnemyState.Idle || currentState == EnemyState.Dead)
        {
            velocity.x = 0;
            velocity.z = 0;
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
        
        // Nếu Player chết, Enemy đứng yên vĩnh viễn (đưa về trạng thái Idle)
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            if (currentState != EnemyState.Idle)
            {
                currentState = EnemyState.Idle;
            }
            return;
        }

        // Định kỳ thay đổi hướng offset một chút để quái vật tránh đi cùng 1 đường
        offsetChangeTimer -= Time.deltaTime;
        if (offsetChangeTimer <= 0f)
        {
            float maxOffset = enemyData.enemyType == EnemyType.Melee ? 1.5f : 3f;
            moveOffset = new Vector3(Random.Range(-maxOffset, maxOffset), 0f, Random.Range(-maxOffset, maxOffset));
            offsetChangeTimer = Random.Range(1.5f, 3f);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (enemyData.enemyType)
        {
            case EnemyType.Melee:
                UpdateMeleeAI(distanceToPlayer);
                break;
            case EnemyType.Ranged:
                UpdateRangedAI(distanceToPlayer);
                break;
        }
    }

    protected virtual void UpdateMeleeAI(float distanceToPlayer)
    {
        // Melee always chases player to get in attack range
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

    protected virtual void UpdateRangedAI(float distanceToPlayer)
    {
        // Ranged moves toward player to get into attack range, then shoots
        LookAtPlayer();

        if (distanceToPlayer > enemyData.attackRange)
        {
            // Too far, move closer to attack range
            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
        else
        {
            // In range, attack!
            currentState = EnemyState.Attacking;
            if (attackTimer <= 0f)
            {
                ShootProjectile();
            }
        }
    }

    protected virtual void MoveTowardsPlayer()
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

        LookAtPlayer();
    }

    protected virtual void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + moveOffset;
        Vector3 direction = (targetPos - transform.position).normalized;
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

    protected virtual void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        attackTimer = enemyData.shootCooldown;
        OnAttack?.Invoke();

        Vector3 spawnPosition = firePoint.position + Vector3.up * 1f;
        // Bắn ngang: flat hoá trục Y để đạn không rơi xuống
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

        // Hiển thị damage text tại hitPoint
        if (DamageTextSpawner.Instance != null && damage > 0)
        {
            DamageTextSpawner.Instance.Spawn(damage, hitPoint, isHeal: false, isPlayer: false, isCrit: false);
        }

        ApplyKnockback(hitDirection);
        StartCoroutine(FlashDamage());

        if (enemyData.IsDead())
        {
            Die();
        }
    }

    /// <summary> Cập nhật lại UI máu (được gọi sau khi WaveSpawner thay đổi chỉ số) </summary>
    public void RefreshHealthState()
    {
        if (enemyData != null)
        {
            enemyData.ResetHealth();
            OnHealthChanged?.Invoke(enemyData.currentHealth, enemyData.maxHealth);
        }
    }

    protected virtual void ApplyKnockback(Vector3 direction)
    {
        direction.y = 0f;
        if (direction == Vector3.zero) return; // không knockback nếu không có hướng
        knockbackVelocity = direction.normalized * enemyData.knockbackForce;
        knockbackTimer = enemyData.knockbackDuration;
    }

    /// <summary>Áp dụng knockback tùy chỉnh (dùng cho OrbitingBall, AoE, v.v.)</summary>
    public void Knockback(Vector3 direction, float force, float duration = 0.2f)
    {
        if (isDead) return;
        direction.y = 0f;
        if (direction == Vector3.zero) return;
        knockbackVelocity = direction.normalized * force;
        knockbackTimer    = duration;
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
    protected virtual void OnEnable()
    {
        // Reset states
        isDead = false;
        currentState = EnemyState.Idle;
        knockbackTimer = 0f;
        velocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;

        // Reset enemy data về giá trị gốc (và random offset lại chỉ số)
        if (enemyData != null)
        {
            enemyData.ResetData();
        }

        // Delay đánh nhịp đầu ngẫu nhiên để tránh quái bắn/đâm chung 1 lúc
        attackTimer = Random.Range(0.2f, 1f);
        offsetChangeTimer = 0f; // Sẽ random ngay ở frame đầu của UpdateAI

        // Reset material color
        if (enemyMaterial != null && !isDead)
        {
            enemyMaterial.color = originalColor;
        }

        // Enable controller
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Clear old event listeners để tránh memory leak
        OnDeath.RemoveAllListeners();

        // Notify health changed
        if (enemyData != null)
        {
            // Reset máu trước khi gọi event
            enemyData.ResetHealth();
            OnHealthChanged?.Invoke(enemyData.currentHealth, enemyData.maxHealth);
        }

        // Bật lại Animator (sửa lỗi Animation đơ khi tái sử dụng từ Pool)
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = true;
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

        // Only draw attack range (red) since detection range is no longer used
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
    }
}

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    Lunging,
    Retreating,
    Dead
}
