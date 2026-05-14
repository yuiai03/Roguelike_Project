using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TimeScaleResetEditor
{
    private const float NormalTimeScale = 1f;

    static TimeScaleResetEditor()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.delayCall += ResetEditModeTimeScaleIfNeeded;
    }

    [MenuItem("Tools/Debug/Reset Time Scale")]
    public static void ResetTimeScale()
    {
        Time.timeScale = NormalTimeScale;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("Time.timeScale reset to 1.");
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            ResetTimeScale();
        }
    }

    private static void ResetEditModeTimeScaleIfNeeded()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !Mathf.Approximately(Time.timeScale, NormalTimeScale))
        {
            ResetTimeScale();
        }
    }
}
