using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 20f;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 100f; // Adjust as needed

    private Vector3 lastPanPosition;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("SimpleCameraController requires a Camera component on the same GameObject.", this);
            this.enabled = false;
        }
    }

    void Update()
    {
        HandlePanning();
        HandleZooming();
    }

    void HandlePanning()
    {
        // Check if Middle Mouse Button is pressed down
        if (Input.GetMouseButtonDown(2)) // 2 is the middle mouse button
        {
            lastPanPosition = Input.mousePosition;
        }

        // Check if Middle Mouse Button is held down
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastPanPosition;
            
            // Adjust sensitivity - may need tweaking depending on screen resolution/camera settings
            float adjustedPanSpeed = panSpeed * Time.deltaTime;
            if (cam.orthographic) 
            {
                // Adjust speed based on orthographic size for consistent feel
                adjustedPanSpeed *= cam.orthographicSize / 10f; // Example adjustment factor
            }

            // Calculate movement vector relative to camera orientation
            Vector3 move = new Vector3(-delta.x * adjustedPanSpeed, -delta.y * adjustedPanSpeed, 0);
            transform.Translate(move, Space.Self); // Move relative to camera's local axes

            // Update last position for next frame's delta calculation
            lastPanPosition = Input.mousePosition;
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            if (cam.orthographic)
            {
                // Adjust orthographic size
                cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime * 50f; // Adjust multiplier as needed
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoomDistance, maxZoomDistance);
            }
            else
            {
                // Move perspective camera forward/backward
                 // Calculate move amount, potentially faster when further away
                float moveAmount = scroll * zoomSpeed * Time.deltaTime * 100f; // Adjust multiplier
                Vector3 move = transform.forward * moveAmount;
                Vector3 newPos = transform.position + move;

                // Basic distance clamping (optional, adjust based on need)
                // float currentDistance = Vector3.Distance(newPos, Vector3.zero); // Assuming origin focus
                // if (currentDistance >= minZoomDistance && currentDistance <= maxZoomDistance)
                // {
                      transform.Translate(move, Space.World);
                // }
            }
        }
    }
} 