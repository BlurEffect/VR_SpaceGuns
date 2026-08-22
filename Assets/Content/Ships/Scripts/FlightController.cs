using UnityEngine;

public class FlightController : MonoBehaviour
{
    [Header("References")]
    public SteeringAgent   steering;
    public ShipFlightProfile flightProfile;

    private Vector3 _velocity;

    void Update()
    {
        if (steering == null || flightProfile == null) return;
        
        steering.ComputeSteering(transform.forward, _velocity, flightProfile.maxSpeed);

        UpdateVelocity();

        // Nose tracks the actual velocity direction — produces smooth arcs instead of the nose
        // snapping to a new heading while the path slowly catches up.
        Vector3 noseTarget = _velocity.sqrMagnitude > 0.01f
            ? _velocity.normalized
            : steering.desiredDirection;

        RotateTowards(noseTarget);
        ApplyBanking(noseTarget);
        Move();
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        float verticalness = Mathf.Abs(Vector3.Dot(targetDirection, Vector3.up));
        // Near-vertical: blend toward Vector3.forward as the up-hint instead of transform.up.
        // transform.up creates a self-referential feedback loop (the Slerp target depends on
        // the Slerp result), causing oscillation when the ship rises or dives steeply.
        Vector3 upRef = Vector3.Lerp(Vector3.up, Vector3.forward,
                            Mathf.Clamp01((verticalness - 0.7f) / 0.3f));
        Quaternion targetRot = Quaternion.LookRotation(targetDirection, upRef);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * flightProfile.turnRate);
    }

    private void ApplyBanking(Vector3 targetDirection)
    {
        // Suppress banking as the ship flies near-vertical. In that orientation InverseTransformDirection
        // produces large local-X values that cause the banking Slerp to fight RotateTowards every frame.
        float verticalness   = Mathf.Abs(Vector3.Dot(targetDirection, Vector3.up));
        float bankSuppression = 1f - Mathf.Clamp01((verticalness - 0.6f) / 0.4f);

        float bankingAngle = Mathf.Clamp(-transform.InverseTransformDirection(targetDirection).x, -1f, 1f)
                             * flightProfile.bankAmount * bankSuppression;
        Quaternion bankRotation = Quaternion.Euler(0f, 0f, bankingAngle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            transform.rotation * bankRotation,
            Time.deltaTime * flightProfile.bankSmooth);
    }

    private void UpdateVelocity()
    {
        float   currentSpeed = _velocity.magnitude;
        Vector3 currentDir   = currentSpeed > 0.001f ? _velocity / currentSpeed : transform.forward;

        float newSpeed = Mathf.MoveTowards(currentSpeed, steering.desiredSpeed,
                             flightProfile.acceleration * Time.deltaTime);

        // Angular rate = a/v  (centripetal formula): fast ships arc wide, slow ships turn tighter.
        float angularRate = currentSpeed > 0.1f
            ? flightProfile.acceleration / currentSpeed
            : flightProfile.acceleration;

        Vector3 newDir = Vector3.RotateTowards(currentDir, steering.desiredDirection,
                             angularRate * Time.deltaTime, 0f);

        _velocity = newDir * newSpeed;
    }

    private void ApplyDriftDamping()
    {
        float   forwardSpeed    = Vector3.Dot(_velocity, transform.forward);
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        Vector3 lateralVelocity = _velocity - forwardVelocity;
        _velocity = forwardVelocity
                    + lateralVelocity * (1f - flightProfile.driftDamping * Time.deltaTime)
                    * flightProfile.driftDampingFactor;
    }

    private void Move()
    {
        transform.position += _velocity * Time.deltaTime;
    }
}
