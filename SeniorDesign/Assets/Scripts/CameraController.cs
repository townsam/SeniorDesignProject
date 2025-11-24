using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float rotateSpeed = 100f;
    public float panSpeed = 1f;
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

    void LateUpdate()
    {
        Vector2 mouseDelta = mouseDeltaAction.ReadValue<Vector2>();

        // Rotate
        if (rightMouseAction.ReadValue<float>() > 0.5f)
        {
            currentYAngle += mouseDelta.x * rotateSpeed * Time.deltaTime;
            currentXAngle -= mouseDelta.y * rotateSpeed * Time.deltaTime;
            currentXAngle = Mathf.Clamp(currentXAngle, 20f, 80f);
        }

        // Pan (aligned with camera)
        if (middleMouseAction.ReadValue<float>() > 0.5f)
        {
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up); // Horizontal forward
            Vector3 pan = (right * -mouseDelta.x + forward * -mouseDelta.y) * panSpeed * Time.deltaTime;
            target.position += pan;
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
