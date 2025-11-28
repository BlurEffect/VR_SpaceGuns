using UnityEngine;
using UnityEngine.Serialization;

public class FlightController : MonoBehaviour
{
    [Header("References")]
    
    public SteeringAgent steering;
    
    //[SerializeField] private Transform hunter;
    //[SerializeField] private Transform target;

    [Header("Speed")]
    
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 15;
    [SerializeField] private float brakingDistance = 10f;
   
    [Header("Turning")]
    // How fast the ship can rotate towards its desired target direction
    public float turnRate = 3f;          
    // Higher values will lead to decreased drift during turns
    [SerializeField][Range(1.0f, 5.0f)] private float driftDamping = 2f;
    // Set this to 0 to prevent sideways drift while the ship turns
    [SerializeField][Range(0.0f, 1.0f)] private float driftDampingFactor = 1f;

    [Header("Banking")]
    // Maximal banking angle during turns
    [SerializeField] private float bankAmount = 60f;
    // How fast the ship will bank 
    [SerializeField] private float bankSmooth = 3f;

    [Header("Lead Pursuit")] 
    public bool useLeadPursuit = false;
    public float predictionTime = 0.5f;  // How far ahead to lead the target
    
    private Vector3 _velocity;

    void Update()
    {
        // Obtain the desired steering direction from the steering agent
        Vector3 steeringDirection = steering ? steering.SteeringDirection : Vector3.forward;
        
        // Apply rotation based on accumulated steering direction
        RotateTowards(steeringDirection);
        
        // Apply banking based on steering direction
        ApplyBanking(steeringDirection);
        
        // Calculate velocity based acceleration, top speed etc. (independent of rotation)
        UpdateVelocity();
        
        // Damp sideways drift
        ApplyDriftDamping();
        
        // Actually move the ship
        Move();
        
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        // Desired facing direction
        Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);

        // Actual rotation (limited turning)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * turnRate
        );
    }

    private void ApplyBanking(Vector3 targetDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(targetDirection);
        float bankingAngle = Mathf.Clamp(-localDirection.x, -1f, 1f) * bankAmount;
        Quaternion bankRotation = Quaternion.Euler(0f, 0f, bankingAngle);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            transform.rotation * bankRotation,
            Time.deltaTime * bankSmooth
        );
    }

    private void UpdateVelocity()
    {
        _velocity += transform.forward * (acceleration * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
        
        /*
        // Determine target speed 
        // Related to seek behavior, if we're close to the target, slow down

        // If we have no target, we wander about with half speed
        float targetSpeed = maxSpeed * 0.5f;
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < brakingDistance)
            {
                targetSpeed = maxSpeed * (distance / brakingDistance);
            }
        }
        
        // Accelerate the spaceship until we hit target speed and clamp it there
        _velocity += transform.forward * (acceleration * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, targetSpeed);
        */
    }

    private void ApplyDriftDamping()
    {
        // Allow for some sideways drift but damp it
        
        // Break velocity into forward and sideways components
        float forwardSpeed = Vector3.Dot(_velocity, transform.forward);
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        Vector3 lateralVelocity = _velocity - forwardVelocity;

        // Dampen sideways drift
        _velocity = forwardVelocity + lateralVelocity * (1f - driftDamping * Time.deltaTime) * driftDampingFactor;
    }
    
    private void Move()
    {
        // Actually move the ship transform
        transform.position += _velocity * Time.deltaTime;

    }
}