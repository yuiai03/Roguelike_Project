using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class PlinkoGameController : MonoBehaviour
{
    private const string SpawnPointName = "SpawnPoint";
    private const string PinsRootName = "Pins";
    private const string SlotsRootName = "Slots";
    private const string BoardBoundsRootName = "BoardBounds";
    private const string KillZoneRootName = "KillZone";
    private const string FunnelsRootName = "Funnels";
    private const string TemplatesRootName = "Templates";
    private const float SlotTieTolerance = 0.0001f;

    private struct DeterministicSlotData
    {
        public Transform Transform;
        public BoxCollider2D Collider;
        public Vector3 LocalPosition;
        public float LocalX;
        public int RewardValue;
    }

    private static Sprite circleSprite;
    private static Sprite squareSprite;
    private static PhysicsMaterial2D ballPhysicsMaterial;
    private static PhysicsMaterial2D pinPhysicsMaterial;

    [Header("References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject pinPrefab;
    [SerializeField] private GameObject slotPrefab;

    [Header("Layout")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private bool autoSpawnOnEnable = true;
    [SerializeField] private int rowCount = 10;
    [SerializeField] private int firstRowPinCount = 3;
    [SerializeField] private int[] slotValues = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
    [SerializeField] private float layoutScale = 1.5f;
    [SerializeField] private float pinSpacing = 1f;
    [SerializeField] private float rowSpacing = 0.9f;
    [SerializeField] private float slotHeight = 1.35f;
    [SerializeField] private float slotTriggerInset = 0.12f;
    [SerializeField] private float wallThickness = 0.35f;
    [SerializeField] private Vector2 boardPadding = new Vector2(0.85f, 1.25f);
    [SerializeField] private Color backgroundColor = new Color(0.11f, 0.15f, 0.19f, 1f);

    [Header("Physics")]
    [SerializeField, Range(0f, 1f)] private float ballBounciness = 0.65f;
    [SerializeField, Range(0f, 1f)] private float ballFriction = 0.05f;

    [Header("Hierarchy")]
    [SerializeField] private Transform pinsRoot;
    [SerializeField] private Transform slotsRoot;
    [SerializeField] private Transform boardBoundsRoot;
    [SerializeField] private Transform killZoneRoot;
    [SerializeField] private Transform funnelsRoot;
    [SerializeField] private Transform templatesRoot;

    private Coroutine spawnRoutine;
    private bool isBuildingBoard;
    private DeterministicSlotData[] deterministicSlots = new DeterministicSlotData[0];
    private readonly List<PlinkoBall> activeBalls = new List<PlinkoBall>();
    private readonly List<PlinkoFunnel> activeFunnels = new List<PlinkoFunnel>();
    private int lastResolvedRewardValue = -1;
#if UNITY_EDITOR
    private bool editorBuildQueued;
#endif

    private int ExpectedSlotCount => firstRowPinCount + rowCount;
    private int ExpectedPinCount => (rowCount * ((firstRowPinCount * 2) + rowCount - 1)) / 2;
    public int LastResolvedRewardValue => lastResolvedRewardValue;
    public bool AutoSpawnOnEnable => autoSpawnOnEnable;
    public Vector3 SpawnLocalPosition => spawnPoint != null ? spawnPoint.localPosition : Vector3.zero;
    public int SlotCount => deterministicSlots != null && deterministicSlots.Length > 0
        ? deterministicSlots.Length
        : (slotValues != null ? slotValues.Length : 0);

    private void Reset()
    {
        gameObject.name = "PlinkoRoot";
        ApplyDefaultSlotValues();
        EnsureHierarchy();
        EnsureTemplates();
        ConfigureCameraIfPresent();
        QueueBuildBoardInEditor();
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            EnsureHierarchy();
            EnsureTemplates();
            BuildBoard();
            return;
        }

        QueueBuildBoardInEditor();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            if (autoSpawnOnEnable)
            {
                StartSpawning();
            }

            return;
        }

        QueueBuildBoardInEditor();
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.1f, spawnInterval);
        rowCount = Mathf.Max(1, rowCount);
        firstRowPinCount = Mathf.Max(1, firstRowPinCount);
        layoutScale = Mathf.Max(0.5f, layoutScale);
        pinSpacing = Mathf.Max(0.2f, pinSpacing);
        rowSpacing = Mathf.Max(0.2f, rowSpacing);
        slotHeight = Mathf.Max(0.5f, slotHeight);
        slotTriggerInset = Mathf.Clamp(slotTriggerInset, 0.01f, pinSpacing * 0.45f);
        wallThickness = Mathf.Max(0.05f, wallThickness);
        boardPadding.x = Mathf.Max(0.1f, boardPadding.x);
        boardPadding.y = Mathf.Max(0.1f, boardPadding.y);
        ballBounciness = Mathf.Clamp01(ballBounciness);
        ballFriction = Mathf.Clamp01(ballFriction);
        ApplyDefaultSlotValues();
        QueueBuildBoardInEditor();
    }

    [ContextMenu("Rebuild Board")]
    public void BuildBoard()
    {
        if (isBuildingBoard)
        {
            return;
        }

        isBuildingBoard = true;

        try
        {
            EnsureHierarchy();
            EnsureTemplates();
            ApplyDefaultSlotValues();

            ClearChildren(pinsRoot);
            ClearChildren(slotsRoot);
            ClearChildren(boardBoundsRoot);
            ClearChildren(killZoneRoot);
            ClearChildren(funnelsRoot);
            activeFunnels.Clear();

            int slotCount = slotValues.Length;
            int lastRowPinCount = firstRowPinCount + rowCount - 1;
            float scaledPinSpacing = ScaleLayout(pinSpacing);
            float scaledRowSpacing = ScaleLayout(rowSpacing);
            float scaledSlotHeight = ScaleLayout(slotHeight);
            Vector2 scaledBoardPadding = ScaleLayout(boardPadding);
            float halfBoardWidth = slotCount * scaledPinSpacing * 0.5f;
            float slotY = ScaleLayout(-4.6f);
            float topRowY = slotY + scaledSlotHeight + ((rowCount - 1) * scaledRowSpacing) + ScaleLayout(0.9f);
            float boardCenterY = (topRowY + slotY) * 0.5f;
            float boardHeight = (topRowY - slotY) + scaledSlotHeight + scaledBoardPadding.y;

            float spawnPointX = spawnPoint != null ? spawnPoint.localPosition.x : 0f;
            spawnPoint.localPosition = new Vector3(spawnPointX, topRowY + (scaledRowSpacing * 1.65f), 0f);

            CreateBoxVisual(
                "BoardBackground",
                boardBoundsRoot,
                new Vector2((slotCount * scaledPinSpacing) + (scaledBoardPadding.x * 2f), boardHeight + scaledBoardPadding.y),
                new Vector3(0f, boardCenterY, 1f),
                backgroundColor,
                0,
                false,
                false);

            CreateSideWalls(halfBoardWidth, boardCenterY, boardHeight);
            CreateSlotDividers(slotCount, slotY);
            CreateKillZone((slotCount * scaledPinSpacing) + (scaledBoardPadding.x * 2f) + ScaleLayout(2f), slotY - scaledSlotHeight - ScaleLayout(1f));

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                int pinCount = firstRowPinCount + rowIndex;
                float rowY = topRowY - (rowIndex * scaledRowSpacing);

                for (int pinIndex = 0; pinIndex < pinCount; pinIndex++)
                {
                    float pinX = (pinIndex - ((pinCount - 1) * 0.5f)) * scaledPinSpacing;
                    CreatePin(pinX, rowY, rowIndex, pinIndex);
                }
            }

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                float slotX = (slotIndex - ((slotCount - 1) * 0.5f)) * scaledPinSpacing;
                CreateSlot(slotX, slotY, slotValues[slotIndex], slotIndex);
            }

            CreateBottomGuide(lastRowPinCount, slotY);
            CacheDeterministicSlots();
            ConfigureCameraIfPresent();
            Physics2D.SyncTransforms();
        }
        finally
        {
            isBuildingBoard = false;
        }
    }

    public void StartSpawning()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    public void HandleBallScored(int rewardValue, GameObject ball)
    {
        lastResolvedRewardValue = rewardValue;
        Debug.Log($"[Plinko] Ball landed in slot value: {rewardValue}", this);
        DestroyBall(ball);
    }

    public void RemoveBall(GameObject ball)
    {
        DestroyBall(ball);
    }

    public void SpawnBallToSlotIndex(int slotIndex)
    {
        SpawnBallToSlotIndex(slotIndex, SpawnLocalPosition);
    }

    public void SpawnBallToSlotIndex(int slotIndex, Vector3 spawnLocalPosition)
    {
        EnsureHierarchy();
        EnsureTemplates();

        if (ballPrefab == null)
        {
            return;
        }

        Physics2D.SyncTransforms();

        if (!TryGetDeterministicSlot(slotIndex, out int clampedSlotIndex, out DeterministicSlotData slotTarget))
        {
            return;
        }

        SpawnBallToResolvedSlot(clampedSlotIndex, spawnLocalPosition, slotTarget);
    }

    public int ResolveSlotIndexFromLocalX(float localX)
    {
        if (deterministicSlots == null || deterministicSlots.Length == 0)
        {
            CacheDeterministicSlots();
        }

        if (deterministicSlots == null || deterministicSlots.Length == 0)
        {
            return -1;
        }

        if (localX <= deterministicSlots[0].LocalX + SlotTieTolerance)
        {
            return 0;
        }

        for (int slotIndex = 0; slotIndex < deterministicSlots.Length - 1; slotIndex++)
        {
            float midpoint = (deterministicSlots[slotIndex].LocalX + deterministicSlots[slotIndex + 1].LocalX) * 0.5f;
            if (localX <= midpoint + SlotTieTolerance)
            {
                return slotIndex;
            }
        }

        return deterministicSlots.Length - 1;
    }

    private IEnumerator SpawnRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            SpawnBall();
            yield return wait;
        }
    }

    private void SpawnBall()
    {
        if (spawnPoint == null)
        {
            return;
        }

        int slotIndex = ResolveSlotIndexFromLocalX(spawnPoint.localPosition.x);
        if (slotIndex < 0)
        {
            return;
        }

        SpawnBallToSlotIndex(slotIndex);
    }

    private void CreatePin(float xPosition, float yPosition, int rowIndex, int pinIndex)
    {
        GameObject pin = Instantiate(pinPrefab, pinsRoot);
        pin.name = $"Pin_{rowIndex + 1}_{pinIndex + 1}";
        pin.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
        pin.transform.localScale = Vector3.one * ScaleLayout(pinSpacing * 0.4f);
        pin.SetActive(true);
    }

    private void CreateSlot(float xPosition, float yPosition, int rewardValue, int slotIndex)
    {
        GameObject slotObject = Instantiate(slotPrefab, slotsRoot);
        slotObject.name = $"Slot_{slotIndex + 1}";
        slotObject.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
        slotObject.transform.localScale = new Vector3(
            ScaleLayout(pinSpacing - (slotTriggerInset * 2f)),
            ScaleLayout(slotHeight),
            1f);
        slotObject.SetActive(true);

        PlinkoSlot slot = slotObject.GetComponent<PlinkoSlot>();
        if (slot != null)
        {
            slot.Initialize(this, rewardValue);
        }
    }

    private void CreateSideWalls(float halfBoardWidth, float boardCenterY, float boardHeight)
    {
        float wallX = halfBoardWidth + ScaleLayout(boardPadding.x * 0.5f);

        CreateBoxVisual(
            "LeftWall",
            boardBoundsRoot,
            new Vector2(ScaleLayout(wallThickness), boardHeight),
            new Vector3(-wallX, boardCenterY, 0f),
            new Color(0.9f, 0.85f, 0.7f, 1f),
            2,
            true,
            false);

        CreateBoxVisual(
            "RightWall",
            boardBoundsRoot,
            new Vector2(ScaleLayout(wallThickness), boardHeight),
            new Vector3(wallX, boardCenterY, 0f),
            new Color(0.9f, 0.85f, 0.7f, 1f),
            2,
            true,
            false);
    }

    private void CreateSlotDividers(int slotCount, float slotY)
    {
        float scaledPinSpacing = ScaleLayout(pinSpacing);
        float leftBoundary = -((slotCount * scaledPinSpacing) * 0.5f);

        for (int dividerIndex = 0; dividerIndex <= slotCount; dividerIndex++)
        {
            float dividerX = leftBoundary + (dividerIndex * scaledPinSpacing);

            CreateBoxVisual(
                $"Divider_{dividerIndex + 1}",
                boardBoundsRoot,
                new Vector2(ScaleLayout(0.08f), ScaleLayout(slotHeight + 0.55f)),
                new Vector3(dividerX, slotY, 0f),
                new Color(0.82f, 0.68f, 0.34f, 1f),
                3,
                true,
                false);
        }
    }

    private void CreateBottomGuide(int lastRowPinCount, float slotY)
    {
        float guideWidth = (lastRowPinCount * ScaleLayout(pinSpacing)) + ScaleLayout(2f);

        CreateBoxVisual(
            "BottomGuide",
            boardBoundsRoot,
            new Vector2(guideWidth, ScaleLayout(0.18f)),
            new Vector3(0f, slotY - ScaleLayout(slotHeight * 0.65f), 0f),
            new Color(0.75f, 0.56f, 0.2f, 1f),
            2,
            false,
            false);
    }

    private void CreateKillZone(float width, float yPosition)
    {
        killZoneRoot.localPosition = new Vector3(0f, yPosition, 0f);
        killZoneRoot.localScale = new Vector3(width, ScaleLayout(1.2f), 1f);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(killZoneRoot.gameObject);
        renderer.sprite = GetSquareSprite();
        renderer.color = new Color(0.8f, 0.1f, 0.1f, 0.08f);
        renderer.sortingOrder = 1;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(killZoneRoot.gameObject);
        collider.size = Vector2.one;
        collider.isTrigger = true;

        PlinkoKillZone killZone = GetOrAddComponent<PlinkoKillZone>(killZoneRoot.gameObject);
        killZone.Initialize(this);
    }

    private GameObject CreateBoxVisual(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector3 localPosition,
        Color color,
        int sortingOrder,
        bool addCollider,
        bool isTrigger)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSquareSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        if (addCollider)
        {
            BoxCollider2D collider = visual.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = isTrigger;
        }

        return visual;
    }

    private void EnsureHierarchy()
    {
        spawnPoint = EnsureChild(SpawnPointName, transform);
        pinsRoot = EnsureChild(PinsRootName, transform);
        slotsRoot = EnsureChild(SlotsRootName, transform);
        boardBoundsRoot = EnsureChild(BoardBoundsRootName, transform);
        killZoneRoot = EnsureChild(KillZoneRootName, transform);
        funnelsRoot = EnsureChild(FunnelsRootName, transform);
        templatesRoot = EnsureChild(TemplatesRootName, transform);
    }

    private void EnsureTemplates()
    {
        if (templatesRoot == null)
        {
            templatesRoot = EnsureChild(TemplatesRootName, transform);
        }

        if (templatesRoot.gameObject.activeSelf)
        {
            templatesRoot.gameObject.SetActive(false);
        }

        if (ballPrefab == null)
        {
            ballPrefab = CreateBallTemplate();
        }
        else
        {
            ConfigureBallTemplate(ballPrefab);
        }

        if (pinPrefab == null)
        {
            pinPrefab = CreatePinTemplate();
        }
        else
        {
            ConfigurePinTemplate(pinPrefab);
        }

        if (slotPrefab == null)
        {
            slotPrefab = CreateSlotTemplate();
        }
        else
        {
            ConfigureSlotTemplate(slotPrefab);
        }

        if (ballPrefab != null)
        {
            ballPrefab.transform.localScale = Vector3.one * ScaleLayout(pinSpacing * 0.42f);
        }

        if (pinPrefab != null)
        {
            pinPrefab.transform.localScale = Vector3.one * ScaleLayout(pinSpacing * 0.4f);
        }
    }

    private void TryBuildBoardInEditor()
    {
        if (Application.isPlaying || isBuildingBoard)
        {
            return;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return;
        }

        EnsureHierarchy();
        EnsureTemplates();

        if (!NeedsBoardRebuild())
        {
            return;
        }

        BuildBoard();
    }

    private void QueueBuildBoardInEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        if (editorBuildQueued)
        {
            return;
        }

        editorBuildQueued = true;
        AddEditorDelayCall();
#else
        TryBuildBoardInEditor();
#endif
    }

#if UNITY_EDITOR
    private void RunQueuedBuildBoardInEditor()
    {
        editorBuildQueued = false;

        if (this == null)
        {
            return;
        }

        TryBuildBoardInEditor();
    }

    private void AddEditorDelayCall()
    {
        System.Type editorApplicationType = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
        if (editorApplicationType == null)
        {
            return;
        }

        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;
        System.Reflection.FieldInfo delayCallField = editorApplicationType.GetField("delayCall", flags);
        if (delayCallField == null)
        {
            return;
        }

        System.Delegate callback = System.Delegate.CreateDelegate(
            delayCallField.FieldType,
            this,
            nameof(RunQueuedBuildBoardInEditor));
        System.Delegate existingCallback = delayCallField.GetValue(null) as System.Delegate;
        delayCallField.SetValue(null, System.Delegate.Combine(existingCallback, callback));
    }
#endif

    private bool NeedsBoardRebuild()
    {
        float scaledRowSpacing = ScaleLayout(rowSpacing);
        float scaledSlotHeight = ScaleLayout(slotHeight);
        float expectedSlotY = ScaleLayout(-4.6f);
        float expectedTopRowY = expectedSlotY + scaledSlotHeight + ((rowCount - 1) * scaledRowSpacing) + ScaleLayout(0.9f);
        float expectedSpawnY = expectedTopRowY + (scaledRowSpacing * 1.65f);

        if (spawnPoint == null ||
            pinsRoot == null ||
            slotsRoot == null ||
            boardBoundsRoot == null ||
            killZoneRoot == null ||
            funnelsRoot == null ||
            templatesRoot == null)
        {
            return true;
        }

        if (ballPrefab == null || pinPrefab == null || slotPrefab == null)
        {
            return true;
        }

        if (pinsRoot.childCount != ExpectedPinCount)
        {
            return true;
        }

        if (pinsRoot.childCount > 0 && !Mathf.Approximately(pinsRoot.GetChild(0).localScale.x, ScaleLayout(pinSpacing * 0.4f)))
        {
            return true;
        }

        if (pinsRoot.childCount > 0)
        {
            SpriteRenderer pinRenderer = pinsRoot.GetChild(0).GetComponent<SpriteRenderer>();
            if (pinRenderer == null || pinRenderer.sprite == null)
            {
                return true;
            }
        }

        if (!Mathf.Approximately(spawnPoint.localPosition.y, expectedSpawnY))
        {
            return true;
        }

        if (slotsRoot.childCount != ExpectedSlotCount)
        {
            return true;
        }

        if (slotsRoot.childCount > 0 && !Mathf.Approximately(slotsRoot.GetChild(0).localScale.x, ScaleLayout(pinSpacing - (slotTriggerInset * 2f))))
        {
            return true;
        }

        if (boardBoundsRoot.childCount == 0)
        {
            return true;
        }

        if (killZoneRoot.GetComponent<BoxCollider2D>() == null ||
            killZoneRoot.GetComponent<PlinkoKillZone>() == null)
        {
            return true;
        }

        if (!Mathf.Approximately(killZoneRoot.localScale.y, ScaleLayout(1.2f)))
        {
            return true;
        }

        if (ballPrefab != null && !Mathf.Approximately(ballPrefab.transform.localScale.x, ScaleLayout(pinSpacing * 0.42f)))
        {
            return true;
        }

        if (ballPrefab != null)
        {
            CircleCollider2D ballTemplateCollider = ballPrefab.GetComponent<CircleCollider2D>();
            if (ballTemplateCollider == null ||
                ballTemplateCollider.sharedMaterial == null ||
                !Mathf.Approximately(ballTemplateCollider.sharedMaterial.bounciness, ballBounciness) ||
                !Mathf.Approximately(ballTemplateCollider.sharedMaterial.friction, ballFriction))
            {
                return true;
            }
        }

        if (pinPrefab != null)
        {
            SpriteRenderer pinTemplateRenderer = pinPrefab.GetComponent<SpriteRenderer>();
            if (pinTemplateRenderer == null || pinTemplateRenderer.sprite == null)
            {
                return true;
            }
        }

        if (templatesRoot.childCount < 3)
        {
            return true;
        }

        return false;
    }

    private GameObject CreateBallTemplate()
    {
        GameObject template = CreateTemplateRoot("BallTemplate");
        ConfigureBallTemplate(template);
        return template;
    }

    private void ConfigureBallTemplate(GameObject template)
    {
        Rigidbody2D rigidbody2D = GetOrAddComponent<Rigidbody2D>(template);
        rigidbody2D.gravityScale = 1.5f;
        rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody2D.sleepMode = RigidbodySleepMode2D.NeverSleep;

        CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(template);
        collider.radius = 0.5f;
        collider.sharedMaterial = GetBallPhysicsMaterial();

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(template);
        renderer.sprite = GetCircleSprite();
        renderer.color = new Color(1f, 0.63f, 0.22f, 1f);
        renderer.sortingOrder = 5;

        template.transform.localScale = Vector3.one * ScaleLayout(pinSpacing * 0.42f);
        GetOrAddComponent<PlinkoBall>(template);
    }

    private GameObject CreatePinTemplate()
    {
        GameObject template = CreateTemplateRoot("PinTemplate");
        ConfigurePinTemplate(template);
        return template;
    }

    private void ConfigurePinTemplate(GameObject template)
    {
        CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(template);
        collider.radius = 0.5f;
        collider.sharedMaterial = GetPinPhysicsMaterial();

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(template);
        renderer.sprite = GetCircleSprite();
        renderer.color = new Color(0.88f, 0.84f, 0.74f, 1f);
        renderer.sortingOrder = 4;

        template.transform.localScale = Vector3.one * ScaleLayout(pinSpacing * 0.4f);
    }

    private GameObject CreateSlotTemplate()
    {
        GameObject template = CreateTemplateRoot("SlotTemplate");
        ConfigureSlotTemplate(template);
        return template;
    }

    private void ConfigureSlotTemplate(GameObject template)
    {
        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(template);
        collider.size = Vector2.one;
        collider.isTrigger = true;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(template);
        renderer.sprite = GetSquareSprite();
        renderer.color = new Color(0.2f, 0.54f, 0.71f, 0.95f);
        renderer.sortingOrder = 2;

        PlinkoSlot slot = GetOrAddComponent<PlinkoSlot>(template);
        TextMeshPro textMesh = template.GetComponentInChildren<TextMeshPro>(true);

        if (textMesh == null)
        {
            GameObject labelObject = new GameObject("ValueLabel");
            labelObject.transform.SetParent(template.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, ScaleLayout(-0.05f), -0.1f);
            labelObject.transform.localScale = Vector3.one * ScaleLayout(0.18f);

            textMesh = labelObject.AddComponent<TextMeshPro>();
            textMesh.text = "0";
            textMesh.fontSize = ScaleLayout(7f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else
        {
            textMesh.transform.localPosition = new Vector3(0f, ScaleLayout(-0.05f), -0.1f);
            textMesh.transform.localScale = Vector3.one * ScaleLayout(0.18f);
            textMesh.fontSize = ScaleLayout(7f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        }

        slot.SetLabel(textMesh);
    }

    private GameObject CreateTemplateRoot(string objectName)
    {
        Transform existing = templatesRoot.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject template = new GameObject(objectName);
        template.transform.SetParent(templatesRoot, false);
        template.SetActive(false);
        return template;
    }

    private void ApplyDefaultSlotValues()
    {
        int expectedSlotCount = ExpectedSlotCount;
        if (slotValues != null && slotValues.Length == expectedSlotCount)
        {
            return;
        }

        slotValues = new int[expectedSlotCount];
        for (int i = 0; i < expectedSlotCount; i++)
        {
            slotValues[i] = i + 1;
        }
    }

    private void ConfigureCameraIfPresent()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        float scaledPinSpacing = ScaleLayout(pinSpacing);
        float scaledRowSpacing = ScaleLayout(rowSpacing);
        float scaledSlotHeight = ScaleLayout(slotHeight);
        Vector2 scaledBoardPadding = ScaleLayout(boardPadding);
        float slotY = ScaleLayout(-4.6f);
        float topRowY = slotY + scaledSlotHeight + ((rowCount - 1) * scaledRowSpacing) + ScaleLayout(0.9f);
        float contentTop = topRowY + ScaleLayout(1.4f);
        float contentBottom = slotY - scaledSlotHeight - ScaleLayout(0.9f);
        float centerY = (contentTop + contentBottom) * 0.5f;
        float viewHalfHeight = ((contentTop - contentBottom) * 0.5f) + ScaleLayout(0.45f);
        float boardHalfWidth = (((ExpectedSlotCount * scaledPinSpacing) + (scaledBoardPadding.x * 2f)) * 0.5f) + ScaleLayout(0.35f);

        targetCamera.orthographic = true;
        targetCamera.backgroundColor = new Color(0.85f, 0.93f, 0.97f, 1f);
        targetCamera.transform.position = new Vector3(0f, centerY, -10f);
        targetCamera.orthographicSize = Mathf.Max(8f, viewHalfHeight, boardHalfWidth / Mathf.Max(0.1f, targetCamera.aspect));
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
        {
            GameObject child = parent.GetChild(childIndex).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void CacheDeterministicSlots()
    {
        if (slotsRoot == null)
        {
            deterministicSlots = new DeterministicSlotData[0];
            return;
        }

        deterministicSlots = new DeterministicSlotData[slotsRoot.childCount];
        for (int slotIndex = 0; slotIndex < slotsRoot.childCount; slotIndex++)
        {
            Transform slotTransform = slotsRoot.GetChild(slotIndex);
            PlinkoSlot slot = slotTransform.GetComponent<PlinkoSlot>();
            deterministicSlots[slotIndex] = new DeterministicSlotData
            {
                Transform = slotTransform,
                Collider = slotTransform.GetComponent<BoxCollider2D>(),
                LocalPosition = slotTransform.localPosition,
                LocalX = slotTransform.localPosition.x,
                RewardValue = slot != null ? slot.RewardValue : 0
            };
        }

        System.Array.Sort(deterministicSlots, CompareDeterministicSlots);
    }

    private static int CompareDeterministicSlots(DeterministicSlotData left, DeterministicSlotData right)
    {
        if (left.LocalX < right.LocalX)
        {
            return -1;
        }

        if (left.LocalX > right.LocalX)
        {
            return 1;
        }

        return 0;
    }

    private void DestroyBall(GameObject ball)
    {
        if (ball == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(ball);
            return;
        }

        DestroyImmediate(ball);
    }

    internal void RegisterActiveBall(PlinkoBall ball)
    {
        if (ball == null)
        {
            return;
        }

        RemoveDestroyedEntries(activeBalls);
        if (activeBalls.Contains(ball))
        {
            return;
        }

        activeBalls.Add(ball);
        for (int funnelIndex = activeFunnels.Count - 1; funnelIndex >= 0; funnelIndex--)
        {
            PlinkoFunnel funnel = activeFunnels[funnelIndex];
            if (funnel == null)
            {
                activeFunnels.RemoveAt(funnelIndex);
                continue;
            }

            funnel.ConfigureCollisionForBall(ball);
        }
    }

    internal void UnregisterActiveBall(PlinkoBall ball)
    {
        if (ball == null)
        {
            return;
        }

        activeBalls.Remove(ball);
    }

    internal void RegisterFunnel(PlinkoFunnel funnel)
    {
        if (funnel == null)
        {
            return;
        }

        RemoveDestroyedEntries(activeFunnels);
        if (activeFunnels.Contains(funnel))
        {
            return;
        }

        activeFunnels.Add(funnel);
        for (int ballIndex = activeBalls.Count - 1; ballIndex >= 0; ballIndex--)
        {
            PlinkoBall ball = activeBalls[ballIndex];
            if (ball == null)
            {
                activeBalls.RemoveAt(ballIndex);
                continue;
            }

            funnel.ConfigureCollisionForBall(ball);
        }
    }

    internal void UnregisterFunnel(PlinkoFunnel funnel)
    {
        if (funnel == null)
        {
            return;
        }

        activeFunnels.Remove(funnel);
    }

    private Transform EnsureChild(string childName, Transform parent)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private float ScaleLayout(float value)
    {
        return value * layoutScale;
    }

    private Vector2 ScaleLayout(Vector2 value)
    {
        return value * layoutScale;
    }

    private void SpawnBallToResolvedSlot(int slotIndex, Vector3 spawnLocalPosition, DeterministicSlotData slotTarget)
    {
        lastResolvedRewardValue = -1;
        GameObject ballInstance = Instantiate(ballPrefab, transform);
        ballInstance.name = "PlinkoBall";
        ballInstance.transform.localPosition = spawnLocalPosition;
        ballInstance.transform.localRotation = Quaternion.identity;
        ballInstance.SetActive(true);

        PlinkoBall ball = ballInstance.GetComponent<PlinkoBall>();
        if (ball == null)
        {
            DestroyBall(ballInstance);
            return;
        }

        ball.InitializeLocked(this, spawnLocalPosition, slotIndex, slotTarget.Transform, slotTarget.RewardValue);
        RegisterActiveBall(ball);
        ball.SetOwnedFunnel(CreateFunnelForBall(ball, slotTarget));
    }

    private bool TryGetDeterministicSlot(int slotIndex, out int clampedSlotIndex, out DeterministicSlotData slotTarget)
    {
        clampedSlotIndex = -1;
        slotTarget = default;

        if (deterministicSlots == null || deterministicSlots.Length == 0)
        {
            CacheDeterministicSlots();
        }

        if (deterministicSlots == null || deterministicSlots.Length == 0)
        {
            return false;
        }

        clampedSlotIndex = Mathf.Clamp(slotIndex, 0, deterministicSlots.Length - 1);
        slotTarget = deterministicSlots[clampedSlotIndex];
        return slotTarget.Transform != null;
    }

    private PlinkoFunnel CreateFunnelForBall(PlinkoBall ball, DeterministicSlotData slotTarget)
    {
        if (ball == null || slotTarget.Transform == null || funnelsRoot == null)
        {
            return null;
        }

        float scaledPinSpacing = ScaleLayout(pinSpacing);
        float scaledRowSpacing = ScaleLayout(rowSpacing);
        float scaledSlotHeight = ScaleLayout(slotHeight);
        float slotTriggerWidth = GetSlotTriggerWidth(slotTarget);
        float ballLocalRadius = ball.GetLocalRadius();
        float slotCenterY = slotTarget.LocalPosition.y;
        float funnelStartY = GetPinRowLocalY(Mathf.Clamp(rowCount - 3, 0, rowCount - 1)) - (scaledRowSpacing * 0.15f);
        float biasEndY = slotCenterY + (scaledSlotHeight * 0.55f);
        float captureEndY = slotCenterY + (scaledSlotHeight * 0.08f);
        float mouthHalfWidth = scaledPinSpacing * 1.25f;
        float throatHalfWidth = Mathf.Max(ballLocalRadius * 1.15f, slotTriggerWidth * 0.32f);
        float railThickness = Mathf.Max(scaledPinSpacing * 0.12f, 0.08f);

        GameObject funnelObject = new GameObject($"Funnel_{slotTarget.RewardValue}_{ball.GetInstanceID()}");
        funnelObject.transform.SetParent(funnelsRoot, false);

        PlinkoFunnel funnel = funnelObject.AddComponent<PlinkoFunnel>();
        funnel.Initialize(
            this,
            ball,
            slotTarget.Transform,
            slotTarget.LocalPosition,
            funnelStartY,
            biasEndY,
            captureEndY,
            mouthHalfWidth,
            throatHalfWidth,
            railThickness);
        RegisterFunnel(funnel);
        return funnel;
    }

    private float GetSlotTriggerWidth(DeterministicSlotData slotTarget)
    {
        if (slotTarget.Collider != null)
        {
            return Mathf.Abs(slotTarget.Transform.localScale.x * slotTarget.Collider.size.x);
        }

        return ScaleLayout(pinSpacing - (slotTriggerInset * 2f));
    }

    private float GetSlotLocalY()
    {
        return ScaleLayout(-4.6f);
    }

    private float GetTopRowLocalY()
    {
        return GetSlotLocalY() + ScaleLayout(slotHeight) + ((rowCount - 1) * ScaleLayout(rowSpacing)) + ScaleLayout(0.9f);
    }

    private float GetPinRowLocalY(int rowIndex)
    {
        return GetTopRowLocalY() - (rowIndex * ScaleLayout(rowSpacing));
    }

    private static void RemoveDestroyedEntries<T>(List<T> items) where T : Object
    {
        for (int itemIndex = items.Count - 1; itemIndex >= 0; itemIndex--)
        {
            if (items[itemIndex] == null)
            {
                items.RemoveAt(itemIndex);
            }
        }
    }

    private PhysicsMaterial2D GetBallPhysicsMaterial()
    {
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = new PhysicsMaterial2D();
            ballPhysicsMaterial.name = "Generated Plinko Ball Material";
            ballPhysicsMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        ballPhysicsMaterial.bounciness = ballBounciness;
        ballPhysicsMaterial.friction = ballFriction;
        ballPhysicsMaterial.bounceCombine = PhysicsMaterialCombine2D.Maximum;
        ballPhysicsMaterial.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        return ballPhysicsMaterial;
    }

    private PhysicsMaterial2D GetPinPhysicsMaterial()
    {
        if (pinPhysicsMaterial == null)
        {
            pinPhysicsMaterial = new PhysicsMaterial2D();
            pinPhysicsMaterial.name = "Generated Plinko Pin Material";
            pinPhysicsMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        pinPhysicsMaterial.bounciness = ballBounciness;
        pinPhysicsMaterial.friction = 0f;
        pinPhysicsMaterial.bounceCombine = PhysicsMaterialCombine2D.Maximum;
        pinPhysicsMaterial.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        return pinPhysicsMaterial;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.48f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
        return circleSprite;
    }

    private static Sprite GetSquareSprite()
    {
        if (squareSprite != null)
        {
            return squareSprite;
        }

        const int textureSize = 4;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        squareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        squareSprite.hideFlags = HideFlags.HideAndDontSave;
        return squareSprite;
    }
}
