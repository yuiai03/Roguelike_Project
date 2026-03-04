using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class base cho tất cả NPC tương tác trong game.
/// Xử lý: xoay, input F/E, trigger enter/exit, hiển thị prompt.
/// </summary>
public abstract class NPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [Tooltip("Tốc độ xoay tự động của NPC")]
    [SerializeField] protected float rotationSpeed = 50f;
    [SerializeField] protected string interactText = "Bấm F để tương tác";

    protected bool canRotate = true;
    protected bool playerInRange = false;
    protected InputSystem_Actions inputActions;

    protected virtual void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    protected virtual void OnEnable()
    {
        inputActions.Enable();
        ChallengePanel.onGameStart += OnGameStart;
    }

    protected virtual void OnDisable()
    {
        inputActions.Disable();
        ChallengePanel.onGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (canRotate) transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        if (playerInRange && !IsPanelOpen() && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    /// <summary>Trả về true nếu panel liên quan đang mở. Override ở subclass.</summary>
    protected virtual bool IsPanelOpen() => false;

    /// <summary>Hành động khi player nhấn F/E trong range.</summary>
    protected abstract void Interact();

    /// <summary>
    /// Điều kiện bổ sung để hiện prompt khi player vào range.
    /// Override để thêm điều kiện (vd: kiểm tra wave).
    /// </summary>
    protected virtual bool CanShowPrompt() => true;

    /// <summary>
    /// Gọi khi player rời khỏi range. Override để xử lý thêm (vd: đóng panel).
    /// </summary>
    protected virtual void OnPlayerExit() { }

    /// <summary>
    /// Gọi khi panel liên quan đóng lại.
    /// Nếu player vẫn còn trong range thì hiển thị lại prompt.
    /// </summary>
    protected void OnPanelClosed()
    {
        if (playerInRange)
            GameUI.Instance?.InteractPanel?.ShowText(interactText);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanShowPrompt()) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = true;
            GameUI.Instance?.InteractPanel?.ShowText(interactText);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
            GameUI.Instance?.InteractPanel?.Hide();
            OnPlayerExit();
        }
    }
}
