using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioCueDatabase", menuName = "Roguelike/Audio Cue Database")]
public class AudioCueDatabase : ScriptableObject
{
    [SerializeField] private List<AudioCueDefinition> cues = new List<AudioCueDefinition>();

    private Dictionary<AudioCue, AudioCueDefinition> lookup;

    private void OnEnable()
    {
        EnsureAllCueSlots();
        RebuildLookup();
    }

    private void OnValidate()
    {
        EnsureAllCueSlots();
        RebuildLookup();
    }

    public bool TryGetCue(AudioCue cue, out AudioCueDefinition definition)
    {
        if (lookup == null || lookup.Count != cues.Count)
        {
            RebuildLookup();
        }

        return lookup.TryGetValue(cue, out definition);
    }

    public AudioClip GetRandomClip(AudioCue cue, out float volumeScale, out float pitch)
    {
        volumeScale = 1f;
        pitch = 1f;

        if (!TryGetCue(cue, out AudioCueDefinition definition))
        {
            return null;
        }

        volumeScale = Mathf.Clamp01(definition.volumeScale);
        pitch = UnityEngine.Random.Range(
            Mathf.Min(definition.pitchRange.x, definition.pitchRange.y),
            Mathf.Max(definition.pitchRange.x, definition.pitchRange.y));

        if (definition.clips == null || definition.clips.Length == 0)
        {
            return null;
        }

        if (definition.clips.Length == 1)
        {
            return definition.clips[0];
        }

        return definition.clips[UnityEngine.Random.Range(0, definition.clips.Length)];
    }

    private void RebuildLookup()
    {
        lookup = new Dictionary<AudioCue, AudioCueDefinition>();
        foreach (AudioCueDefinition definition in cues)
        {
            if (definition == null)
            {
                continue;
            }

            lookup[definition.cue] = definition;
        }
    }

    private void EnsureAllCueSlots()
    {
        if (cues == null)
        {
            cues = new List<AudioCueDefinition>();
        }

        foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
        {
            bool exists = false;
            foreach (AudioCueDefinition definition in cues)
            {
                if (definition != null && definition.cue == cue)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                cues.Add(new AudioCueDefinition { cue = cue });
            }
        }

        cues.RemoveAll(definition => definition == null);
        cues.Sort((left, right) => left.cue.CompareTo(right.cue));
    }
}

[Serializable]
public class AudioCueDefinition
{
    public AudioCue cue;
    public AudioClip[] clips;

    [Range(0f, 1f)]
    public float volumeScale = 1f;

    public Vector2 pitchRange = Vector2.one;
}
