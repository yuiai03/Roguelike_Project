using UnityEngine;

/// <summary>
/// Manages enemy animations based on enemy state. Works similar to PlayerAnimationController.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimationName = "selen_idle";
    [SerializeField] private string runAnimationName = "selen_run";
    [SerializeField] private string dashAnimationName = "selen_dash";

    [Header("Settings")]
    [SerializeField] private float transitionTime = 0.15f;
    [SerializeField] private bool debugMode = false;

    // Enemy state enum for animation mapping
    public enum EnemyAnimState
    {
        Idle,
        Run,
        Dash
    }

    private Enemy enemy;
    private EnemyAnimState currentAnimState = EnemyAnimState.Idle;
    private EnemyAnimState previousAnimState = EnemyAnimState.Idle;
    private EnemyState lastEnemyState = EnemyState.Idle;

    // Animation hash IDs - cached for performance
    private int idleHash;
    private int runHash;
    private int dashHash;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError($"[EnemyAnimationController] No Animator found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Cache animation hashes for better performance
        idleHash = Animator.StringToHash(idleAnimationName);
        runHash = Animator.StringToHash(runAnimationName);
        dashHash = Animator.StringToHash(dashAnimationName);

        if (debugMode)
        {
            Debug.Log($"[EnemyAnimController] {gameObject.name} initialized");
            Debug.Log($"  - Animator: {animator.name}");
            Debug.Log($"  - Idle: {idleAnimationName} (Hash: {idleHash})");
            Debug.Log($"  - Run: {runAnimationName} (Hash: {runHash})");
            Debug.Log($"  - Dash: {dashAnimationName} (Hash: {dashHash})");
        }
    }

    private void Update()
    {
        if (animator == null || enemy == null || enemy.IsDead())
        {
            return;
        }

        UpdateAnimationBasedOnEnemyState();
    }

    /// <summary>
    /// Update animation based on current enemy state
    /// </summary>
    private void UpdateAnimationBasedOnEnemyState()
    {
        EnemyState currentEnemyState = enemy.GetCurrentState();

        // Only change animation when enemy state actually changes
        if (currentEnemyState != lastEnemyState)
        {
            EnemyAnimState newAnimState = MapEnemyStateToAnimation(currentEnemyState);

            if (newAnimState != currentAnimState)
            {
                PlayAnimation(newAnimState);

                if (debugMode)
                {
                    Debug.Log($"[{gameObject.name}] Enemy State: {lastEnemyState} → {currentEnemyState}");
                    Debug.Log($"  Animation: {previousAnimState} → {currentAnimState}");
                }
            }

            lastEnemyState = currentEnemyState;
        }
    }

    /// <summary>
    /// Map enemy state to animation state
    /// </summary>
    private EnemyAnimState MapEnemyStateToAnimation(EnemyState enemyState)
    {
        switch (enemyState)
        {
            case EnemyState.Idle:
            case EnemyState.Attacking:
                return EnemyAnimState.Idle;
                
            case EnemyState.Dead:
                // Thay vì return Idle để nó phát CrossFade, ta tắt luôn Animator để dừng mọi chạy/dash
                if (animator != null) animator.enabled = false;
                return EnemyAnimState.Idle;

            case EnemyState.Chasing:
            case EnemyState.Retreating:
                return EnemyAnimState.Run;

            case EnemyState.Lunging:
                return EnemyAnimState.Dash;

            default:
                return EnemyAnimState.Idle;
        }
    }

    /// <summary>
    /// Play animation by state enum. Handles transitions automatically.
    /// </summary>
    private void PlayAnimation(EnemyAnimState state)
    {
        if (animator == null) return;
        if (currentAnimState == state) return; // Don't replay same animation

        previousAnimState = currentAnimState;
        currentAnimState = state;

        int stateHash = GetStateHash(state);
        animator.CrossFade(stateHash, transitionTime);

        if (debugMode)
        {
            Debug.Log($"[{gameObject.name}] Playing animation: {state} (Hash: {stateHash})");
        }
    }

    /// <summary>
    /// Play animation immediately without transition
    /// </summary>
    public void PlayAnimationImmediate(EnemyAnimState state)
    {
        if (animator == null) return;

        previousAnimState = currentAnimState;
        currentAnimState = state;

        int stateHash = GetStateHash(state);
        animator.Play(stateHash, 0, 0f);

        if (debugMode)
        {
            Debug.Log($"[{gameObject.name}] Playing animation immediate: {state}");
        }
    }

    /// <summary>
    /// Get animation hash from state enum
    /// </summary>
    private int GetStateHash(EnemyAnimState state)
    {
        switch (state)
        {
            case EnemyAnimState.Idle:
                return idleHash;
            case EnemyAnimState.Run:
                return runHash;
            case EnemyAnimState.Dash:
                return dashHash;
            default:
                Debug.LogWarning($"[EnemyAnimationController] Unknown animation state: {state}");
                return idleHash;
        }
    }

    /// <summary>
    /// Public methods to manually trigger animations if needed
    /// </summary>
    public void PlayIdle() => PlayAnimation(EnemyAnimState.Idle);
    public void PlayRun() => PlayAnimation(EnemyAnimState.Run);
    public void PlayDash() => PlayAnimation(EnemyAnimState.Dash);

    public EnemyAnimState GetCurrentAnimState() => currentAnimState;
    public EnemyAnimState GetPreviousAnimState() => previousAnimState;

    /// <summary>
    /// Set animator speed (useful for slow-motion effects)
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        if (enemy == null || animator == null) return;

        Debug.Log($"═══ {gameObject.name} Animation State ═══");
        Debug.Log($"  Enemy State: {enemy.GetCurrentState()}");
        Debug.Log($"  Current Anim State: {currentAnimState}");
        Debug.Log($"  Previous Anim State: {previousAnimState}");

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"  Animator State Hash: {stateInfo.shortNameHash}");
        Debug.Log($"  Animator Speed: {animator.speed}");
        Debug.Log($"  Is In Transition: {animator.IsInTransition(0)}");
        Debug.Log($"════════════════════════════════════");
    }
}
