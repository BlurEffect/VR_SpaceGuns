using UnityEngine;

// Per-ship tactical brain. Acquires targets via sensor profile, assigns them to turrets,
// and swaps the steering behavior profile between attack and patrol.
public class ShipAI : MonoBehaviour
{
    [Header("Profiles")]
    public ShipSensorProfile sensorProfile;
    public ShipSteeringBehaviorProfile attackProfile;
    public ShipSteeringBehaviorProfile patrolProfile;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolWaypoints;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.5f;

    [Header("Turrets")]
    [SerializeField] private Targeting[] turrets;
    [SerializeField] private Shooting[] guns;

    public Transform AssignedTarget { get; private set; }
    public SteeringAgent SteeringAgent => _steeringAgent;

    private SteeringAgent _steeringAgent;
    private float _scanTimer;

    void Awake()
    {
        _steeringAgent = GetComponent<SteeringAgent>();
    }

    void Update()
    {
        _scanTimer += Time.deltaTime;
        if (_scanTimer >= scanInterval)
        {
            _scanTimer = 0f;
            if (AssignedTarget == null)
                ScanForTarget();
        }

        UpdateSteering();
        UpdateTurrets();
    }

    // Called by FactionManager to override self-scan with a specific target.
    public void AssignTarget(Transform t)
    {
        AssignedTarget = t;
    }

    // Returns the ship to autonomous self-scanning.
    public void ClearTarget()
    {
        AssignedTarget = null;
    }

    private void ScanForTarget()
    {
        if (sensorProfile == null) return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, sensorProfile.detectionRange, sensorProfile.enemyMask);

        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            float sqDist = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = hit.transform;
            }
        }

        AssignedTarget = nearest;
    }

    private void UpdateSteering()
    {
        if (_steeringAgent == null) return;

        if (AssignedTarget != null)
        {
            _steeringAgent.seekTarget = AssignedTarget;
            _steeringAgent.seekTargetRigidbody = AssignedTarget.GetComponent<Rigidbody>();
            if (attackProfile != null)
                _steeringAgent.behaviorProfile = attackProfile;
        }
        else
        {
            _steeringAgent.seekTarget = null;
            _steeringAgent.seekTargetRigidbody = null;
            _steeringAgent.patrolWaypoints = patrolWaypoints;
            if (patrolProfile != null)
                _steeringAgent.behaviorProfile = patrolProfile;
        }
    }

    private void UpdateTurrets()
    {
        foreach (Targeting turret in turrets)
        {
            if (turret == null) continue;
            turret.target = AssignedTarget;
            turret.targetRigidbody = AssignedTarget != null
                ? AssignedTarget.GetComponent<Rigidbody>()
                : null;
        }

        float sqDist = AssignedTarget != null
            ? (AssignedTarget.position - transform.position).sqrMagnitude
            : float.MaxValue;

        foreach (Shooting gun in guns)
        {
            if (gun == null || gun.GunProfile == null) continue;
            float range = gun.GunProfile.effectiveRange;
            gun.enabled = AssignedTarget != null && sqDist <= range * range;
        }
    }
}
