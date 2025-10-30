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
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody targetRigidbody; // optional (for predictive aiming)

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

    
    private Vector3 predictedTargetPos;

    void Update()
    {
        
        
        if (!target) return;
        
        //TurretVFXManager.Instance.SpawnTracer(muzzleRight.position, target.position, projectileSpeed);

        

        // --- STEP 1: Predict target position if moving ---
        Vector3 targetPos = target.position;
        if (targetRigidbody)
        {
            Vector3 toTarget = target.position - rotatorX.position;
            float timeToTarget = toTarget.magnitude / Mathf.Max(projectileSpeed, 0.01f);
            predictedTargetPos = target.position + targetRigidbody.linearVelocity * timeToTarget;
        }
        else
        {
            predictedTargetPos = targetPos;
        }

        if (rotatorYMain != null)
        {
            // --- STEP 2: Handle yaw (horizontal rotation) ---
            Vector3 flatDir = predictedTargetPos - rotatorYMain.position;
            flatDir.y = 0f; // keep only horizontal component
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion desiredYaw = Quaternion.LookRotation(flatDir, Vector3.up);
                rotatorYMain.rotation = Quaternion.RotateTowards(
                    rotatorYMain.rotation, desiredYaw, yawSpeed * Time.deltaTime);
            }
        }

        /* Original pitch implementation with rotator directly aiming at target, however we need to pitch in a way so that the barrels align properly
        // --- STEP 3: Handle pitch (vertical rotation) ---
        // predictedTargetPos - a world-space direction from the pitch joint to the target.
        //Since the rotatorX only rotates on its local X-axis, we need to know where the target is in that local coordinate frame:
        // This says: “what is the direction to the target, relative to my parent’s orientation?”
        Vector3 localTargetDir = rotatorX.parent.InverseTransformPoint(predictedTargetPos);
       // We only care about pitching around X, so we look at the vertical vs forward components.
        //    We can use Mathf.Atan2 to find the pitch angle:
        //Atan2(y, z) gives you the vertical angle (up/down) between forward and the target direction.
        //    Multiplying by Rad2Deg converts radians to degrees.
      //  Mathf.Atan2(y, x) returns the angle (in radians) between the positive X-axis and the vector (x, y) on a 2D plane.
        float targetPitch = -Mathf.Atan2(localTargetDir.y, localTargetDir.z) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        Quaternion desiredPitch = Quaternion.Euler(targetPitch, 0f, 0f);
        rotatorX.localRotation = Quaternion.RotateTowards(
            rotatorX.localRotation, desiredPitch, pitchSpeed * Time.deltaTime);
        */

        if (rotatorX != null)
        {

            Vector3 barrelsMid = (rotatorBarrelLeft.position + rotatorBarrelRight.position) * 0.5f;
            Vector3 toTarget2 = target.position - barrelsMid;
            /*We can compute the pitch angle θ by looking at the direction from the barrel position to the target (like before),
    then project that direction into the local space of the rotatorX, and extract the vertical angle.*/
            Vector3 localDirPitch = rotatorX.parent.InverseTransformDirection(toTarget2);
            float targetPitch = -Mathf.Atan2(localDirPitch.y, localDirPitch.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            Quaternion desiredPitch = Quaternion.Euler(targetPitch, 0f, 0f);
            rotatorX.localRotation = Quaternion.RotateTowards(
                rotatorX.localRotation, desiredPitch, pitchSpeed * Time.deltaTime);
        }

        Vector3 dirLeft = target.position - rotatorBarrelLeft.position;
        Vector3 dirRight = target.position - rotatorBarrelRight.position;
        // Convert that direction into the local space of each barrel’s parent
        Vector3 localDirLeft = rotatorBarrelLeft.parent.InverseTransformDirection(dirLeft);
        Vector3 localDirRight = rotatorBarrelRight.parent.InverseTransformDirection(dirRight);
        
        float yawLeft = Mathf.Atan2(localDirLeft.x, localDirLeft.z) * Mathf.Rad2Deg;
        float yawRight = Mathf.Atan2(localDirRight.x, localDirRight.z) * Mathf.Rad2Deg;
        
        yawLeft = Mathf.Clamp(yawLeft, minPitch, maxPitch);
        yawRight = Mathf.Clamp(yawRight, minPitch, maxPitch);
        
        Quaternion desiredYawLeft = Quaternion.Euler(0f, yawLeft, 0f);
        Quaternion desiredYawRight = Quaternion.Euler(0f, yawRight, 0f);

        rotatorBarrelLeft.localRotation = Quaternion.RotateTowards(
            rotatorBarrelLeft.localRotation, desiredYawLeft, barrelYawSpeed * Time.deltaTime);
        
        rotatorBarrelRight.localRotation = Quaternion.RotateTowards(
            rotatorBarrelRight.localRotation, desiredYawRight, barrelYawSpeed * Time.deltaTime);
        
        //rotatorBarrelLeft.localRotation = Quaternion.Euler(0f, yawLeft, 0f);
        //rotatorBarrelRight.localRotation = Quaternion.Euler(0f, yawRight, 0f);
        
        
        
        
        //rotatorBarrelRight.localRotation = Quaternion.RotateTowards(
        //   rotatorBarrelRight.localRotation, Quaternion.Inverse(desiredBarrelYaw), yawSpeed * Time.deltaTime);
        
        //rotatorBarrelLeft.localRotation = Quaternion.Inverse(rotatorYMain.localRotation);
        //rotatorBarrelRight.localRotation = rotatorYMain.localRotation;
        
        
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