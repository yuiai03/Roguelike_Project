using System;
using UnityEngine;

[Serializable]
public class MapThemeDefinition
{
    public string themeName;
    public Material groundMaterial;
    public Material wallMaterial;
    public GameObject effectRoot;
}

public class MapThemeManager : MonoBehaviour
{
    public static MapThemeManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private MeshRenderer groundRenderer;
    [SerializeField] private MeshRenderer[] wallRenderers;
    [SerializeField] private MapThemeDefinition[] themes;

    [Header("Transition")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float holdDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    public int CurrentThemeIndex { get; private set; } = -1;
    public bool IsTransitioning { get; private set; }
    public event Action<int> OnThemeTransitionCompleted;

    private int pendingThemeIndex = -1;
    private PlayerLevelSystem levelSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MapThemeManager] Duplicate instance detected. Destroying the newest one.");
            Destroy(this);
            return;
        }

        Instance = this;
        CleanupRendererList();
    }

    private void OnEnable()
    {
        TryBindLevelSystem();
    }

    private void Start()
    {
        TryBindLevelSystem();
        ApplyThemeImmediate(ResolveThemeIndex(GetCurrentLevel()));
    }

    private void OnDisable()
    {
        UnbindLevelSystem();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool WillThemeChangeForLevel(int level)
    {
        int targetIndex = ResolveThemeIndex(level);
        return targetIndex >= 0 && targetIndex != CurrentThemeIndex;
    }

    private void HandleLevelUp(int newLevel)
    {
        int targetIndex = ResolveThemeIndex(newLevel);
        if (targetIndex < 0 || targetIndex == CurrentThemeIndex)
        {
            return;
        }

        StartThemeTransition(targetIndex);
    }

    private void StartThemeTransition(int targetIndex)
    {
        if (!IsValidThemeIndex(targetIndex))
        {
            return;
        }

        if (IsTransitioning)
        {
            pendingThemeIndex = targetIndex;
            return;
        }

        IsTransitioning = true;
        pendingThemeIndex = -1;

        LoadingUIManager loadingUI = LoadingUIManager.Instance;
        if (loadingUI == null)
        {
            ApplyThemeImmediate(targetIndex);
            FinishThemeTransition(targetIndex);
            return;
        }

        loadingUI.PlayBlackTransition(
            () => ApplyThemeImmediate(targetIndex),
            () => FinishThemeTransition(targetIndex),
            fadeInDuration,
            holdDuration,
            fadeOutDuration);
    }

    private void FinishThemeTransition(int themeIndex)
    {
        CurrentThemeIndex = themeIndex;
        IsTransitioning = false;
        OnThemeTransitionCompleted?.Invoke(themeIndex);

        if (pendingThemeIndex >= 0 && pendingThemeIndex != CurrentThemeIndex)
        {
            int nextThemeIndex = pendingThemeIndex;
            pendingThemeIndex = -1;
            StartThemeTransition(nextThemeIndex);
        }
    }

    private void ApplyThemeImmediate(int themeIndex)
    {
        if (!IsValidThemeIndex(themeIndex))
        {
            return;
        }

        MapThemeDefinition theme = themes[themeIndex];

        if (groundRenderer != null && theme.groundMaterial != null)
        {
            groundRenderer.sharedMaterial = theme.groundMaterial;
        }

        if (wallRenderers != null)
        {
            foreach (MeshRenderer wallRenderer in wallRenderers)
            {
                if (wallRenderer == null || theme.wallMaterial == null)
                {
                    continue;
                }

                wallRenderer.sharedMaterial = theme.wallMaterial;
            }
        }

        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i] == null || themes[i].effectRoot == null)
            {
                continue;
            }

            themes[i].effectRoot.SetActive(i == themeIndex);
        }

        CurrentThemeIndex = themeIndex;
    }

    private int ResolveThemeIndex(int level)
    {
        if (themes == null || themes.Length == 0)
        {
            return -1;
        }

        int normalizedLevel = Mathf.Max(level, 1);
        return ((normalizedLevel - 1) / 10) % themes.Length;
    }

    private int GetCurrentLevel()
    {
        return PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance.GetCurrentLevel() : 0;
    }

    private bool IsValidThemeIndex(int themeIndex)
    {
        return themes != null && themeIndex >= 0 && themeIndex < themes.Length;
    }

    private void TryBindLevelSystem()
    {
        if (levelSystem != null)
        {
            return;
        }

        levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.AddListener(HandleLevelUp);
        }
    }

    private void UnbindLevelSystem()
    {
        if (levelSystem == null)
        {
            return;
        }

        levelSystem.OnLevelUp.RemoveListener(HandleLevelUp);
        levelSystem = null;
    }

    private void CleanupRendererList()
    {
        if (wallRenderers == null)
        {
            return;
        }

        wallRenderers = Array.FindAll(wallRenderers, renderer => renderer != null);
    }
}
