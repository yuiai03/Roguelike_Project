using UnityEngine;
using UnityEngine.InputSystem;
public class LeaderboardNPC : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject interactPromptPanel; 
    [SerializeField] private GameObject leaderboardPanel;
    [Header("Settings")]
    [Tooltip("Tốc độ xoay tự động của NPC")]
    [SerializeField] private float rotationSpeed = 50f;
    private Transform playerTransform;
    private bool playerInRange = false;
    private InputSystem_Actions inputActions;
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        if (playerInRange)
        {
            if ((Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                Interact();
            }
        }
    }
    private void Interact()
    {
        if (leaderboardPanel != null)
        {
            bool isActive = leaderboardPanel.activeSelf;
            leaderboardPanel.SetActive(!isActive);
            
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetInputActive(isActive);
            }

            if (!isActive)
            {
                {
                    Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance.GetLeaderboardData();
                }
            }
        }
        else
        {
            GameStartUIManager uiManager = FindObjectOfType<GameStartUIManager>();
            if (uiManager != null)
            {
                uiManager.ShowLeaderboard();
            }
            else
            {
                Debug.LogError("Chưa gán Leaderboard Panel cho LeaderboardNPC!");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (WaveSpawner.Instance.GetCurrentWave() > 0) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            playerInRange = true;
            {
                interactPromptPanel.SetActive(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
            {
                interactPromptPanel.SetActive(false);
            }
            if (leaderboardPanel.activeSelf)
            {
                leaderboardPanel.SetActive(false);
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.SetInputActive(true);
                }
            }
        }
    }
}
