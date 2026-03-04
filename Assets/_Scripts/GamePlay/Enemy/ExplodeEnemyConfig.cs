using UnityEngine;

[CreateAssetMenu(fileName = "ExplodeEnemyConfig", menuName = "Roguelike/Enemy/Explode Config")]
public class ExplodeEnemyConfig : EnemyConfig
{
    [Header("Exploder Settings")]
    public float detectionRange    = 6f;    
    public float chargeSpeed       = 18f;   
    public float chargeDistance    = 8f;    
    public float warningDuration   = 0.8f;  

    [Header("Explosion")]
    public float explosionRadius   = 3f;
    public float explosionDamage   = 50f;
    public float deathExplosionMult = 0.5f; 
}
