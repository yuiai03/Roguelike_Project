using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlinkoBall : MonoBehaviour
{
    private static readonly List<CircleCollider2D> ActiveBallColliders = new List<CircleCollider2D>();

    [SerializeField] private float initialHorizontalImpulse = 0.12f;
    [SerializeField] private float maxLifetime = 15f;

    private Rigidbody2D cachedRigidbody;
    private CircleCollider2D cachedCollider;
    private Coroutine lifetimeRoutine;
    private bool isResolved;
    private PlinkoGameController ownerController;
    private Transform lockedSlot;
    private int lockedRewardValue = -1;
    private int lockedSlotIndex = -1;
    private PlinkoFunnel ownedFunnel;

    public int LockedRewardValue => lockedRewardValue;
    public int LockedSlotIndex => lockedSlotIndex;
    public Transform LockedSlot => lockedSlot;
    public PlinkoFunnel OwnedFunnel => ownedFunnel;
    public Rigidbody2D CachedRigidbody => cachedRigidbody;
    public CircleCollider2D CachedCollider => cachedCollider;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        isResolved = false;
        RegisterActiveCollider();
    }

    private void OnDisable()
    {
        if (ownerController != null)
        {
            ownerController.UnregisterActiveBall(this);
            ownerController = null;
        }

        ReleaseOwnedFunnel();
        UnregisterActiveCollider();

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }
    }

    public void Initialize()
    {
        Initialize(transform.position);
    }

    public void Initialize(Vector2 spawnPosition)
    {
        CacheComponents();
        ResetLockedTarget();

        isResolved = false;
        cachedCollider.enabled = true;
        cachedRigidbody.simulated = true;
        transform.localRotation = Quaternion.identity;
        transform.position = spawnPosition;
        cachedRigidbody.position = spawnPosition;
        cachedRigidbody.rotation = 0f;
        cachedRigidbody.linearVelocity = Vector2.zero;
        cachedRigidbody.angularVelocity = 0f;
        cachedRigidbody.WakeUp();

        if (!Mathf.Approximately(initialHorizontalImpulse, 0f))
        {
            cachedRigidbody.AddForce(Vector2.right * initialHorizontalImpulse, ForceMode2D.Impulse);
        }

        RegisterActiveCollider();
        RestartLifetimeRoutine();
    }

    public void InitializeLocked(
        PlinkoGameController owner,
        Vector3 localSpawnPosition,
        int slotIndex,
        Transform lockedTargetSlot,
        int rewardValue)
    {
        CacheComponents();
        ResetLockedTarget();

        isResolved = false;
        ownerController = owner;
        lockedSlot = lockedTargetSlot;
        lockedRewardValue = rewardValue;
        lockedSlotIndex = slotIndex;

        cachedCollider.enabled = true;
        cachedRigidbody.simulated = true;
        cachedRigidbody.linearVelocity = Vector2.zero;
        cachedRigidbody.angularVelocity = 0f;
        cachedRigidbody.rotation = 0f;

        transform.localPosition = localSpawnPosition;
        transform.localRotation = Quaternion.identity;
        cachedRigidbody.position = transform.position;
        cachedRigidbody.WakeUp();
        RegisterActiveCollider();
        RestartLifetimeRoutine();
    }

    public bool TryResolve()
    {
        if (isResolved)
        {
            return false;
        }

        isResolved = true;
        ReleaseOwnedFunnel();
        return true;
    }

    public void SetOwnedFunnel(PlinkoFunnel funnel)
    {
        if (ownedFunnel == funnel)
        {
            return;
        }

        ReleaseOwnedFunnel();
        ownedFunnel = funnel;
    }

    public float GetLocalRadius()
    {
        CacheComponents();
        return cachedCollider == null ? 0f : cachedCollider.radius * Mathf.Abs(transform.localScale.x);
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(maxLifetime);

        if (!isResolved)
        {
            isResolved = true;
            ReleaseOwnedFunnel();
            Destroy(gameObject);
        }
    }

    public bool IsLockedToSlot(Transform slotTransform)
    {
        return slotTransform != null && lockedSlot == slotTransform;
    }

    private void CacheComponents()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody2D>();
        }

        if (cachedCollider == null)
        {
            cachedCollider = GetComponent<CircleCollider2D>();
        }
    }

    private void RestartLifetimeRoutine()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
        }

        if (Application.isPlaying)
        {
            lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }
    }

    private void ResetLockedTarget()
    {
        if (ownerController != null)
        {
            ownerController.UnregisterActiveBall(this);
            ownerController = null;
        }

        ReleaseOwnedFunnel();
        lockedSlot = null;
        lockedRewardValue = -1;
        lockedSlotIndex = -1;
    }

    private void ReleaseOwnedFunnel()
    {
        if (ownedFunnel == null)
        {
            return;
        }

        GameObject funnelObject = ownedFunnel.gameObject;
        ownedFunnel = null;

        if (funnelObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(funnelObject);
            return;
        }

        DestroyImmediate(funnelObject);
    }

    private void RegisterActiveCollider()
    {
        CacheComponents();

        if (cachedCollider == null || ActiveBallColliders.Contains(cachedCollider))
        {
            return;
        }

        for (int index = ActiveBallColliders.Count - 1; index >= 0; index--)
        {
            CircleCollider2D otherCollider = ActiveBallColliders[index];
            if (otherCollider == null)
            {
                ActiveBallColliders.RemoveAt(index);
                continue;
            }

            Physics2D.IgnoreCollision(cachedCollider, otherCollider, true);
        }

        ActiveBallColliders.Add(cachedCollider);
    }

    private void UnregisterActiveCollider()
    {
        if (cachedCollider == null)
        {
            return;
        }

        ActiveBallColliders.Remove(cachedCollider);
    }
}
