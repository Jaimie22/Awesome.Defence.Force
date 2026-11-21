using UnityEngine;


public class IsoCamera : MonoBehaviour
{

    [Header("Parts")]

    // The camera (a child of this rig). Found automatically if left empty.
    public Camera cam;

    [Header("Angle")]

    // How steeply the camera looks down. This never changes during play.
    [Range(10f, 89f)] public float pitch = 45f;

    // Which way the camera faces around the board when the game starts.
    // 45 gives the diagonal, isometric-style view.
    public float startYaw = 45f;

    // Ticked = orthographic (flat, no perspective, classic iso look).
    // Unticked = perspective.
    public bool orthographic = true;

    [Header("Framing")]

    // Orthographic: how much of the board fits on screen (bigger = see more).
    // Perspective: how far the camera is from the rig (bigger = see more).
    [Min(0.1f)] public float viewSize = 12f;

    // In orthographic mode, how far back the camera sits. It just needs to be
    // big enough that nothing gets cut off; it doesn't change the framing.
    [Min(1f)] public float orthoDistance = 40f;

    [Header("Orbit: Q / E Steps")]

    // Tick to let Q and E rotate the view.
    public bool allowStepRotate = true;

    // How far each Q or E press turns the view (degrees). 90 or 45 work well.
    [Range(5f, 180f)] public float rotateStep = 90f;

    [Header("Orbit: Right Mouse Drag")]

    // Tick to let the player hold the right mouse button and drag to orbit.
    public bool allowDragOrbit = true;

    // How fast dragging turns the view. Higher = faster.
    [Min(0.01f)] public float dragSensitivity = 3f;

    [Header("Smoothing")]

    // How smoothly the view swings round (higher = snappier).
    [Min(0.1f)] public float rotateSmoothing = 8f;


    // "target" is where we want to face, "current" is where we face right now.
    // Easing current towards target each frame makes the turning smooth.
    float targetYaw, currentYaw;


    void Start()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();

        targetYaw = currentYaw = startYaw;
        Apply();
    }


    void Update()
    {
        // --- Q / E: turn in steps ---
        if (allowStepRotate)
        {
            if (Input.GetKeyDown(KeyCode.Q)) targetYaw += rotateStep;
            if (Input.GetKeyDown(KeyCode.E)) targetYaw -= rotateStep;
        }

        // --- Right mouse drag: orbit freely ---
        // (Mouse button 1 = the right button.)
        if (allowDragOrbit && Input.GetMouseButton(1))
        {
            // How far the mouse moved sideways this frame.
            float dragX = Input.GetAxis("Mouse X");
            targetYaw += dragX * dragSensitivity;
        }

        // Ease towards the target direction for a smooth swing.
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotateSmoothing * Time.deltaTime);

        Apply();
    }


    void Apply()
    {
        if (cam == null) return;

        // Spin the rig to face the current direction.
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Tilt the camera down by the (locked) pitch angle.
        Quaternion tilt = Quaternion.Euler(pitch, 0f, 0f);
        cam.orthographic = orthographic;

        if (orthographic)
        {
            // Orthographic: the view size sets how much you can see.
            cam.orthographicSize = viewSize;
            cam.transform.localPosition = tilt * new Vector3(0f, 0f, -orthoDistance);
        }
        else
        {
            // Perspective: how far back the camera sits sets how much you see.
            cam.transform.localPosition = tilt * new Vector3(0f, 0f, -viewSize);
        }

        cam.transform.localRotation = tilt;
    }


    void OnValidate()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (Application.isPlaying) return;

        targetYaw = currentYaw = startYaw;
        Apply();
    }
}