using UnityEngine;

/// <summary>
/// Manages all player animations. Easily extensible for new animation states.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    // Animation State enum - Add new states here
    public enum AnimationState
    {
        Idle,
        Run,
        Dash,
        // Easy to add more:
        // Jump,
        // Attack,
        // Hit,
        // Death,
        // etc.
    }

    // Animation hash IDs - cached for performance
    private static class AnimHash
    {
        public static readonly int Idle = Animator.StringToHash("annie_idle");
        public static readonly int Run = Animator.StringToHash("annie_run");
        public static readonly int Dash = Animator.StringToHash("annie_dash");

        // Add more animation hashes here when adding new animations:
        // public static readonly int Jump = Animator.StringToHash("annie_jump");
        // public static readonly int Attack = Animator.StringToHash("annie_attack");
    }

    // Transition settings - customize blend times per animation
    private static class TransitionTime
    {
        public const float Fast = 0.1f;      // For quick actions (dash, jump)
        public const float Normal = 0.15f;   // For regular movement
        public const float Slow = 0.25f;     // For smooth transitions
    }

    private AnimationState currentState;
    private AnimationState previousState;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError($"[PlayerAnimationController] No Animator found on {gameObject.name}!");
        }
    }

    /// <summary>
    /// Play animation by state enum. Handles transitions automatically.
    /// </summary>
    public void PlayAnimation(AnimationState state, float transitionTime = TransitionTime.Normal)
    {
        if (animator == null) return;
        if (currentState == state) return; // Don't replay same animation

        previousState = currentState;
        currentState = state;

        int stateHash = GetStateHash(state);
        animator.CrossFade(stateHash, transitionTime);
    }

    /// <summary>
    /// Play animation immediately without transition
    /// </summary>
    public void PlayAnimationImmediate(AnimationState state)
    {
        if (animator == null) return;

        previousState = currentState;
        currentState = state;

        int stateHash = GetStateHash(state);
        animator.Play(stateHash, 0, 0f);
    }

    /// <summary>
    /// Get animation hash from state enum
    /// </summary>
    private int GetStateHash(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Idle:
                return AnimHash.Idle;
            case AnimationState.Run:
                return AnimHash.Run;
            case AnimationState.Dash:
                return AnimHash.Dash;

            // Add new cases here when adding animations:
            // case AnimationState.Jump:
            //     return AnimHash.Jump;

            default:
                Debug.LogWarning($"[PlayerAnimationController] Unknown animation state: {state}");
                return AnimHash.Idle;
        }
    }

    /// <summary>
    /// Get current animation state
    /// </summary>
    public AnimationState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Get previous animation state
    /// </summary>
    public AnimationState GetPreviousState()
    {
        return previousState;
    }

    /// <summary>
    /// Check if specific animation is currently playing
    /// </summary>
    public bool IsPlaying(AnimationState state)
    {
        return currentState == state;
    }

    /// <summary>
    /// Get animator component reference
    /// </summary>
    public Animator GetAnimator()
    {
        return animator;
    }

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

    /// <summary>
    /// Advanced: Play animation with custom transition based on previous state
    /// </summary>
    public void PlayAnimationSmart(AnimationState state)
    {
        float transitionTime = TransitionTime.Normal;

        // Customize transition time based on state changes
        switch (state)
        {
            case AnimationState.Dash:
                transitionTime = TransitionTime.Fast; // Quick dash
                break;

            case AnimationState.Idle:
                // Slower transition to idle from run
                transitionTime = previousState == AnimationState.Run ? TransitionTime.Slow : TransitionTime.Normal;
                break;

            case AnimationState.Run:
                transitionTime = TransitionTime.Normal;
                break;
        }

        PlayAnimation(state, transitionTime);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Debug: Show current animation state in inspector
    /// </summary>
    void OnGUI()
    {
        if (animator != null && animator.enabled)
        {
            // Uncomment to show debug info in game view:
            // GUI.Label(new Rect(10, 10, 200, 20), $"Animation: {currentState}");
        }
    }
#endif
}
