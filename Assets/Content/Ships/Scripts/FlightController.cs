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

        RotateTowards(steering.desiredDirection);
        ApplyBanking(steering.desiredDirection);

        Vector3 desiredVelocity = steering.desiredDirection * steering.desiredSpeed;
        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity, flightProfile.acceleration * Time.deltaTime);

        ApplyDriftDamping();
        Move();
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        // Blend between world up (keeps ship level) and transform.up (avoids gimbal lock near vertical).
        float verticalness = Mathf.Abs(Vector3.Dot(targetDirection, Vector3.up));
        Vector3 upRef = Vector3.Lerp(Vector3.up, transform.up, verticalness);
        Quaternion targetRot = Quaternion.LookRotation(targetDirection, upRef);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * flightProfile.turnRate);
    }

    private void ApplyBanking(Vector3 targetDirection)
    {
        float bankingAngle = Mathf.Clamp(-transform.InverseTransformDirection(targetDirection).x, -1f, 1f)
                             * flightProfile.bankAmount;
        Quaternion bankRotation = Quaternion.Euler(0f, 0f, bankingAngle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            transform.rotation * bankRotation,
            Time.deltaTime * flightProfile.bankSmooth);
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
