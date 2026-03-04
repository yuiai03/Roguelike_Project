using UnityEngine;
using UnityEngine.InputSystem;

public class PreGameNPC : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameStartUIManager uiManager;

    [Header("Settings")]
    [Tooltip("Khoảng cách tối đa người chơi có thể tương tác")]
    [SerializeField] private float interactRange = 3f;

    private Transform playerTransform;
    private bool playerInRange = false;
    private bool hasInteracted = false;

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
        if (hasInteracted) return;

        if (playerInRange)
        {
            Debug.Log("Player in range");

            if (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                Interact();
            }
        }
    }

    private void Interact()
    {
        hasInteracted = true;

        if (uiManager != null)
        {
            uiManager.ShowTutorial(this);
        }
        else
        {
            Debug.LogError("GameStartUIManager chưa được gắn vào PreGameNPC!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasInteracted) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            playerInRange = true;

            if (uiManager != null)
            {
                uiManager.ShowInteractPrompt(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (hasInteracted) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;

            if (uiManager != null)
            {
                uiManager.ShowInteractPrompt(false);
            }
        }
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }
}
