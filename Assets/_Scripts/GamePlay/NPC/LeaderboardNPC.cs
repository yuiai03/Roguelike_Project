using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class LeaderboardNPC : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject leaderboardPanel;
    [Header("Settings")]
    [Tooltip("Tốc độ xoay tự động của NPC")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private string interactText = "Bấm F để xem bảng xếp hạng";

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
            if (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                Interact();
            }
        }
    }
    private void Interact()
    {
        GameStartUIManager.Instance.ShowLeaderboard();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (WaveSpawner.Instance.GetCurrentWave() > 0) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            playerInRange = true;
            {
                GameStartUIManager.Instance.ShowInteractPrompt(true, interactText);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
            {
                GameStartUIManager.Instance.ShowInteractPrompt(false);
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
