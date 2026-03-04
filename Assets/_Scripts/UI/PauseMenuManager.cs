using UnityEngine;
using UnityEngine.UI;
public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button endGameButton;
    [Header("Optional References")]
    [Tooltip("Gắn thủ công UI Leaderboard để hiển thị khi Kết thúc game. Hoặc có thể tự GameOver")]
    [SerializeField] private GameObject leaderboardPanel;
    private bool isPaused = false;
    private void Awake()
    {
            resumeButton.onClick.AddListener(ResumeGame);
            endGameButton.onClick.AddListener(EndGame);
            pauseMenuPanel.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    public void PauseGame()
    {
        isPaused = true;
        {
            pauseMenuPanel.SetActive(true);
        }
        Time.timeScale = 0f;
        
        if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(false);
    }
    public void ResumeGame()
    {
        isPaused = false;
        {
            pauseMenuPanel.SetActive(false);
        }
        Time.timeScale = 1f;
        
        if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(true);
    }
    public void EndGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        {
            pauseMenuPanel.SetActive(false);
        }
        if (!PlayerHealth.Instance.IsDead())
        {
            PlayerHealth.Instance.TakeDamage(999999f, Vector3.zero, Vector3.zero);
        }
        {
            leaderboardPanel.SetActive(true);
            {
                Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance.GetLeaderboardData();
            }
        }
    }
    private void OnDestroy()
    {
resumeButton.onClick.RemoveListener(ResumeGame);
endGameButton.onClick.RemoveListener(EndGame);
    }
}
