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
    private Action activeTransitionCallback;
    private Action queuedTransitionCallback;

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

    private void Start()
    {
        ApplyThemeImmediate(ResolveThemeIndexForWave(1));
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool WillThemeChangeForWave(int upcomingWave)
    {
        int targetIndex = ResolveThemeIndexForWave(upcomingWave);
        return targetIndex >= 0 && targetIndex != CurrentThemeIndex;
    }

    public void TransitionToWaveTheme(int upcomingWave, Action onComplete = null)
    {
        int targetIndex = ResolveThemeIndexForWave(upcomingWave);
        if (targetIndex < 0 || targetIndex == CurrentThemeIndex)
        {
            onComplete?.Invoke();
            return;
        }

        StartThemeTransition(targetIndex, onComplete);
    }

    private void StartThemeTransition(int targetIndex, Action onComplete = null)
    {
        if (!IsValidThemeIndex(targetIndex))
        {
            onComplete?.Invoke();
            return;
        }

        if (IsTransitioning)
        {
            pendingThemeIndex = targetIndex;
            queuedTransitionCallback = onComplete;
            return;
        }

        IsTransitioning = true;
        pendingThemeIndex = -1;
        activeTransitionCallback = onComplete;

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

        Action completedCallback = activeTransitionCallback;
        activeTransitionCallback = null;

        OnThemeTransitionCompleted?.Invoke(themeIndex);
        completedCallback?.Invoke();

        if (pendingThemeIndex >= 0 && pendingThemeIndex != CurrentThemeIndex)
        {
            int nextThemeIndex = pendingThemeIndex;
            Action nextCallback = queuedTransitionCallback;
            pendingThemeIndex = -1;
            queuedTransitionCallback = null;
            StartThemeTransition(nextThemeIndex, nextCallback);
        }
        else
        {
            pendingThemeIndex = -1;
            queuedTransitionCallback = null;
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

    private int ResolveThemeIndexForWave(int waveNumber)
    {
        if (themes == null || themes.Length == 0)
        {
            return -1;
        }

        int normalizedWave = Mathf.Max(waveNumber, 1);
        return ((normalizedWave - 1) / 10) % themes.Length;
    }

    private bool IsValidThemeIndex(int themeIndex)
    {
        return themes != null && themeIndex >= 0 && themeIndex < themes.Length;
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
