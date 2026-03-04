using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    public enum AnimationState
    {
        Idle,
        Run,
        Dash,

    }

    private static class AnimHash
    {
        public static readonly int Idle = Animator.StringToHash("annie_idle");
        public static readonly int Run = Animator.StringToHash("annie_run");
        public static readonly int Dash = Animator.StringToHash("annie_dash");

    }

    private static class TransitionTime
    {
        public const float Fast = 0.1f;      
        public const float Normal = 0.15f;   
        public const float Slow = 0.25f;     
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

    public void PlayAnimation(AnimationState state, float transitionTime = TransitionTime.Normal)
    {
        if (animator == null) return;
        if (currentState == state) return; 

        previousState = currentState;
        currentState = state;

        int stateHash = GetStateHash(state);
        animator.CrossFade(stateHash, transitionTime);
    }

    public void PlayAnimationImmediate(AnimationState state)
    {
        if (animator == null) return;

        previousState = currentState;
        currentState = state;

        int stateHash = GetStateHash(state);
        animator.Play(stateHash, 0, 0f);
    }

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

            default:
                Debug.LogWarning($"[PlayerAnimationController] Unknown animation state: {state}");
                return AnimHash.Idle;
        }
    }

    public AnimationState GetCurrentState()
    {
        return currentState;
    }

    public AnimationState GetPreviousState()
    {
        return previousState;
    }

    public bool IsPlaying(AnimationState state)
    {
        return currentState == state;
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    public void PlayAnimationSmart(AnimationState state)
    {
        float transitionTime = TransitionTime.Normal;

        switch (state)
        {
            case AnimationState.Dash:
                transitionTime = TransitionTime.Fast; 
                break;

            case AnimationState.Idle:

                transitionTime = previousState == AnimationState.Run ? TransitionTime.Slow : TransitionTime.Normal;
                break;

            case AnimationState.Run:
                transitionTime = TransitionTime.Normal;
                break;
        }

        PlayAnimation(state, transitionTime);
    }

#if UNITY_EDITOR

    void OnGUI()
    {
        if (animator != null && animator.enabled)
        {

        }
    }
#endif
}
