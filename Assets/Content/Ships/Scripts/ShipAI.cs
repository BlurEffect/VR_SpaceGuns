using UnityEngine;

// Per-ship tactical brain. Acquires targets via sensor profile, assigns them to turrets,
// and swaps the steering behavior profile between attack and patrol.
public class ShipAI : MonoBehaviour
{
    [Header("Profiles")]
    public ShipClass shipClass = ShipClass.Fighter;
    public ShipSensorProfile sensorProfile;
    public ShipSteeringBehaviorProfile attackProfile;
    public ShipSteeringBehaviorProfile patrolProfile;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolWaypoints;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.5f;
    [SerializeField] private bool selfScanForTargets = true;

    [Header("Primary Turrets")]
    [SerializeField] private TurretMount[] primaryMounts;

    [Header("Point Defense")]
    [SerializeField] private TurretMount[] pointDefenseMounts;

    public Transform AssignedTarget  { get; private set; }
    public Transform MovementTarget  { get; private set; }
    public SteeringAgent SteeringAgent => _steeringAgent;

    private SteeringAgent _steeringAgent;
    private float _scanTimer;
    private Collider[] _scannedEnemies = System.Array.Empty<Collider>();

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
            ScanForEnemies();
            if (selfScanForTargets && AssignedTarget == null)
                AssignedTarget = FindNearest();
        }

        UpdateSteering();
        UpdateTurrets();
        UpdatePointDefenseTurrets();
    }

    public void AssignTarget(Transform t)        => AssignedTarget  = t;
    public void ClearTarget()                    => AssignedTarget  = null;
    public void AssignMovementTarget(Transform t) => MovementTarget = t;
    public void ClearMovementTarget()             => MovementTarget = null;

    private void ScanForEnemies()
    {
        if (sensorProfile == null) return;
        _scannedEnemies = Physics.OverlapSphere(
            transform.position, sensorProfile.detectionRange, sensorProfile.enemyMask);
    }

    private Transform FindNearest()
    {
        Transform nearest = null;
        float nearestSq = float.MaxValue;
        foreach (Collider hit in _scannedEnemies)
        {
            float sq = (hit.transform.position - transform.position).sqrMagnitude;
            if (sq < nearestSq) { nearestSq = sq; nearest = hit.transform; }
        }
        return nearest;
    }

    private void UpdateSteering()
    {
        if (_steeringAgent == null) return;

        if (MovementTarget != null)
        {
            // Explicit movement order takes priority over combat and patrol.
            // AssignedTarget still drives turrets independently.
            _steeringAgent.seekTarget          = MovementTarget;
            _steeringAgent.seekTargetRigidbody = null;
            _steeringAgent.behaviorProfile     = AssignedTarget != null ? attackProfile : patrolProfile;
        }
        else if (AssignedTarget != null)
        {
            _steeringAgent.seekTarget          = AssignedTarget;
            _steeringAgent.seekTargetRigidbody = AssignedTarget.GetComponent<Rigidbody>();
            _steeringAgent.behaviorProfile     = attackProfile;
        }
        else
        {
            _steeringAgent.seekTarget          = null;
            _steeringAgent.seekTargetRigidbody = null;
            _steeringAgent.patrolWaypoints     = patrolWaypoints;
            _steeringAgent.behaviorProfile     = patrolProfile;
        }
    }

    private void UpdateTurrets()
    {
        float sqDist = AssignedTarget != null
            ? (AssignedTarget.position - transform.position).sqrMagnitude
            : float.MaxValue;

        foreach (TurretMount mount in primaryMounts)
        {
            if (mount.targeting == null) continue;

            mount.targeting.target          = AssignedTarget;
            mount.targeting.targetRigidbody = AssignedTarget != null
                ? AssignedTarget.GetComponent<Rigidbody>() : null;

            if (mount.shooting != null && mount.shooting.GunProfile != null)
            {
                float range = mount.shooting.GunProfile.effectiveRange;
                mount.shooting.enabled = AssignedTarget != null
                    && sqDist <= range * range
                    && mount.targeting.ReadyToFire;
            }
        }
    }

    private void UpdatePointDefenseTurrets()
    {
        foreach (TurretMount mount in pointDefenseMounts)
        {
            if (mount.targeting == null) continue;

            Transform nearest = null;
            float nearestSq = float.MaxValue;
            foreach (Collider hit in _scannedEnemies)
            {
                float sq = (hit.transform.position - mount.targeting.transform.position).sqrMagnitude;
                if (sq < nearestSq) { nearestSq = sq; nearest = hit.transform; }
            }

            mount.targeting.target = nearest;
            mount.targeting.targetRigidbody = nearest != null
                ? nearest.GetComponent<Rigidbody>() : null;

            if (mount.shooting != null && mount.shooting.GunProfile != null)
            {
                float range = mount.shooting.GunProfile.effectiveRange;
                mount.shooting.enabled = nearest != null
                    && nearestSq <= range * range
                    && mount.targeting.ReadyToFire;
            }
        }
    }
}

[System.Serializable]
public struct TurretMount
{
    public Targeting targeting;
    public Shooting  shooting;
}
