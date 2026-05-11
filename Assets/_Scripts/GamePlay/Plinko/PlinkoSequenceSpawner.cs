using UnityEngine;

[DisallowMultipleComponent]
public class PlinkoSequenceSpawner : MonoBehaviour
{
    [SerializeField] private PlinkoGameController plinkoController;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool ensureUnpausedOnStart = true;
    [SerializeField] private bool slotValuesAreOneBased = true;
    [SerializeField] private float spawnGap = 0.2f;
    [SerializeField] private float spawnVerticalOffsetPerBall = 0.9f;
    [SerializeField] private int[] targetSlots = { 1, 3, 5 };
    [SerializeField] private int debugSpawnCount;
    [SerializeField] private int[] debugSpawnedSlotIndices = System.Array.Empty<int>();
    [SerializeField] private float[] debugSpawnedAtTimes = System.Array.Empty<float>();

    private bool sequenceRunning;
    private int nextSequenceIndex;
    private float nextSpawnAt;

    public int[] TargetSlots => targetSlots;
    public int DebugSpawnCount => debugSpawnCount;
    public int[] DebugSpawnedSlotIndices => debugSpawnedSlotIndices;
    public float[] DebugSpawnedAtTimes => debugSpawnedAtTimes;
    public bool IsSequenceRunning => sequenceRunning;
    private float SequenceClock => Time.unscaledTime;

    private void Reset()
    {
        CacheControllerReference();
    }

    private void Awake()
    {
        CacheControllerReference();
    }

    private void Start()
    {
        if (!Application.isPlaying || !playOnEnable)
        {
            return;
        }

        StartConfiguredSequence();
    }

    private void OnDisable()
    {
        StopConfiguredSequence();
    }

    private void Update()
    {
        if (!Application.isPlaying || !sequenceRunning || nextSequenceIndex >= targetSlots.Length)
        {
            return;
        }

        if (SequenceClock < nextSpawnAt)
        {
            return;
        }

        SpawnCurrentSequenceBall();
    }

    [ContextMenu("Spawn Configured Sequence")]
    public void StartConfiguredSequence()
    {
        CacheControllerReference();
        StopConfiguredSequence();

        if (!Application.isPlaying || plinkoController == null || targetSlots == null || targetSlots.Length == 0)
        {
            return;
        }

        if (ensureUnpausedOnStart && !Mathf.Approximately(Time.timeScale, 1f))
        {
            Time.timeScale = 1f;
        }

        sequenceRunning = true;
        nextSequenceIndex = 0;
        nextSpawnAt = SequenceClock;
        debugSpawnCount = 0;
        debugSpawnedSlotIndices = System.Array.Empty<int>();
        debugSpawnedAtTimes = System.Array.Empty<float>();
        SpawnCurrentSequenceBall();
    }

    public void StopConfiguredSequence()
    {
        sequenceRunning = false;
        nextSequenceIndex = 0;
        nextSpawnAt = 0f;
    }

    public void SetTargetSlots(int[] configuredSlots, bool valuesAreOneBased)
    {
        targetSlots = configuredSlots ?? System.Array.Empty<int>();
        slotValuesAreOneBased = valuesAreOneBased;
    }

    private void CacheControllerReference()
    {
        if (plinkoController != null)
        {
            return;
        }

        plinkoController = GetComponent<PlinkoGameController>();
        if (plinkoController != null)
        {
            return;
        }

        plinkoController = GetComponentInParent<PlinkoGameController>();
    }

    private void SpawnCurrentSequenceBall()
    {
        int slotIndex = slotValuesAreOneBased ? targetSlots[nextSequenceIndex] - 1 : targetSlots[nextSequenceIndex];
        Vector3 spawnLocalPosition = plinkoController.SpawnLocalPosition + (Vector3.up * (spawnVerticalOffsetPerBall * nextSequenceIndex));
        plinkoController.SpawnBallToSlotIndex(slotIndex, spawnLocalPosition);
        AppendDebugEntry(slotIndex, SequenceClock);
        Debug.Log($"[PlinkoSequenceSpawner] Spawned slotIndex {slotIndex} at t={SequenceClock:F2}", this);
        nextSequenceIndex++;

        if (nextSequenceIndex >= targetSlots.Length)
        {
            sequenceRunning = false;
            return;
        }

        nextSpawnAt = SequenceClock + Mathf.Max(0f, spawnGap);
    }

    private void AppendDebugEntry(int slotIndex, float spawnedAtTime)
    {
        debugSpawnCount++;

        int[] spawnedSlotIndices = new int[debugSpawnCount];
        float[] spawnedAtTimes = new float[debugSpawnCount];

        for (int index = 0; index < debugSpawnCount - 1; index++)
        {
            spawnedSlotIndices[index] = debugSpawnedSlotIndices[index];
            spawnedAtTimes[index] = debugSpawnedAtTimes[index];
        }

        spawnedSlotIndices[debugSpawnCount - 1] = slotIndex;
        spawnedAtTimes[debugSpawnCount - 1] = spawnedAtTime;

        debugSpawnedSlotIndices = spawnedSlotIndices;
        debugSpawnedAtTimes = spawnedAtTimes;
    }
}
