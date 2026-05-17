using UnityEngine;
using UnityEngine.Serialization;

// Weighted blending of steering behaviors in order to determine a desired direction
public class SteeringAgent : MonoBehaviour
{
    [Header("Parameters")]

    
    
    
    public float wanderJitter = 0.5f;
    public float wanderRadius = 1.5f;
    public float maxSteeringAngle = 90f;
    public float orbitRadius = 10f;
    
    public ShipSteeringSettings steeringSettings;
    public ShipSteeringBehaviorProfile steeringBehaviorProfile;

    [Header("Targets")]
    public Transform seekTarget;
    public Transform fleeTarget;

    [Header("Other Settings")]
    public float avoidDistance = 15f;
    public LayerMask obstacleMask;

    // The final computed steering direction
    public Vector3 SteeringDirection { get; private set; }

    // Group behavior inputs
    public Vector3 groupCenter;
    public Vector3 groupDirection;
    public Vector3 neighborPosition;

    private Vector3 wanderTarget;
    
    // Outputs
    public Vector3 desiredDirection { get; private set; }
    public float desiredSpeed { get; private set; }
    
    public void ComputeSteering(Vector3 currentForward, Vector3 currentVelocity, float maxSpeed)
    {
        Vector3 combinedVelocity = Vector3.zero;

        // Seek
        if (steeringBehaviorProfile.seekWeight > 0f && seekTarget != null)
        {
            combinedVelocity += SteeringModule.Seek(transform.position, seekTarget.position, maxSpeed) * 
                                steeringBehaviorProfile.seekWeight;
        }
        
        // Flee
        if (steeringBehaviorProfile.fleeWeight > 0f && fleeTarget != null)
        {
            combinedVelocity += SteeringModule.Flee(transform.position, fleeTarget.position, maxSpeed) *
                                steeringBehaviorProfile.fleeWeight;
        }

        // Arrive
        if (steeringBehaviorProfile.arriveWeight > 0f && seekTarget != null)
        {
            combinedVelocity += SteeringModule.Arrive(transform.position, seekTarget.position, maxSpeed, steeringSettings.slowRadius, steeringSettings.arriveRadius) *
                                steeringBehaviorProfile.arriveWeight;
        }



        

        // --- WANDER ---
        if (steeringBehaviorProfile.wanderWeight > 0f)
        {
            Vector3 wanderVel = SteeringModule.Wander(currentForward, ref wanderTarget, wanderJitter, wanderRadius, maxSpeed * 0.4f);
            combinedVelocity += wanderVel * steeringBehaviorProfile.wanderWeight;
        }
        
        // ORBIT
        if (steeringBehaviorProfile.orbitWeight > 0f && seekTarget != null)
            combinedVelocity += SteeringModule.Orbit(transform.position, seekTarget.position, currentForward, orbitRadius, maxSpeed) * steeringBehaviorProfile.orbitWeight;

        // TODO: Current Velocity is wrong here, should be target velocity
        // PURSUE
        if (steeringBehaviorProfile.pursueWeight > 0f && seekTarget != null)
            combinedVelocity += SteeringModule.Pursue(transform.position, seekTarget.position, currentVelocity, maxSpeed) * steeringBehaviorProfile.pursueWeight;

        // EVADE
        if (steeringBehaviorProfile.evadeWeight > 0f && fleeTarget != null)
            combinedVelocity += SteeringModule.Evade(transform.position, fleeTarget.position, currentVelocity, maxSpeed) * steeringBehaviorProfile.evadeWeight;


        // If negligible, keep current forward
        if (combinedVelocity.sqrMagnitude < 0.001f)
        {
            desiredDirection = currentForward;
            desiredSpeed = 0f;
            return;
        }

        // --- Compute final desiredDirection and desiredSpeed ---
        Vector3 finalDir = combinedVelocity.normalized;
        finalDir = SteeringModule.ClampDirection(currentForward, finalDir, maxSteeringAngle);

        desiredDirection = finalDir;
        desiredSpeed = combinedVelocity.magnitude;
    }
    
    void Update()
    {
        /*
        Vector3 steering = Vector3.zero;

        // Blend all steering behaviors
        if (seekWeight > 0 && seekTarget)
            steering += SteeringModule.Seek(transform, seekTarget.position) * seekWeight;

        if (arriveWeight > 0 && seekTarget)
            steering += SteeringModule.Arrive(transform, seekTarget.position, arriveRadius) * arriveWeight;

        if (fleeWeight > 0 && fleeTarget)
            steering += SteeringModule.Flee(transform, fleeTarget.position) * fleeWeight;

        if (wanderWeight > 0)
            steering += SteeringModule.Wander(transform) * wanderWeight;

        if (avoidWeight > 0)
            steering += SteeringModule.AvoidObstacles(transform, avoidDistance, obstacleMask) * avoidWeight;

        if (cohesionWeight > 0)
            steering += SteeringModule.Cohesion(transform, groupCenter) * cohesionWeight;

        if (separationWeight > 0)
            steering += SteeringModule.Separation(transform, neighborPosition, 10f) * separationWeight;

        if (alignmentWeight > 0)
            steering += SteeringModule.Alignment(groupDirection) * alignmentWeight;

        // Final normalized steering direction
        SteeringDirection = steering.sqrMagnitude > 0.01f
            ? steering.normalized
            : transform.forward;  // no input = keep flying straight
            
            */
    }
}
