using UnityEngine;

public static class SteeringModule
{
    // ---------------------------------------------
    // Seek - Fly directly towards a target
    // ---------------------------------------------
    public static Vector3 Seek(Vector3 position, Vector3 targetPos, float maxSpeed)
    {
        return (targetPos - position).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Flee - Fly directly away from a target
    // ---------------------------------------------
    public static Vector3 Flee(Vector3 position, Vector3 targetPos, float maxSpeed)
    {
        return -(targetPos - position).normalized * maxSpeed;
    }

    // ---------------------------------------------
    // Arrive - Move towards a target like seek, but slow down as we approach until eventually coming to a standstill
    // ---------------------------------------------
    public static Vector3 Arrive(Vector3 position, Vector3 targetPos, float maxSpeed, float slowRadius, float arriveRadius)
    {
        Vector3 toTarget = targetPos - position;
        float distanceToTarget = toTarget.magnitude;
        
        // We have arrived at the target 
        if (distanceToTarget < arriveRadius)
        {
            return Vector3.zero;
        }

        // Slow down linearly as we approach the target
        float speedFactor = Mathf.Clamp01(distanceToTarget / slowRadius);
        
        return toTarget.normalized * maxSpeed * speedFactor;
    }

    
    
    
    
    
    // ---------------------------------------------
    // WANDER (random but smooth)
    // ---------------------------------------------

    public static Vector3 Wander(Vector3 forward, ref Vector3 wanderTarget, float jitter, float radius, float maxSpeed)
    {
        // TODO: Recheck this behavior
        
        // jitter the wander target
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter
        );

        // project onto sphere
        wanderTarget = wanderTarget.normalized * radius;

        Vector3 targetWorld = forward + wanderTarget;
        return targetWorld.normalized;
    }

    // TODO: Check/Adapt behavior below
    
    // ---------------------------------------------
    // OBSTACLE AVOIDANCE (simple raycast)
    // ---------------------------------------------
    public static Vector3 AvoidObstacles(Transform self, float avoidDistance, LayerMask mask)
    {
        if (Physics.Raycast(self.position, self.forward, out RaycastHit hit, avoidDistance, mask))
        {
            // Steer sideways away from the hit normal
            Vector3 steer = Vector3.Reflect(self.forward, hit.normal);
            return steer.normalized;
        }

        return Vector3.zero;
    }

    // ---------------------------------------------
    // COHESION (steer toward center of group)
    // ---------------------------------------------
    public static Vector3 Cohesion(Transform self, Vector3 groupCenter)
    {
        return (groupCenter - self.position).normalized;
    }

    // ---------------------------------------------
    // SEPARATION (steer away from neighbors)
    // ---------------------------------------------
    public static Vector3 Separation(Transform self, Vector3 neighborPos, float desiredSeparation)
    {
        Vector3 diff = self.position - neighborPos;
        float dist = diff.magnitude;

        if (dist < desiredSeparation)
            return diff.normalized * (1f - (dist / desiredSeparation));

        return Vector3.zero;
    }

    // ---------------------------------------------
    // ALIGNMENT (match group direction)
    // ---------------------------------------------
    public static Vector3 Alignment(Vector3 groupDir)
    {
        return groupDir.normalized;
    }
    
    // --- NEW BEHAVIORS ---

    // Orbit around a target at a given radius
    public static Vector3 Orbit(Vector3 position, Vector3 targetPos, Vector3 forward, float orbitRadius, float maxSpeed)
    {
        Vector3 toTarget = position - targetPos;
        Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized; // horizontal orbit
        Vector3 desired = tangent * maxSpeed;

        // Slow down if too far from orbit radius
        float dist = toTarget.magnitude;
        if (dist < orbitRadius)
            desired *= dist / orbitRadius;
        else if (dist > orbitRadius)
            desired *= orbitRadius / dist;

        return desired;
    }

    // Pursuit: move toward where the target will be
    public static Vector3 Pursue(Vector3 position, Vector3 targetPos, Vector3 targetVelocity, float maxSpeed)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;
        float speed = maxSpeed;

        float prediction = distance / maxSpeed;
        Vector3 futurePos = targetPos + targetVelocity * prediction;

        return Seek(position, futurePos, maxSpeed);
    }

    // Evade: move away from where the target will be
    public static Vector3 Evade(Vector3 position, Vector3 targetPos, Vector3 targetVelocity, float maxSpeed)
    {
        Vector3 toTarget = targetPos - position;
        float distance = toTarget.magnitude;
        float prediction = distance / maxSpeed;
        Vector3 futurePos = targetPos + targetVelocity * prediction;

        return (position - futurePos).normalized * maxSpeed;
    }
    
    
    
    
    
    
    
    // --- Clamp the change in direction ---
    public static Vector3 ClampDirection(Vector3 currentDir, Vector3 desiredDir, float maxAngleDeg)
    {
        float angle = Vector3.Angle(currentDir, desiredDir);

        if (angle <= maxAngleDeg)
            return desiredDir;

        return Vector3.RotateTowards(currentDir, desiredDir, Mathf.Deg2Rad * maxAngleDeg, 0f);
    }
}
