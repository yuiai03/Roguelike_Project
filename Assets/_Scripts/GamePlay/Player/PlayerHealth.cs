using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    private const float QuitScoreSubmitTimeoutSeconds = 1.5f;
    private const float DeathCleanupDelaySeconds = 1f;
    private const float DeathLeaderboardDelaySeconds = 0.5f;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;
    public UnityEvent<float> OnHeal;

    [Header("Death Visual")]
    [SerializeField] private float deathDissolveDuration = 1.2f;
    [SerializeField] private bool hidePlayerAfterDissolve = true;

    private bool isDead;
    private bool hasSubmittedRunScore;
    private bool isSavingScoreBeforeQuit;
    private bool allowQuitAfterScoreSave;
    private PlayerData playerData;
    private HealthBarUIBase healthBarUI;
    private Coroutine deathDissolveCoroutine;

    public static PlayerHealth Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        playerData = GetComponent<PlayerData>();
        healthBarUI = GetComponentInChildren<HealthBarUIBase>();
        Application.wantsToQuit += HandleWantsToQuit;
    }

    private void Start()
    {
        if (playerData != null)
        {
            playerData.currentHealth = playerData.GetMaxHealth();

            OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        }
        isDead = false;
        hasSubmittedRunScore = false;
        isSavingScoreBeforeQuit = false;
        allowQuitAfterScoreSave = false;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead || playerData == null) return;

        playerData.currentHealth -= damage;
        playerData.currentHealth = Mathf.Max(0f, playerData.currentHealth);

        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        OnTakeDamage?.Invoke();
        AudioManager.Instance?.PlayWorldSfx(AudioCue.PlayerHit);

        if (DamageTextSpawner.Instance != null && damage > 0)
        {
            Vector3 spawnPos = healthBarUI != null ? healthBarUI.transform.position : transform.position + Vector3.up * 1f;
            DamageTextSpawner.Instance.Spawn(damage, spawnPos, isHeal: false, isPlayer: true);
        }

        if (playerData.currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || playerData == null) return;

        playerData.currentHealth += amount;
        playerData.currentHealth = Mathf.Min(playerData.currentHealth, playerData.GetMaxHealth());

        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        OnHeal?.Invoke(amount);
        if (amount > 0f)
        {
            AudioManager.Instance?.PlayWorldSfx(AudioCue.LevelUp);
        }

        if (DamageTextSpawner.Instance != null && amount > 0)
        {
            Vector3 spawnPos = healthBarUI != null ? healthBarUI.transform.position : transform.position + Vector3.up * 1f;
            DamageTextSpawner.Instance.Spawn(amount, spawnPos, isHeal: true, isPlayer: false);
        }

        Debug.Log($"Player healed {amount}. Current Health: {playerData.currentHealth}/{playerData.GetMaxHealth()}");
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (playerData == null) return;

        playerData.maxHealth += amount;
        playerData.currentHealth += amount;
        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        FreezePlayerForDeath();
        OnDeath?.Invoke();
        AudioManager.Instance?.PlayWorldSfx(AudioCue.PlayerDeath);
        Debug.Log("Player died!");

        StartDeathSequence();
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetCurrentHealth()
    {
        return playerData != null ? playerData.currentHealth : 0f;
    }

    public float GetMaxHealth()
    {
        return playerData != null ? playerData.GetMaxHealth() : 0f;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    private bool HandleWantsToQuit()
    {
        if (allowQuitAfterScoreSave)
        {
            return true;
        }

        if (isSavingScoreBeforeQuit || !ShouldSaveScoreBeforeQuit())
        {
            return !isSavingScoreBeforeQuit;
        }

        Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager leaderboardManager =
            Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance;
        int currentRunScore = GetCurrentRunScore();

        if (leaderboardManager == null || currentRunScore <= 0)
        {
            return true;
        }

        hasSubmittedRunScore = true;
        isSavingScoreBeforeQuit = true;
        leaderboardManager.SubmitScore(currentRunScore, ContinueQuitAfterScoreSave, ContinueQuitAfterScoreSave);
        StartCoroutine(ForceQuitAfterScoreSaveTimeout());
        return false;
    }

    private bool ShouldSaveScoreBeforeQuit()
    {
        if (hasSubmittedRunScore || isDead)
        {
            return false;
        }

        if (WaveSpawner.Instance != null && WaveSpawner.Instance.GetCurrentWave() > 0)
        {
            return true;
        }

        return GetCurrentRunScore() > 0;
    }

    private int GetCurrentRunScore()
    {
        return PlayerLevelSystem.Instance != null
            ? Mathf.FloorToInt(PlayerLevelSystem.Instance.GetTotalExpGained())
            : 0;
    }

    private bool SubmitCurrentRunScore(System.Action onComplete = null)
    {
        if (hasSubmittedRunScore)
        {
            onComplete?.Invoke();
            return false;
        }

        Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager leaderboardManager =
            Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance;
        int currentRunScore = GetCurrentRunScore();

        if (leaderboardManager == null || currentRunScore <= 0)
        {
            onComplete?.Invoke();
            return false;
        }

        hasSubmittedRunScore = true;
        leaderboardManager.SubmitScore(currentRunScore, onComplete, onComplete);
        return true;
    }

    private void FreezePlayerForDeath()
    {
        PlayerController controller = GetComponent<PlayerController>();
        controller?.SetInputActive(false);

        PlayerAnimationController animationController = GetComponent<PlayerAnimationController>();
        animationController?.ForceIdleImmediate();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void HidePlayerAttachedUiForDeath()
    {
        if (healthBarUI == null)
        {
            healthBarUI = GetComponentInChildren<HealthBarUIBase>(true);
        }

        if (healthBarUI == null)
        {
            return;
        }

        CanvasGroup canvasGroup = healthBarUI.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return;
        }

        healthBarUI.gameObject.SetActive(false);
    }

    private void ClearGameplayObjectsOnDeath()
    {
        WaveSpawner.Instance?.StopAndClearCurrentWave();
        ClearRemainingEnemies();
        ObjectPool.Instance?.DespawnAllActiveObjects();
    }

    private void ClearRemainingEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            PoolType poolType = enemy.GetPoolType();
            if (poolType != PoolType.None && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Despawn(enemy.gameObject, poolType);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private void StartDeathSequence()
    {
        if (deathDissolveCoroutine != null)
        {
            StopCoroutine(deathDissolveCoroutine);
        }

        deathDissolveCoroutine = StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        SubmitCurrentRunScore();

        yield return new WaitForSecondsRealtime(DeathCleanupDelaySeconds);
        HidePlayerAttachedUiForDeath();
        ClearGameplayObjectsOnDeath();

        yield return DeathDissolveRoutine();
        yield return new WaitForSecondsRealtime(DeathLeaderboardDelaySeconds);

        ShowLeaderboardAfterDeath();
    }

    private IEnumerator DeathDissolveRoutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            yield break;
        }

        Material[] materials = CollectFadeMaterials(renderers);
        Color[] originalColors = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            ConfigureMaterialForFade(material);
            originalColors[i] = GetMaterialColor(material);
        }

        float duration = Mathf.Max(0.01f, deathDissolveDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < materials.Length; i++)
            {
                SetMaterialAlpha(materials[i], originalColors[i], alpha);
            }

            yield return null;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            SetMaterialAlpha(materials[i], originalColors[i], 0f);
        }

        if (hidePlayerAfterDissolve)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }
    }

    private void ShowLeaderboardAfterDeath()
    {
        Time.timeScale = 1f;
        GameUI.Instance?.InteractPanel?.Hide();

        LeaderboardPanel leaderboardPanel = GameUI.Instance?.LeaderboardPanel;
        if (leaderboardPanel == null)
        {
            return;
        }

        leaderboardPanel.ShowAfterDeath();
    }

    private static Material[] CollectFadeMaterials(Renderer[] renderers)
    {
        List<Material> materials = new List<Material>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            materials.AddRange(renderer.materials);
        }

        return materials.ToArray();
    }

    private static void ConfigureMaterialForFade(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private static void SetMaterialAlpha(Material material, Color originalColor, float alpha)
    {
        if (material == null)
        {
            return;
        }

        Color color = originalColor;
        color.a = alpha;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private IEnumerator ForceQuitAfterScoreSaveTimeout()
    {
        yield return new WaitForSecondsRealtime(QuitScoreSubmitTimeoutSeconds);
        ContinueQuitAfterScoreSave();
    }

    private void ContinueQuitAfterScoreSave()
    {
        if (!isSavingScoreBeforeQuit)
        {
            return;
        }

        isSavingScoreBeforeQuit = false;
        allowQuitAfterScoreSave = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= HandleWantsToQuit;

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
