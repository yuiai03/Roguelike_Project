using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class HealthBarUIBase : MonoBehaviour
{
    [SerializeField] protected bool hideWhenFull = false;
    [SerializeField] protected Image fillImage;

    protected TextMeshProUGUI healthText;
    protected Camera mainCamera;
    protected CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
        var cameraRotation = mainCamera.transform.rotation;
        transform.LookAt(transform.position + cameraRotation * Vector3.forward, cameraRotation * Vector3.up);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && hideWhenFull)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (fillImage != null)
            healthText = fillImage.GetComponentInChildren<TextMeshProUGUI>();

        Initialize();
    }

    protected virtual void Start()
    {
        float currentHealth = GetCurrentHealth();
        float maxHealth = GetMaxHealth();
        UpdateHealthBar(currentHealth, maxHealth);
    }

    protected abstract void Initialize();
    protected abstract float GetCurrentHealth();
    protected abstract float GetMaxHealth();

    public virtual void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0) return;

        if (hideWhenFull && canvasGroup != null)
        {
            bool isFullHealth = currentHealth >= maxHealth;
            canvasGroup.alpha = isFullHealth ? 0f : 1f;
        }

        fillImage.fillAmount = currentHealth / maxHealth;

        if (healthText != null) healthText.text = currentHealth.ToString();
    }
}
