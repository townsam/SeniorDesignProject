using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float rotateSpeed = 100f;
    public float panSpeed = 1f;
    /// <summary>Ground-plane pan speed when using WASD (world units per second).</summary>
    public float keyboardPanSpeed = 18f;
    public float zoomSpeed = 500f;
    public float minZoom = 10f;
    public float maxZoom = 50f;

    public InputActionAsset inputActions;

    private InputAction mouseDeltaAction;
    private InputAction rightMouseAction;
    private InputAction middleMouseAction;
    private InputAction zoomAction;

    private float distance = 20f;
    private float currentXAngle = 45f;
    private float currentYAngle = 0f;

    private Vector3 initialTargetPosition;
    private float initialDistance;
    private float initialXAngle;
    private float initialYAngle;
    private bool hasBaseline;

    void Awake()
    {
        var map = inputActions.FindActionMap("Player");

        mouseDeltaAction = map.FindAction("MouseDelta");
        rightMouseAction = map.FindAction("RightMouse");
        middleMouseAction = map.FindAction("MiddleMouse");
        zoomAction = map.FindAction("Zoom");

        mouseDeltaAction.Enable();
        rightMouseAction.Enable();
        middleMouseAction.Enable();
        zoomAction.Enable();
    }

    void Start()
    {
        CaptureViewBaseline();
    }

    /// <summary>Saves current orbit target, distance, and angles (used by <see cref="ResetView"/>).</summary>
    public void CaptureViewBaseline()
    {
        if (target != null)
        {
            initialTargetPosition = target.position;
        }

        initialDistance = distance;
        initialXAngle = currentXAngle;
        initialYAngle = currentYAngle;
        hasBaseline = true;
    }

    /// <summary>Restore camera to the baseline from <see cref="Start"/> or last <see cref="CaptureViewBaseline"/>.</summary>
    public void ResetView()
    {
        if (!hasBaseline)
        {
            return;
        }

        if (target != null)
        {
            target.position = initialTargetPosition;
        }

        distance = initialDistance;
        currentXAngle = initialXAngle;
        currentYAngle = initialYAngle;
    }

    void LateUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            ResetView();
        }

        Vector2 mouseDelta = mouseDeltaAction.ReadValue<Vector2>();

        // Rotate
        if (rightMouseAction.ReadValue<float>() > 0.5f)
        {
            currentYAngle += mouseDelta.x * rotateSpeed * Time.deltaTime;
            currentXAngle -= mouseDelta.y * rotateSpeed * Time.deltaTime;
            currentXAngle = Mathf.Clamp(currentXAngle, 20f, 80f);
        }

        Vector3 right = transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);

        // Pan (aligned with camera)
        if (middleMouseAction.ReadValue<float>() > 0.5f)
        {
            Vector3 pan = (right * -mouseDelta.x + forward * -mouseDelta.y) * panSpeed * Time.deltaTime;
            target.position += pan;
        }

        // Keyboard pan (same axes as middle-mouse)
        if (kb != null && target != null)
        {
            Vector2 wasd = Vector2.zero;
            if (kb.wKey.isPressed)
            {
                wasd.y += 1f;
            }

            if (kb.sKey.isPressed)
            {
                wasd.y -= 1f;
            }

            if (kb.aKey.isPressed)
            {
                wasd.x -= 1f;
            }

            if (kb.dKey.isPressed)
            {
                wasd.x += 1f;
            }

            if (wasd.sqrMagnitude > 1e-6f)
            {
                wasd.Normalize();
                target.position += (right * wasd.x + forward * wasd.y) * keyboardPanSpeed * Time.deltaTime;
            }
        }

        // Zoom
        float zoomInput = zoomAction.ReadValue<float>();
        distance -= zoomInput * zoomSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, minZoom, maxZoom);

        // Update camera position
        Quaternion rotation = Quaternion.Euler(currentXAngle, currentYAngle, 0f);
        transform.position = target.position - rotation * Vector3.forward * distance;
        transform.LookAt(target.position);
    }
}
