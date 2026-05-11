using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlinkoFunnel : MonoBehaviour
{
    private readonly List<BoxCollider2D> wallColliders = new List<BoxCollider2D>(4);

    private PlinkoGameController ownerController;
    private PlinkoBall ownerBall;
    private Transform lockedSlot;
    private Vector2 slotCenterLocal;
    private float funnelStartY;
    private float biasEndY;
    private float captureEndY;
    private float mouthHalfWidth;
    private float throatHalfWidth;
    private float railThickness;

    public PlinkoBall OwnerBall => ownerBall;
    public Transform LockedSlot => lockedSlot;
    public IReadOnlyList<BoxCollider2D> WallColliders => wallColliders;
    public Vector2 SlotCenterLocal => slotCenterLocal;
    public float FunnelStartY => funnelStartY;
    public float BiasEndY => biasEndY;
    public float CaptureEndY => captureEndY;
    public float MouthHalfWidth => mouthHalfWidth;
    public float ThroatHalfWidth => throatHalfWidth;
    public float RailThickness => railThickness;

    public void Initialize(
        PlinkoGameController controller,
        PlinkoBall ball,
        Transform targetSlot,
        Vector2 slotCenter,
        float startY,
        float biasY,
        float captureY,
        float mouthWidth,
        float throatWidth,
        float thickness)
    {
        ownerController = controller;
        ownerBall = ball;
        lockedSlot = targetSlot;
        slotCenterLocal = slotCenter;
        funnelStartY = startY;
        biasEndY = biasY;
        captureEndY = captureY;
        mouthHalfWidth = mouthWidth;
        throatHalfWidth = throatWidth;
        railThickness = thickness;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        BuildGeometry();
    }

    public void ConfigureCollisionForBall(PlinkoBall ball)
    {
        if (ball == null || ball.CachedCollider == null)
        {
            return;
        }

        bool shouldIgnore = ball != ownerBall;
        for (int wallIndex = 0; wallIndex < wallColliders.Count; wallIndex++)
        {
            BoxCollider2D wallCollider = wallColliders[wallIndex];
            if (wallCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(ball.CachedCollider, wallCollider, shouldIgnore);
        }
    }

    private void OnDestroy()
    {
        if (ownerController != null)
        {
            ownerController.UnregisterFunnel(this);
        }
    }

    private void BuildGeometry()
    {
        ClearChildren();
        wallColliders.Clear();

        float slotCenterX = slotCenterLocal.x;
        CreateRail(
            "LeftBiasRail",
            new Vector2(slotCenterX - mouthHalfWidth, funnelStartY),
            new Vector2(slotCenterX - throatHalfWidth, biasEndY));
        CreateRail(
            "RightBiasRail",
            new Vector2(slotCenterX + mouthHalfWidth, funnelStartY),
            new Vector2(slotCenterX + throatHalfWidth, biasEndY));
        CreateCaptureWall("LeftCaptureWall", slotCenterX - throatHalfWidth);
        CreateCaptureWall("RightCaptureWall", slotCenterX + throatHalfWidth);
    }

    private void CreateRail(string wallName, Vector2 start, Vector2 end)
    {
        GameObject wallObject = new GameObject(wallName);
        wallObject.transform.SetParent(transform, false);
        wallObject.transform.localScale = Vector3.one;

        Vector2 delta = end - start;
        wallObject.transform.localPosition = (start + end) * 0.5f;
        wallObject.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        BoxCollider2D collider = wallObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(Mathf.Max(delta.magnitude, railThickness), railThickness);
        wallColliders.Add(collider);
    }

    private void CreateCaptureWall(string wallName, float xPosition)
    {
        GameObject wallObject = new GameObject(wallName);
        wallObject.transform.SetParent(transform, false);
        wallObject.transform.localScale = Vector3.one;
        wallObject.transform.localPosition = new Vector2(xPosition, (biasEndY + captureEndY) * 0.5f);
        wallObject.transform.localRotation = Quaternion.identity;

        BoxCollider2D collider = wallObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(railThickness, Mathf.Max(Mathf.Abs(biasEndY - captureEndY), railThickness));
        wallColliders.Add(collider);
    }

    private void ClearChildren()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            GameObject child = transform.GetChild(childIndex).gameObject;
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
}
