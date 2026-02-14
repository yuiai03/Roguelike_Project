using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private bool isGrounded;
    private bool dashPressed;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private PlayerHealth playerHealth;
    private PlayerData playerData;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 dashDirection;

    [Header("Model Reference")]
    [SerializeField] private Transform modelTransform;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerData = GetComponent<PlayerData>();

        if (modelTransform == null)
        {
            Renderer childRenderer = GetComponentInChildren<Renderer>();
            if (childRenderer != null && childRenderer.transform != transform)
            {
                modelTransform = childRenderer.transform;
            }
        }

        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnEnable()
    {
        if (inputActions != null)
            inputActions.Player.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null)
            inputActions.Player.Disable();
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
            inputActions.Dispose();
        }
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead()) return;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, playerData.groundDistance, groundMask);
            if (isGrounded && velocity.y < 0) velocity.y = -2f;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            dashPressed = true;
        }

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;

        if (dashPressed && !isDashing && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = playerData.dashDuration;
            dashCooldownTimer = playerData.dashCooldown;
            
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                dashDirection = moveDirection.normalized;
            }
            else
            {
                dashDirection = modelTransform != null ? modelTransform.forward : transform.forward;
            }
        }
        dashPressed = false;

        if (isDashing)
        {
            controller.Move(dashDirection * playerData.dashSpeed * Time.deltaTime);
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }
        else
        {
            controller.Move(moveDirection * playerData.GetEffectiveMoveSpeed() * Time.deltaTime);
        }

        if (moveDirection.sqrMagnitude > 0.01f && modelTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            modelTransform.rotation = Quaternion.RotateTowards(
                modelTransform.rotation, 
                targetRotation, 
                playerData.rotationSpeed * Time.deltaTime
            );
        }

        velocity.y += playerData.gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public Transform GetModelTransform()
    {
        return modelTransform;
    }

    void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        if (playerData != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, playerData.groundDistance);
        }
    }
}