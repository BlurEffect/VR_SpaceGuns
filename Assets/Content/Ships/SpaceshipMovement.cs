using UnityEngine;
using UnityEngine.Serialization;

public class SpaceshipMovement : MonoBehaviour
{
    [Header("References")]
    
    
    [SerializeField] private Transform hunter;
    [SerializeField] private Transform target;

    [Header("Flight Settings")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 15;
    
    [SerializeField] private float brakingDistance = 10f;
    
    // Higher values will lead to decreased drift during turns
    [SerializeField][Range(1.0f, 5.0f)] private float driftDamping = 2f;
    // Set this to 0 to prevent sideways drift while the ship turns
    [SerializeField][Range(0.0f, 1.0f)] private float driftDampingFactor = 1f;

    [Header("Banking")]
    [SerializeField] private float rollAmount = 45f;
    [SerializeField] private float rotationSmooth = 2f;

    
    private Vector3 _velocity;

    void Update()
    {
        Vector3 seekDirection = Seek();
        Vector3 avoidDirection = Avoid();
        Vector3 wanderDirection = Wander();
        
        // Add the different steering directions and normalize the result to obtain the overall desired direction
        // Option to add weights later on
        Vector3 steeringDirection = (seekDirection * 1.0f + 
                                     avoidDirection * 1.0f + 
                                     wanderDirection * 1.0f).normalized;
        
        // Apply rotation based on accumulated steering direction
        RotateTowards(steeringDirection);

        // Independent of rotation, propel the ship forward 
        MoveForward();
        
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        // Desired facing direction
        Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);

        // Actual rotation (limited turning)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSmooth
        );
    }

    // Returns direction towards target position
    private Vector3 Seek()
    {
        Vector3 seekDirection = Vector3.zero;
        if (target != null)
        {
            seekDirection = (target.position - transform.position).normalized;
        }
        return seekDirection;
    }

    private Vector3 Avoid()
    {
        return Vector3.zero;
    }

    private Vector3 Wander()
    {
        return Vector3.zero;
    }

    private void MoveForward()
    {
        // Determine target speed 
        // Related to seek behavior, if we're close to the target, slow down

        float targetSpeed = maxSpeed;
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < brakingDistance)
            {
                targetSpeed = maxSpeed * (distance / brakingDistance);
            }
        }
        
        // Accelerate the spaceship until we hit max speed and clamp it there
        _velocity += transform.forward * (acceleration * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, targetSpeed);

        // Allow for some sideways drift but damp it
        
        // Break velocity into forward and sideways components
        float forwardSpeed = Vector3.Dot(_velocity, transform.forward);
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        Vector3 lateralVelocity = _velocity - forwardVelocity;

        // Dampen sideways drift
        _velocity = forwardVelocity + lateralVelocity * (1f - driftDamping * Time.deltaTime) * driftDampingFactor;
        
        // Actually move the ship transform
        transform.position += _velocity * Time.deltaTime;
    }
}