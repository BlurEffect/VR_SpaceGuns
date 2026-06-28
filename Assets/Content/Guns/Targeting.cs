using UnityEngine;

public class Targeting : MonoBehaviour
{
    [Header("Turret Parts")]
    [SerializeField] private Transform rotatorYMain;   // Base - rotates horizontally (yaw)
    [SerializeField] private Transform rotatorX;       // Barrel - rotates vertically (pitch)
    [SerializeField] private Transform rotatorBarrelLeft;       // Barrel - rotates vertically (pitch)
    [SerializeField] private Transform rotatorBarrelRight;       // Barrel - rotates vertically (pitch)

    [SerializeField] private Transform muzzleLeft;       // Barrel - rotates vertically (pitch)
    [SerializeField] private Transform muzzleRight;       // Barrel - rotates vertically (pitch)

    [Header("Target Settings")]
    public Transform target;
    public Rigidbody targetRigidbody; // optional (for predictive aiming)

    [Header("Aiming Settings")]
    [SerializeField] private float yawSpeed = 90f;     // degrees per second
    [SerializeField] private float barrelYawSpeed = 90f;     // degrees per second
    [SerializeField] private float pitchSpeed = 60f;   // degrees per second
    [SerializeField] private float projectileSpeed = 50f; // m/s (used for prediction)

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -10f;    // down
    [SerializeField] private float maxPitch = 45f;     // up

    [Header("Barrel Yaw Limits")]
    [SerializeField] private float minYaw = -45f;    // down
    [SerializeField] private float maxYaw = 45f;     // up

    [Header("Aim & Fire Check")]
    [SerializeField] private float     aimThresholdDegrees = 5f;
    [SerializeField] private LayerMask lineOfFireBlockers;

    public bool IsAimed     { get; private set; }
    public bool ReadyToFire { get; private set; }

    private Vector3 predictedTargetPos;

    void Update()
    {
        if (!target) return;

        // --- STEP 1: Predict target position ---
        if (targetRigidbody)
        {
            Vector3 toTarget = target.position - rotatorX.position;
            float timeToTarget = toTarget.magnitude / Mathf.Max(projectileSpeed, 0.01f);
            predictedTargetPos = target.position + targetRigidbody.linearVelocity * timeToTarget;
        }
        else
        {
            predictedTargetPos = target.position;
        }

        // --- STEP 2: Yaw --- project onto parent’s local horizontal plane, not world XZ
        if (rotatorYMain != null)
        {
            Vector3 parentUp = rotatorYMain.parent != null ? rotatorYMain.parent.up : Vector3.up;
            Vector3 flatDir  = Vector3.ProjectOnPlane(predictedTargetPos - rotatorYMain.position, parentUp);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion desiredYaw = Quaternion.LookRotation(flatDir, parentUp);
                rotatorYMain.rotation = Quaternion.RotateTowards(
                    rotatorYMain.rotation, desiredYaw, yawSpeed * Time.deltaTime);
            }
        }

        // --- STEP 3: Pitch ---
        if (rotatorX != null)
        {
            Vector3 midRef = rotatorX.position;
            if (rotatorBarrelLeft != null && rotatorBarrelRight != null)
                midRef = (rotatorBarrelLeft.position + rotatorBarrelRight.position) * 0.5f;

            Vector3 localDirPitch = rotatorX.parent.InverseTransformDirection(predictedTargetPos - midRef);
            float   targetPitch   = Mathf.Clamp(-Mathf.Atan2(localDirPitch.y, localDirPitch.z) * Mathf.Rad2Deg, minPitch, maxPitch);

            rotatorX.localRotation = Quaternion.RotateTowards(
                rotatorX.localRotation, Quaternion.Euler(targetPitch, 0f, 0f), pitchSpeed * Time.deltaTime);
        }

        // --- STEP 4: Per-barrel yaw ---
        if (rotatorBarrelLeft != null && rotatorBarrelRight != null)
        {
            Vector3 localDirLeft  = rotatorBarrelLeft.parent.InverseTransformDirection(predictedTargetPos - rotatorBarrelLeft.position);
            Vector3 localDirRight = rotatorBarrelRight.parent.InverseTransformDirection(predictedTargetPos - rotatorBarrelRight.position);

            float yawLeft  = Mathf.Clamp(Mathf.Atan2(localDirLeft.x,  localDirLeft.z)  * Mathf.Rad2Deg, minYaw, maxYaw);
            float yawRight = Mathf.Clamp(Mathf.Atan2(localDirRight.x, localDirRight.z) * Mathf.Rad2Deg, minYaw, maxYaw);

            rotatorBarrelLeft.localRotation  = Quaternion.RotateTowards(
                rotatorBarrelLeft.localRotation,  Quaternion.Euler(0f, yawLeft,  0f), barrelYawSpeed * Time.deltaTime);
            rotatorBarrelRight.localRotation = Quaternion.RotateTowards(
                rotatorBarrelRight.localRotation, Quaternion.Euler(0f, yawRight, 0f), barrelYawSpeed * Time.deltaTime);
        }

        // --- Aim & line-of-fire check ---
        if (muzzleLeft != null && muzzleRight != null)
        {
            Vector3 muzzleMid     = (muzzleLeft.position + muzzleRight.position) * 0.5f;
            Vector3 muzzleForward = (muzzleLeft.forward  + muzzleRight.forward).normalized;
            Vector3 toTarget      = predictedTargetPos - muzzleMid;

            IsAimed     = Vector3.Angle(muzzleForward, toTarget.normalized) <= aimThresholdDegrees;
            ReadyToFire = IsAimed && !Physics.Raycast(muzzleMid, muzzleForward, toTarget.magnitude, lineOfFireBlockers);
        }
        else
        {
            IsAimed     = false;
            ReadyToFire = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Optional: visualize predicted point
        if (Application.isPlaying && target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(predictedTargetPos, 0.25f);
        }
    }
}