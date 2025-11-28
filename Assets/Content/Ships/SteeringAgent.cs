using UnityEngine;

// Weighted blending of steering behaviors in order to determine a desired direction
public class SteeringAgent : MonoBehaviour
{
    [Header("Weights")]
    public float seekWeight = 0f;
    public float arriveWeight = 0f;
    public float fleeWeight = 0f;
    public float wanderWeight = 0f;
    public float avoidWeight = 0f;
    public float cohesionWeight = 0f;
    public float separationWeight = 0f;
    public float alignmentWeight = 0f;

    [Header("Targets")]
    public Transform seekTarget;
    public Transform fleeTarget;

    [Header("Other Settings")]
    public float arriveRadius = 15f;
    public float avoidDistance = 15f;
    public LayerMask obstacleMask;

    // The final computed steering direction
    public Vector3 SteeringDirection { get; private set; }

    // Group behavior inputs
    public Vector3 groupCenter;
    public Vector3 groupDirection;
    public Vector3 neighborPosition;

    void Update()
    {
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
    }
}
