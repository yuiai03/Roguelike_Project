using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimationName = "selen_idle";
    [SerializeField] private string runAnimationName = "selen_run";
    [SerializeField] private string dashAnimationName = "selen_dash";
    [SerializeField] private string attackAnimationName = "selen_attack";

    [Header("Settings")]
    [SerializeField] private float transitionTime = 0.15f;
    [SerializeField] private bool debugMode = false;

    public enum EnemyAnimState
    {
        Idle,
        Run,
        Dash,
        Attack
    }

    private Enemy enemy;
    private EnemyAnimState currentAnimState = EnemyAnimState.Idle;
    private EnemyAnimState previousAnimState = EnemyAnimState.Idle;
    private EnemyState lastEnemyState = EnemyState.Idle;

    private int idleHash;
    private int runHash;
    private int dashHash;
    private int attackHash;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

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

        idleHash = Animator.StringToHash(idleAnimationName);
        runHash = Animator.StringToHash(runAnimationName);
        dashHash = Animator.StringToHash(dashAnimationName);
        attackHash = Animator.StringToHash(attackAnimationName);

        if (debugMode)
        {
            Debug.Log($"[EnemyAnimController] {gameObject.name} initialized");
            Debug.Log($"  - Animator: {animator.name}");
            Debug.Log($"  - Idle: {idleAnimationName} (Hash: {idleHash})");
            Debug.Log($"  - Run: {runAnimationName} (Hash: {runHash})");
            Debug.Log($"  - Dash: {dashAnimationName} (Hash: {dashHash})");
        }
    }

    private void OnEnable()
    {

        if (animator != null)
        {
            animator.enabled = true;

            int hash = GetStateHash(EnemyAnimState.Idle);
            if (hash != 0)
            {
                animator.Play(hash, 0, 0f);
            }
        }
        currentAnimState = EnemyAnimState.Idle;
        previousAnimState = EnemyAnimState.Idle;
        lastEnemyState = EnemyState.Idle;

        if (enemy != null)
        {
            enemy.OnAttack.AddListener(OnEnemyAttack);
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnAttack.RemoveListener(OnEnemyAttack);
        }
    }

    private void OnEnemyAttack()
    {
        if (animator == null) return;
        
        // Use CrossFade or Play with the state hash directly instead of a Trigger
        animator.Play(attackHash, 0, 0f);
        
        // Prevent state checker from immediately overriding this with Idle
        if (enemy != null)
        {
            lastEnemyState = enemy.GetCurrentState();
        }
        previousAnimState = currentAnimState;
        currentAnimState = EnemyAnimState.Attack;

        if (debugMode)
        {
            Debug.Log($"[{gameObject.name}] Played Attack Animation (Hash: {attackHash})");
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

    private void UpdateAnimationBasedOnEnemyState()
    {
        EnemyState currentEnemyState = enemy.GetCurrentState();

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

    private EnemyAnimState MapEnemyStateToAnimation(EnemyState enemyState)
    {
        switch (enemyState)
        {
            case EnemyState.Idle:
            case EnemyState.Attacking:
                return EnemyAnimState.Idle;

            case EnemyState.Dead:

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

    private void PlayAnimation(EnemyAnimState state)
    {
        if (animator == null) return;
        if (currentAnimState == state) return; 

        previousAnimState = currentAnimState;
        currentAnimState = state;

        int stateHash = GetStateHash(state);
        animator.CrossFade(stateHash, transitionTime);

        if (debugMode)
        {
            Debug.Log($"[{gameObject.name}] Playing animation: {state} (Hash: {stateHash})");
        }
    }

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
            case EnemyAnimState.Attack:
                return attackHash;
            default:
                Debug.LogWarning($"[EnemyAnimationController] Unknown animation state: {state}");
                return idleHash;
        }
    }

    public void PlayIdle() => PlayAnimation(EnemyAnimState.Idle);
    public void PlayRun() => PlayAnimation(EnemyAnimState.Run);
    public void PlayDash() => PlayAnimation(EnemyAnimState.Dash);

    public EnemyAnimState GetCurrentAnimState() => currentAnimState;
    public EnemyAnimState GetPreviousAnimState() => previousAnimState;

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
