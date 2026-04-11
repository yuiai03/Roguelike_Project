using UnityEngine;
using UnityEngine.Events;

public enum GameFlowState
{
    Tutorial = 0,
    Playing = 1,
    Paused = 2,
    LevelUp = 3,
    GameOver = 4
}

public class GameStateManager : Singleton<GameStateManager>
{
    [Header("State")]
    [SerializeField] private GameFlowState currentState = GameFlowState.Tutorial;

    [Header("Events")]
    public UnityEvent<GameFlowState, GameFlowState> OnStateChanged;

    public GameFlowState CurrentState => currentState;

    public bool IsInputAllowed()
    {
        return currentState == GameFlowState.Playing;
    }

    public bool IsGameplayPaused()
    {
        return currentState != GameFlowState.Playing;
    }

    public bool IsInState(GameFlowState state)
    {
        return currentState == state;
    }

    public void SetState(GameFlowState nextState)
    {
        if (currentState == nextState) return;

        GameFlowState previousState = currentState;
        currentState = nextState;

        ApplyRuntimeState();
        OnStateChanged?.Invoke(previousState, currentState);
    }

    public void ForceRefreshState()
    {
        ApplyRuntimeState();
    }

    private void ApplyRuntimeState()
    {
        Time.timeScale = IsGameplayPaused() ? 0f : 1f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(IsInputAllowed());
        }
    }
}
