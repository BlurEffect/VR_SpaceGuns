using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Drives a player-controlled turret directly from input — no Targeting.cs AI involved.
// Wire the same rotator Transforms used by Targeting.cs; disable/remove Targeting.cs on
// this turret. Keep the turret out of ShipAI.primaryMounts so AI doesn't overwrite Shooting.
public class PlayerTurretController : MonoBehaviour
{
    [Header("Turret Parts")]
    [SerializeField] private Transform rotatorYMain;
    [SerializeField] private Transform rotatorX;
    [SerializeField] private Transform rotatorBarrelLeft;
    [SerializeField] private Transform rotatorBarrelRight;
    [SerializeField] private Transform muzzleLeft;
    [SerializeField] private Transform muzzleRight;

    [Header("Shooting")]
    [SerializeField] private Shooting shooting;

    [Header("Look Sensitivity")]
    [SerializeField] private float mouseSensitivity       = 0.15f;
    [SerializeField] private float stickSensitivity       = 90f;
    [SerializeField] private float stickDeadzone          = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float zoomSensitivityMult = 0.35f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch =  45f;

    [Header("Zoom")]
    [SerializeField] private Camera turretCamera;
    [SerializeField] private float defaultFov    = 60f;
    [SerializeField] private float zoomedFov     = 30f;
    [SerializeField] private float fovLerpSpeed  =  8f;

    [Header("Line of Fire")]
    [SerializeField] private LayerMask lineOfFireBlockers;
    [SerializeField] private float     lineOfFireCheckDistance = 50f;

    [Header("Action Buttons")]
    public UnityEvent onAction1;
    public UnityEvent onAction2;
    public UnityEvent onAction3;
    public UnityEvent onAction4;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    // Cached actions
    private InputAction _look;
    private InputAction _fire;
    private InputAction _zoom;
    private InputAction _act1, _act2, _act3, _act4;

    // Runtime state
    private float _currentPitch;
    private bool  _fireHeld;
    private bool  _isZooming;

    void Awake()
    {
        var map = inputActions.FindActionMap("Turret", throwIfNotFound: true);
        _look = map["TurretLook"];
        _fire = map["Fire"];
        _zoom = map["Zoom"];
        _act1 = map["Action1"];
        _act2 = map["Action2"];
        _act3 = map["Action3"];
        _act4 = map["Action4"];
    }

    void OnEnable()
    {
        inputActions.FindActionMap("Turret").Enable();

        _fire.performed += OnFirePerformed;
        _fire.canceled  += OnFireCanceled;
        _zoom.performed += OnZoomPerformed;
        _zoom.canceled  += OnZoomCanceled;
        _act1.performed += OnAction1;
        _act2.performed += OnAction2;
        _act3.performed += OnAction3;
        _act4.performed += OnAction4;
    }

    void OnDisable()
    {
        _fire.performed -= OnFirePerformed;
        _fire.canceled  -= OnFireCanceled;
        _zoom.performed -= OnZoomPerformed;
        _zoom.canceled  -= OnZoomCanceled;
        _act1.performed -= OnAction1;
        _act2.performed -= OnAction2;
        _act3.performed -= OnAction3;
        _act4.performed -= OnAction4;

        inputActions.FindActionMap("Turret").Disable();

        if (shooting != null) shooting.enabled = false;
    }

    private void OnFirePerformed(InputAction.CallbackContext _) => _fireHeld = true;
    private void OnFireCanceled (InputAction.CallbackContext _) => _fireHeld = false;
    private void OnZoomPerformed(InputAction.CallbackContext _) => _isZooming = true;
    private void OnZoomCanceled (InputAction.CallbackContext _) => _isZooming = false;
    private void OnAction1(InputAction.CallbackContext _) => onAction1.Invoke();
    private void OnAction2(InputAction.CallbackContext _) => onAction2.Invoke();
    private void OnAction3(InputAction.CallbackContext _) => onAction3.Invoke();
    private void OnAction4(InputAction.CallbackContext _) => onAction4.Invoke();

    void Update()
    {
        Vector2 look = _look.ReadValue<Vector2>();

        // Mouse delivers pixel-space deltas (magnitude >> 1); stick stays in [-1, 1].
        bool isMouseInput = look.sqrMagnitude > 4f;

        float sensitivityScale = _isZooming ? zoomSensitivityMult : 1f;

        float yawDelta, pitchDelta;
        if (isMouseInput)
        {
            yawDelta   =  look.x * mouseSensitivity * sensitivityScale;
            pitchDelta = -look.y * mouseSensitivity * sensitivityScale;
        }
        else
        {
            if (look.magnitude < stickDeadzone) look = Vector2.zero;
            yawDelta   =  look.x * stickSensitivity * sensitivityScale * Time.deltaTime;
            pitchDelta = -look.y * stickSensitivity * sensitivityScale * Time.deltaTime;
        }

        ApplyYaw(yawDelta);
        ApplyPitch(pitchDelta);
        UpdateZoomFov();
        UpdateFiring();
    }

    private void ApplyYaw(float degrees)
    {
        if (rotatorYMain == null || Mathf.Approximately(degrees, 0f)) return;
        Vector3 axis = rotatorYMain.parent != null ? rotatorYMain.parent.up : Vector3.up;
        rotatorYMain.Rotate(axis, degrees, Space.World);
    }

    private void ApplyPitch(float degrees)
    {
        if (rotatorX == null) return;
        _currentPitch = Mathf.Clamp(_currentPitch + degrees, minPitch, maxPitch);
        rotatorX.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
    }

    private void UpdateZoomFov()
    {
        if (turretCamera == null) return;
        float target = _isZooming ? zoomedFov : defaultFov;
        turretCamera.fieldOfView = Mathf.Lerp(
            turretCamera.fieldOfView, target, fovLerpSpeed * Time.deltaTime);
    }

    private void UpdateFiring()
    {
        if (shooting == null) return;

        if (!_fireHeld || muzzleLeft == null || muzzleRight == null)
        {
            shooting.enabled = false;
            return;
        }

        Vector3 mid = (muzzleLeft.position + muzzleRight.position) * 0.5f;
        Vector3 fwd = (muzzleLeft.forward  + muzzleRight.forward).normalized;
        bool lineOfFireClear = !Physics.Raycast(mid, fwd, lineOfFireCheckDistance, lineOfFireBlockers);
        shooting.enabled = lineOfFireClear;
    }
}
