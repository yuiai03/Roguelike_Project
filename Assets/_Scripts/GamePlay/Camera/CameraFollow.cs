using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 10f, -8f);

    [Header("Height Lock")]
    public bool lockHeight = true;
    public float fixedHeight = 10f;

    [Header("Zoom Settings")]
    public float minHeight = 30f;
    public float maxHeight = 40f;
    public float zoomSpeed = 2f;

    void Update()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                float zoomChange = -Mathf.Sign(scroll) * zoomSpeed;
                
                if (lockHeight)
                {
                    fixedHeight = Mathf.Clamp(fixedHeight + zoomChange, minHeight, maxHeight);
                }
                else
                {
                    offset.y = Mathf.Clamp(offset.y + zoomChange, minHeight, maxHeight);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (lockHeight)
        {
            desiredPosition.y = fixedHeight;
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
