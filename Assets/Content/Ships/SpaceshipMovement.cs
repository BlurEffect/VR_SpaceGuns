using UnityEngine;

public class SpaceshipMovement : MonoBehaviour
{
    [SerializeField] private Transform hunter;
    [SerializeField] private Transform target;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float mass = 10f;
    [SerializeField] private float maxSpeed = 10f;

    [SerializeField] private float brakingDistane = 10f;
    
    private Vector3 _desiredVelocity;
    private Vector3 _velocity;
    private Vector3 _steering;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _steering = Vector3.zero;
        
        _steering += Seek();
        _steering += Flee();
        
        // Limit steering force to max force
        _steering = Vector3.ClampMagnitude(_steering, maxForce);
        
        // Apply mass: F = ma, so a = F/m
        Vector3 acceleration = _steering / mass;
        
        // Update velocity and clamp to max speed
        _velocity = Vector3.ClampMagnitude(_velocity + acceleration * Time.deltaTime, maxSpeed);
        
        // Update position
        transform.position += _velocity * Time.deltaTime;
        
        // Rotate to face movement direction (only if moving)
        if (_velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity);
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    //------------------------------------------- Steering Behaviors ---------------------------------------------------
    //------------------------------------------------------------------------------------------------------------------

    private Vector3 Seek()
    {
        Vector3 seekSteering = Vector3.zero;

        if (target != null)
        {
            Vector3 vectorToTarget = target.position - transform.position;
            float distanceToTarget = vectorToTarget.magnitude;

            // If we're close enough, stop completely
            if (distanceToTarget < 1f)
            {
                // Brake to full stop
                seekSteering = -_velocity;
                return seekSteering;
            }
            
            
            // Arrival behavior, slow down when close to reaching the target
            Vector3 desiredVelocity = Vector3.zero;
            if (distanceToTarget < brakingDistane)
            {
                // Calculate desired velocity: direction to target at max speed
                desiredVelocity = vectorToTarget.normalized * maxSpeed * (distanceToTarget / brakingDistane);
            }
            else
            {
                // Calculate desired velocity: direction to target at max speed
                desiredVelocity = vectorToTarget.normalized * maxSpeed;
            }
        
            // Calculate steering force: difference between desired and current velocity
            seekSteering = desiredVelocity - _velocity;
        }
        
        return seekSteering;
    }
    
    private Vector3 Flee()
    {
        Vector3 fleeSteering = Vector3.zero;

        if (hunter != null)
        {
            // Calculate desired velocity: direction to target at max speed
            Vector3 desiredVelocity = (transform.position - hunter.position).normalized * maxSpeed;
        
            // Calculate steering force: difference between desired and current velocity
            fleeSteering = desiredVelocity - _velocity;
        }
        
        return fleeSteering;
    }
}
