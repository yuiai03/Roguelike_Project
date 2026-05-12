using UnityEngine;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    public const string MusicVolumeKey = "music_volume";
    public const string SfxVolumeKey = "sfx_volume";

    [Header("Data")]
    [SerializeField] private AudioCueDatabase cueDatabase;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource worldSource;

    [Header("Mix")]
    [SerializeField] private float duckedMusicMultiplier = 0.45f;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool isMusicDucked;
    private bool hasSwitchedToBattleMusic;
    private float currentMusicCueVolumeScale = 1f;
    private WaveSpawner subscribedWaveSpawner;
    private readonly Dictionary<AudioCue, float> lastCuePlayTimes = new Dictionary<AudioCue, float>();

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerRoot = GameObject.Find("-----Manager-----");
        GameObject audioManagerObject = new GameObject("AudioManager");
        if (managerRoot != null)
        {
            audioManagerObject.transform.SetParent(managerRoot.transform, false);
        }

        audioManagerObject.AddComponent<AudioManager>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        EnsureDependencies();
        LoadSettings();
        ApplyVolumes();
    }

    private void Start()
    {
        PlayMusic(AudioCue.PreBattleMusic, restartIfSame: true);
        TrySubscribeToWaveSpawner();
    }

    private void OnDestroy()
    {
        UnsubscribeFromWaveSpawner();
    }

    private void Reset()
    {
        EnsureDependencies();
    }

    private void OnValidate()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        EnsureDependencies();
        ApplyVolumes();
    }

    public void PlayMusic(AudioCue cue, bool restartIfSame = false)
    {
        if (musicSource == null)
        {
            return;
        }

        AudioClip clip = GetClip(cue, out float volumeScale, out _, out _);
        if (clip == null)
        {
            return;
        }

        if (!restartIfSame && musicSource.isPlaying && musicSource.clip == clip)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
        currentMusicCueVolumeScale = volumeScale;
        ApplyMusicVolume();
    }

    public void PlayUISfx(AudioCue cue)
    {
        PlayOneShot(uiSource, cue, sfxVolume);
    }

    public void PlayWorldSfx(AudioCue cue)
    {
        PlayOneShot(worldSource, cue, sfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicDucked(bool ducked)
    {
        isMusicDucked = ducked;
        ApplyVolumes();
    }

    private void TrySubscribeToWaveSpawner()
    {
        WaveSpawner waveSpawner = WaveSpawner.Instance;
        if (waveSpawner == null || subscribedWaveSpawner == waveSpawner)
        {
            return;
        }

        UnsubscribeFromWaveSpawner();
        subscribedWaveSpawner = waveSpawner;
        subscribedWaveSpawner.OnWaveStart.AddListener(HandleWaveStart);
    }

    private void UnsubscribeFromWaveSpawner()
    {
        if (subscribedWaveSpawner == null)
        {
            return;
        }

        subscribedWaveSpawner.OnWaveStart.RemoveListener(HandleWaveStart);
        subscribedWaveSpawner = null;
    }

    private void HandleWaveStart(int waveNumber)
    {
        if (hasSwitchedToBattleMusic || waveNumber != 1)
        {
            return;
        }

        hasSwitchedToBattleMusic = true;
        PlayMusic(AudioCue.GameMusic, restartIfSame: true);
    }

    private void PlayOneShot(AudioSource source, AudioCue cue, float busVolume)
    {
        if (source == null)
        {
            return;
        }

        AudioClip clip = GetClip(cue, out float volumeScale, out float pitch, out float minInterval);
        if (clip == null)
        {
            return;
        }

        if (minInterval > 0f)
        {
            float now = Time.unscaledTime;
            if (lastCuePlayTimes.TryGetValue(cue, out float lastPlayTime) && now - lastPlayTime < minInterval)
            {
                return;
            }

            lastCuePlayTimes[cue] = now;
        }

        source.pitch = pitch;
        source.PlayOneShot(clip, busVolume * volumeScale);
    }

    private AudioClip GetClip(AudioCue cue, out float volumeScale, out float pitch, out float minInterval)
    {
        volumeScale = 1f;
        pitch = 1f;
        minInterval = 0f;

        if (cueDatabase == null)
        {
            cueDatabase = Resources.Load<AudioCueDatabase>("Audio/DefaultAudioCueDatabase");
        }

        if (cueDatabase == null)
        {
            return null;
        }

        if (cueDatabase.TryGetCue(cue, out AudioCueDefinition definition))
        {
            minInterval = Mathf.Max(0f, definition.minInterval);
        }

        return cueDatabase.GetRandomClip(cue, out volumeScale, out pitch);
    }

    private void EnsureDependencies()
    {
        if (musicSource == null)
        {
            musicSource = GetOrCreateSource("MusicSource");
            musicSource.loop = true;
        }

        if (uiSource == null)
        {
            uiSource = GetOrCreateSource("UISource");
            uiSource.ignoreListenerPause = true;
        }

        if (worldSource == null)
        {
            worldSource = GetOrCreateSource("WorldSource");
        }

        ConfigureSource(musicSource);
        ConfigureSource(uiSource);
        ConfigureSource(worldSource);
    }

    private AudioSource GetOrCreateSource(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        AudioSource source = child.GetComponent<AudioSource>();
        if (source == null)
        {
            source = child.gameObject.AddComponent<AudioSource>();
        }

        return source;
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    private void ApplyVolumes()
    {
        ApplyMusicVolume();

        if (uiSource != null)
        {
            uiSource.volume = sfxVolume;
        }

        if (worldSource != null)
        {
            worldSource.volume = sfxVolume;
        }
    }

    private void ApplyMusicVolume()
    {
        ApplyMusicVolume(currentMusicCueVolumeScale);
    }

    private void ApplyMusicVolume(float cueVolumeScale)
    {
        if (musicSource == null)
        {
            return;
        }

        float duckScale = isMusicDucked ? duckedMusicMultiplier : 1f;
        musicSource.volume = musicVolume * cueVolumeScale * duckScale;
    }
}
