using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Barrel
{
    public Transform yawRotator; // optional independent per-barrel yaw pivot; null = no independent yaw
    public Transform muzzle;     // required fire origin/direction
}

public class Targeting : MonoBehaviour
{
    [Header("Turret Parts")]
    [SerializeField] private Transform rotatorYMain;   // Base - rotates horizontally (yaw)
    [SerializeField] private Transform rotatorX;       // Barrel - rotates vertically (pitch)

    [Header("Barrels")]
    [SerializeField] private Barrel[] barrels;
    public IReadOnlyList<Barrel> Barrels => barrels;

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
            if (barrels != null)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (Barrel b in barrels)
                {
                    if (b.yawRotator == null) continue;
                    sum += b.yawRotator.position;
                    count++;
                }
                if (count > 0) midRef = sum / count;
            }

            Vector3 localDirPitch = rotatorX.parent.InverseTransformDirection(predictedTargetPos - midRef);
            float   targetPitch   = Mathf.Clamp(-Mathf.Atan2(localDirPitch.y, localDirPitch.z) * Mathf.Rad2Deg, minPitch, maxPitch);

            rotatorX.localRotation = Quaternion.RotateTowards(
                rotatorX.localRotation, Quaternion.Euler(targetPitch, 0f, 0f), pitchSpeed * Time.deltaTime);
        }

        // --- STEP 4: Per-barrel yaw ---
        if (barrels != null)
        {
            foreach (Barrel b in barrels)
            {
                if (b.yawRotator == null) continue;

                Vector3 localDir = b.yawRotator.parent.InverseTransformDirection(predictedTargetPos - b.yawRotator.position);
                float   yaw      = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, minYaw, maxYaw);

                b.yawRotator.localRotation = Quaternion.RotateTowards(
                    b.yawRotator.localRotation, Quaternion.Euler(0f, yaw, 0f), barrelYawSpeed * Time.deltaTime);
            }
        }

        // --- Aim & line-of-fire check ---
        Vector3 posSum = Vector3.zero;
        Vector3 fwdSum = Vector3.zero;
        int     muzzleCount = 0;
        if (barrels != null)
        {
            foreach (Barrel b in barrels)
            {
                if (b.muzzle == null) continue;
                posSum += b.muzzle.position;
                fwdSum += b.muzzle.forward;
                muzzleCount++;
            }
        }

        if (muzzleCount > 0)
        {
            Vector3 muzzleMid     = posSum / muzzleCount;
            Vector3 muzzleForward = fwdSum.normalized;
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