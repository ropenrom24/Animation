using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 20f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    private Vector2 lastPanPosition;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("SimpleCameraController requires a Camera component on the same GameObject.", this);
            enabled = false;
        }

        // Force orthographic mode for 2D
        cam.orthographic = true;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMousePanning();
        HandleMouseZooming();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchControls();
#endif
    }

    // === DESKTOP CONTROLS ===
    void HandleMousePanning()
    {
        if (Input.GetMouseButtonDown(2))
            lastPanPosition = Input.mousePosition;

        if (Input.GetMouseButton(2))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastPanPosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * (panSpeed * Time.deltaTime * cam.orthographicSize / 100f);
            transform.Translate(move, Space.Self);
            lastPanPosition = Input.mousePosition;
        }
    }

    void HandleMouseZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // === MOBILE CONTROLS ===
    void HandleTouchControls()
    {
        if (Input.touchCount == 1)
        {
            // One finger: pan
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                lastPanPosition = touch.position;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastPanPosition;
                Vector3 move = new Vector3(-delta.x, -delta.y, 0) * (panSpeed * Time.deltaTime * cam.orthographicSize / 100f);
                transform.Translate(move, Space.Self);
                lastPanPosition = touch.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            // Two fingers: pinch to zoom
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevDist = Vector2.Distance(t0Prev, t1Prev);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist;

            cam.orthographicSize -= delta * zoomSpeed * Time.deltaTime * 0.1f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}
