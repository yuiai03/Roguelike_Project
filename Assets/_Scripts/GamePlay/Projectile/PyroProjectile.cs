using UnityEngine;

/// <summary>
/// Đạn lửa Pyro — kích hoạt ParticleSystem ở obj con khi spawn.
/// </summary>
public class PyroProjectile : Projectile
{
    private ParticleSystem childParticle;

    protected override void Awake()
    {
        base.Awake();
        childParticle = GetComponentInChildren<ParticleSystem>(includeInactive: true);
    }

    public override void Initialize(float damage, float speed, float lifetime, Vector3 direction, LayerMask targetLayer, GameObject owner)
    {
        base.Initialize(damage, speed, lifetime, direction, targetLayer, owner);
        transform.rotation = Quaternion.identity;
        PlayParticle();
    }

    private void PlayParticle()
    {
        if (childParticle == null) return;
        childParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        childParticle.Play();
    }

    protected override void DispawnProjectile()
    {
        childParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
