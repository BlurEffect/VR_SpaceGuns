using UnityEngine;

// All functions return velocity vectors (direction * speed) so they can be blended directly.
public static class SteeringModule
{
    // ---------------------------------------------
    // Seek — fly directly towards a target
    // ---------------------------------------------
    public static Vector3 Seek(Vector3 position, Vector3 targetPos, float maxSpeed)
    {
        return (targetPos - position).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Flee — fly directly away from a target
    // ---------------------------------------------
    public static Vector3 Flee(Vector3 position, Vector3 targetPos, float maxSpeed)
    {
        return (position - targetPos).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Arrive — seek but decelerate to a stop at target
    // ---------------------------------------------
    public static Vector3 Arrive(Vector3 position, Vector3 targetPos, float maxSpeed, float slowRadius, float arriveRadius)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;

        if (distance < arriveRadius)
            return Vector3.zero;

        return toTarget.normalized * maxSpeed * Mathf.Clamp01(distance / slowRadius);
    }

    // ---------------------------------------------
    // Wander — smooth random wandering in agent-local space
    // ---------------------------------------------
    // wanderTarget is an offset on a unit sphere kept in local space; pass transform.up as the up vector.
    public static Vector3 Wander(Vector3 forward, Vector3 up, ref Vector3 wanderTarget,
                                  float jitter, float radius, float projectDistance, float maxSpeed)
    {
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter
        );
        wanderTarget = wanderTarget.normalized * radius;

        // Project sphere ahead in agent-local space, then rotate into world space.
        // Using LookRotation so the sphere always sits in front of wherever the ship is facing.
        Quaternion agentRot = Quaternion.LookRotation(forward, up);
        Vector3 localTarget = Vector3.forward * projectDistance + wanderTarget;
        return (agentRot * localTarget).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // AvoidObstacles — fan of 5 feeler SphereCasts
    // ---------------------------------------------
    public static Vector3 AvoidObstacles(Transform self, float avoidDistance, LayerMask mask, float shipRadius)
    {
        // Angles and weights: straight ahead most urgent, outer rays less so
        float[] angles  = {  0f, 20f, -20f, 45f, -45f };
        float[] weights = { 1f, 0.75f, 0.75f, 0.5f, 0.5f };

        Vector3 steer = Vector3.zero;

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(angles[i], self.up) * self.forward;
            float dist = avoidDistance * (i == 0 ? 1f : 0.7f);

            if (Physics.SphereCast(self.position, shipRadius, dir, out RaycastHit hit, dist, mask))
            {
                float urgency = 1f - (hit.distance / dist);
                steer += Vector3.Reflect(dir, hit.normal).normalized * weights[i] * urgency;
            }
        }

        return steer.sqrMagnitude > 0f ? steer.normalized : Vector3.zero;
    }

    // ---------------------------------------------
    // Cohesion — steer toward the center of a group
    // ---------------------------------------------
    public static Vector3 Cohesion(Vector3 position, Vector3 groupCenter, float maxSpeed)
    {
        return (groupCenter - position).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Separation — steer away from a single neighbor
    // ---------------------------------------------
    public static Vector3 Separation(Vector3 position, Vector3 neighborPos, float desiredSeparation, float maxSpeed)
    {
        Vector3 diff = position - neighborPos;
        float dist = diff.magnitude;

        if (dist >= desiredSeparation || dist <= 0f)
            return Vector3.zero;

        return diff.normalized * (1f - dist / desiredSeparation) * maxSpeed;
    }

    // ---------------------------------------------
    // Alignment — match a group's heading
    // ---------------------------------------------
    public static Vector3 Alignment(Vector3 groupDir, float maxSpeed)
    {
        return groupDir.normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Orbit — circle around a target at a set radius
    // ---------------------------------------------
    public static Vector3 Orbit(Vector3 position, Vector3 targetPos, float orbitRadius, float maxSpeed)
    {
        Vector3 toAgent   = position - targetPos;
        Vector3 tangent   = Vector3.Cross(Vector3.up, toAgent).normalized;
        float   dist      = toAgent.magnitude;
        float   speedScale = dist < orbitRadius ? dist / orbitRadius : orbitRadius / dist;
        return tangent * maxSpeed * speedScale;
    }

    // ---------------------------------------------
    // Pursue — intercept a moving target
    // ---------------------------------------------
    public static Vector3 Pursue(Vector3 position, Vector3 targetPos, Vector3 targetVelocity, float maxSpeed)
    {
        Vector3 toTarget = targetPos - position;
        float prediction = toTarget.magnitude / maxSpeed;
        return Seek(position, targetPos + targetVelocity * prediction, maxSpeed);
    }

    // ---------------------------------------------
    // Evade — escape from a moving pursuer
    // ---------------------------------------------
    public static Vector3 Evade(Vector3 position, Vector3 targetPos, Vector3 targetVelocity, float maxSpeed)
    {
        Vector3 toTarget = targetPos - position;
        float prediction = toTarget.magnitude / maxSpeed;
        Vector3 futurePos = targetPos + targetVelocity * prediction;
        return (position - futurePos).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Containment — keep agent inside a spherical region
    // ---------------------------------------------
    // Returns zero when safely inside; steers back toward center when the predicted
    // position breaches the region boundary.
    public static Vector3 Containment(Vector3 position, Vector3 velocity,
                                       Vector3 regionCenter, float regionRadius,
                                       float lookAheadTime, float maxSpeed)
    {
        Vector3 predicted = position + velocity * lookAheadTime;
        float distFromCenter = Vector3.Distance(predicted, regionCenter);

        if (distFromCenter < regionRadius)
            return Vector3.zero;

        // Urgency scales with how far outside the predicted point lands
        float urgency = Mathf.Clamp01((distFromCenter - regionRadius) / regionRadius + 0.5f);
        return (regionCenter - position).normalized * maxSpeed * urgency;
    }

    // ---------------------------------------------
    // Formation — hold an assigned slot relative to a leader
    // ---------------------------------------------
    public static Vector3 Formation(Vector3 position, Transform leader, Vector3 localOffset,
                                     float maxSpeed, float slowRadius, float arriveRadius)
    {
        return Arrive(position, leader.TransformPoint(localOffset), maxSpeed, slowRadius, arriveRadius);
    }

    // ---------------------------------------------
    // Patrol — loop through a set of waypoints
    // ---------------------------------------------
    public static Vector3 Patrol(Vector3 position, Transform[] waypoints,
                                  ref int waypointIndex, float waypointRadius, float maxSpeed)
    {
        if (waypoints == null || waypoints.Length == 0)
            return Vector3.zero;

        if (Vector3.Distance(position, waypoints[waypointIndex].position) < waypointRadius)
            waypointIndex = (waypointIndex + 1) % waypoints.Length;

        return Seek(position, waypoints[waypointIndex].position, maxSpeed);
    }

    // ---------------------------------------------
    // AttackRun — approach, fly through, then break off
    // ---------------------------------------------
    // Phase 1 (outside attackRange): Pursue.
    // Phase 2 (inside range, target ahead, not too close): hold heading — fly through.
    // Phase 3 (past target or inside breakOffRange): break off perpendicular + rearward.
    public static Vector3 AttackRun(Vector3 position, Vector3 forward,
                                     Vector3 targetPos, Vector3 targetVelocity,
                                     float maxSpeed, float attackRange, float breakOffRange)
    {
        Vector3 toTarget = targetPos - position;
        float distance   = toTarget.magnitude;

        if (distance > attackRange)
            return Pursue(position, targetPos, targetVelocity, maxSpeed);

        float dot = Vector3.Dot(forward, toTarget.normalized);

        if (dot > 0.2f && distance > breakOffRange)
            return forward * maxSpeed; // fly through

        // Break off perpendicular to target vector, biased away.
        // Use ship forward to pick the break side — ships arriving from different angles break off differently.
        Vector3 breakRef = Mathf.Abs(Vector3.Dot(toTarget.normalized, forward)) < 0.95f
            ? forward : Vector3.up;
        Vector3 perp = Vector3.Cross(toTarget.normalized, breakRef).normalized;
        return (perp - toTarget.normalized * 0.5f).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // ClampDirection — limit per-frame direction change
    // ---------------------------------------------
    public static Vector3 ClampDirection(Vector3 currentDir, Vector3 desiredDir, float maxAngleDeg)
    {
        if (Vector3.Angle(currentDir, desiredDir) <= maxAngleDeg)
            return desiredDir;

        return Vector3.RotateTowards(currentDir, desiredDir, Mathf.Deg2Rad * maxAngleDeg, 0f);
    }
}
