using UnityEngine;

public class LawaChurl_Electro : Enemy
{
    private float bombDropDuration = 2f;
    private float spawnHeight = 15f; 

    private bool isAttackingAnimation;

    protected override void OnEnable()
    {
        base.OnEnable();
        isAttackingAnimation = false;
        if (enemyData != null)
        {
            enemyData.enemyType = EnemyType.Ranged;
        }

        LawaChurlElectroConfig config = enemyData.GetConfig<LawaChurlElectroConfig>();
        if (config != null)
        {
            bombDropDuration = config.bombDropDuration;
            spawnHeight = config.bombSpawnHeight;
        }
    }

    protected override void UpdateRangedAI(float distanceToPlayer)
    {
        if (isAttackingAnimation)
        {
            LookAtPlayer();
            velocity.x = 0;
            velocity.z = 0;
            return;
        }

        LookAtPlayer();

        if (distanceToPlayer > enemyData.attackRange || attackTimer > 0f)
        {
            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
        else
        {
            currentState = EnemyState.Attacking;
            StartCoroutine(PerformAttackCoroutine());
        }
    }

    private System.Collections.IEnumerator PerformAttackCoroutine()
    {
        isAttackingAnimation = true;
        velocity.x = 0;
        velocity.z = 0;
        ShootProjectile();
        yield return new WaitForSeconds(1.0f);
        isAttackingAnimation = false;
    }

    protected override void ShootProjectile() // Ném bom
    {
        attackTimer = enemyData.shootCooldown;
        OnAttack?.Invoke();

        Vector3 targetPos = player.position;

        // Spawn Warning Circle at Player position
        GameObject warningObj = ObjectPool.Instance.Spawn(PoolType.WarningCircle, targetPos, Quaternion.identity);
        if (warningObj != null)
        {
            WarningCircle warningCircle = warningObj.GetComponent<WarningCircle>();
            if (warningCircle != null)
            {
                warningCircle.OnWarningComplete.RemoveAllListeners();
                warningCircle.OnWarningComplete.AddListener(() => 
                {
                    // Bomb should ideally already hit visual wise, we trigger the hit logic / secondary bombs here
                });
                warningCircle.StartWarning(bombDropDuration);
            }
        }

        // Spawn Big Bomb high up
        Vector3 bombSpawnPos = targetPos + Vector3.up * spawnHeight;
        GameObject bombObj = ObjectPool.Instance.Spawn(PoolType.ElectroBomb_Big, bombSpawnPos, Quaternion.identity);
        
        ElectroBomb bombScript = bombObj.GetComponent<ElectroBomb>();
        if (bombScript != null)
        {
            // The bomb script will handle dropping down, exploding, and spawning the 6 small ones
            bombScript.Initialize(enemyData.projectileDamage, bombDropDuration, targetPos, playerLayer, this.gameObject, true);
        }
        else 
        {
            Debug.LogError("ElectroBomb script missing on Big Bomb Prefab!");
        }
    }
}
