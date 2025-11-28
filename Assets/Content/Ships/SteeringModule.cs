using UnityEngine;

public static class SteeringModule
{
    // ---------------------------------------------
    // SEEK
    // ---------------------------------------------
    public static Vector3 Seek(Transform self, Vector3 targetPos)
    {
        return (targetPos - self.position).normalized;
    }

    // ---------------------------------------------
    // FLEE
    // ---------------------------------------------
    public static Vector3 Flee(Transform self, Vector3 targetPos)
    {
        return -(targetPos - self.position).normalized;
    }

    // ---------------------------------------------
    // ARRIVE (direction normalized)
    // Speed control is handled by FlightController
    // ---------------------------------------------
    public static Vector3 Arrive(Transform self, Vector3 targetPos, float slowRadius)
    {
        Vector3 toTarget = targetPos - self.position;
        float dist = toTarget.magnitude;

        return toTarget.normalized * Mathf.Clamp01(dist / slowRadius);
    }

    // ---------------------------------------------
    // WANDER (random but smooth)
    // ---------------------------------------------
    private static Vector3 wanderTarget = Vector3.forward;

    public static Vector3 Wander(Transform self, float jitter = 0.4f, float radius = 3f)
    {
        // jitter the wander target
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter,
            Random.Range(-1f, 1f) * jitter
        );

        // project onto sphere
        wanderTarget = wanderTarget.normalized * radius;

        Vector3 worldTarget = self.TransformPoint(wanderTarget);
        return (worldTarget - self.position).normalized;
    }

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
}
