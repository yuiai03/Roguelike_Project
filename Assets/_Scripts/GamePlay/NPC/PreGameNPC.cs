using UnityEngine;
using UnityEngine.InputSystem;

public class PreGameNPC : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Khoảng cách tối đa người chơi có thể tương tác")]
    [SerializeField] private float interactRange = 3f;
    [Tooltip("Tốc độ xoay tự động của NPC")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private string interactText = "Bấm F để nói chuyện";

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
        hasInteracted = true;

        if (GameStartUIManager.Instance != null)
        {
            GameStartUIManager.Instance.ShowTutorial(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasInteracted) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            playerInRange = true;

            if (GameStartUIManager.Instance != null)
            {
                GameStartUIManager.Instance.ShowInteractPrompt(true, interactText);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (hasInteracted) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;

            if (GameStartUIManager.Instance != null)
            {
                GameStartUIManager.Instance.ShowInteractPrompt(false);
            }
        }
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }
}
