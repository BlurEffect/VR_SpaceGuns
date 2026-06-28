using UnityEngine;

// Blends steering behaviors each frame using per-role weight profiles.
public class SteeringAgent : MonoBehaviour
{
    [Header("Profiles")]
    public ShipSteeringProfile       steeringProfile;
    [HideInInspector] public ShipSteeringBehaviorProfile behaviorProfile;

    [Header("Seek / Flee Targets")]
    public Transform seekTarget;
    public Rigidbody seekTargetRigidbody;   // optional — used by Pursue and AttackRun
    public Transform fleeTarget;
    public Rigidbody fleeTargetRigidbody;   // optional — used by Evade

    [Header("Orbit")]
    public float orbitRadius = 10f;         // set per engagement

    [Header("Containment")]
    public Transform containmentCenter;
    public float     containmentRadius        = 50f;
    public float     containmentLookAheadTime =  1.5f;

    [Header("Formation")]
    public Transform formationLeader;
    public Vector3   formationOffset;

    [Header("Group Behavior Inputs (set externally by a flock manager)")]
    public Vector3 groupCenter;
    public Vector3 groupDirection;
    public Vector3 neighborPosition;

    // Outputs read by FlightController
    public Vector3 desiredDirection { get; private set; }
    public float   desiredSpeed     { get; private set; }

    [HideInInspector] public Transform[] patrolWaypoints;

    private Vector3   _wanderTarget;
    private int       _currentWaypointIndex;
    private bool      _attackRunBreakOff;
    private Vector3   _breakOffDir;
    private Transform _prevSeekTarget;

    public void ComputeSteering(Vector3 currentForward, Vector3 currentVelocity, float maxSpeed)
    {
        Vector3 combined = Vector3.zero;

        Vector3 seekVel = seekTargetRigidbody != null ? seekTargetRigidbody.linearVelocity : Vector3.zero;
        Vector3 fleeVel = fleeTargetRigidbody != null ? fleeTargetRigidbody.linearVelocity : Vector3.zero;

        // --- Basic ---
        if (behaviorProfile.seekWeight > 0f && seekTarget != null)
            combined += SteeringModule.Seek(transform.position, seekTarget.position, maxSpeed)
                        * behaviorProfile.seekWeight;

        if (behaviorProfile.fleeWeight > 0f && fleeTarget != null)
            combined += SteeringModule.Flee(transform.position, fleeTarget.position, maxSpeed)
                        * behaviorProfile.fleeWeight;

        if (behaviorProfile.arriveWeight > 0f && seekTarget != null)
            combined += SteeringModule.Arrive(transform.position, seekTarget.position, maxSpeed,
                            steeringProfile.slowRadius, steeringProfile.arriveRadius)
                        * behaviorProfile.arriveWeight;

        // --- Wander ---
        if (behaviorProfile.wanderWeight > 0f)
            combined += SteeringModule.Wander(currentForward, transform.up, ref _wanderTarget,
                            steeringProfile.wanderJitter, steeringProfile.wanderRadius,
                            steeringProfile.wanderProjectDistance, maxSpeed)
                        * behaviorProfile.wanderWeight;

        // --- Avoidance ---
        if (behaviorProfile.avoidWeight > 0f)
            combined += SteeringModule.AvoidObstacles(transform,
                            steeringProfile.avoidDistance, steeringProfile.obstacleMask, steeringProfile.shipRadius)
                        * maxSpeed * behaviorProfile.avoidWeight;

        // --- Flocking ---
        if (behaviorProfile.cohesionWeight > 0f)
            combined += SteeringModule.Cohesion(transform.position, groupCenter, maxSpeed)
                        * behaviorProfile.cohesionWeight;

        if (behaviorProfile.separationWeight > 0f)
            combined += SteeringModule.Separation(transform.position, neighborPosition,
                            steeringProfile.separationDistance, maxSpeed)
                        * behaviorProfile.separationWeight;

        if (behaviorProfile.alignmentWeight > 0f)
            combined += SteeringModule.Alignment(groupDirection, maxSpeed)
                        * behaviorProfile.alignmentWeight;

        // --- Targeting ---
        if (behaviorProfile.orbitWeight > 0f && seekTarget != null)
            combined += SteeringModule.Orbit(transform.position, seekTarget.position, orbitRadius, maxSpeed)
                        * behaviorProfile.orbitWeight;

        if (behaviorProfile.pursueWeight > 0f && seekTarget != null)
            combined += SteeringModule.Pursue(transform.position, seekTarget.position, seekVel, maxSpeed)
                        * behaviorProfile.pursueWeight;

        if (behaviorProfile.evadeWeight > 0f && fleeTarget != null)
            combined += SteeringModule.Evade(transform.position, fleeTarget.position, fleeVel, maxSpeed)
                        * behaviorProfile.evadeWeight;

        if (behaviorProfile.attackRunWeight > 0f && seekTarget != null)
        {
            // Reset break-off state when target changes
            if (seekTarget != _prevSeekTarget)
            {
                _prevSeekTarget    = seekTarget;
                _attackRunBreakOff = false;
            }

            Vector3 toTarget = seekTarget.position - transform.position;
            float   dist     = toTarget.magnitude;
            float   dot      = Vector3.Dot(currentForward, toTarget.normalized);

            // Enter break-off (sticky): triggers when within breakOffRange or target passes behind.
            // Direction is locked at entry so it doesn't jitter as geometry changes.
            if (!_attackRunBreakOff && (dist <= behaviorProfile.breakOffRange || dot <= 0.2f))
            {
                _attackRunBreakOff = true;
                Vector3 breakRef = Mathf.Abs(dot) < 0.95f ? currentForward : Vector3.up;
                Vector3 perp     = Vector3.Cross(toTarget.normalized, breakRef).normalized;
                _breakOffDir     = (perp - toTarget.normalized * 0.5f).normalized;
            }
            // Exit break-off only once safely outside attack range for a clean re-approach
            else if (_attackRunBreakOff && dist > behaviorProfile.attackRange)
            {
                _attackRunBreakOff = false;
            }

            Vector3 attackRunVel = _attackRunBreakOff
                ? _breakOffDir * maxSpeed
                : SteeringModule.AttackRun(transform.position, currentForward,
                      seekTarget.position, seekVel, maxSpeed,
                      behaviorProfile.attackRange, behaviorProfile.breakOffRange);

            combined += attackRunVel * behaviorProfile.attackRunWeight;
        }

        // --- Navigation ---
        if (behaviorProfile.containmentWeight > 0f && containmentCenter != null)
            combined += SteeringModule.Containment(transform.position, currentVelocity,
                            containmentCenter.position, containmentRadius, containmentLookAheadTime, maxSpeed)
                        * behaviorProfile.containmentWeight;

        if (behaviorProfile.formationWeight > 0f && formationLeader != null)
            combined += SteeringModule.Formation(transform.position, formationLeader, formationOffset,
                            maxSpeed, steeringProfile.slowRadius, steeringProfile.arriveRadius)
                        * behaviorProfile.formationWeight;

        if (behaviorProfile.patrolWeight > 0f && patrolWaypoints != null && patrolWaypoints.Length > 0)
            combined += SteeringModule.Patrol(transform.position, patrolWaypoints,
                            ref _currentWaypointIndex, steeringProfile.waypointReachRadius, maxSpeed)
                        * behaviorProfile.patrolWeight;

        // --- Resolve ---
        if (combined.sqrMagnitude < 0.001f)
        {
            desiredDirection = currentForward;
            desiredSpeed     = 0f;
            return;
        }

        desiredDirection = SteeringModule.ClampDirection(currentForward, combined.normalized,
                               steeringProfile.maxSteeringAngle);
        desiredSpeed     = Mathf.Min(combined.magnitude, maxSpeed);
    }

    void Update() { }
}
