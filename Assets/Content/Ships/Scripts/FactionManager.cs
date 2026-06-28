using System.Collections.Generic;
using UnityEngine;

// Per-faction strategic coordinator. Distributes targets across ships and issues
// faction-wide orders. Ships that have no assigned target fall back to self-scanning.
public class FactionManager : MonoBehaviour
{
    [SerializeField] private List<ShipAI> ships = new();

    [Header("Patrol")]
    [SerializeField] private Transform patrolRouteRoot;

    [Header("Initial Enemies (for testing without a GameManager)")]
    [SerializeField] private List<EnemyEntry> initialEnemies = new();

    private Transform[]      _patrolWaypoints = System.Array.Empty<Transform>();
    private List<EnemyEntry> _enemyTargets    = new();
    private List<Transform>  _pool            = new();

    void Start()
    {
        foreach (EnemyEntry entry in initialEnemies)
            if (entry.target != null) _enemyTargets.Add(entry);

        if (patrolRouteRoot != null)
        {
            _patrolWaypoints = new Transform[patrolRouteRoot.childCount];
            for (int i = 0; i < patrolRouteRoot.childCount; i++)
                _patrolWaypoints[i] = patrolRouteRoot.GetChild(i);
        }

        if (_enemyTargets.Count > 0)
            DistributeTargets();
    }

    public void RegisterShip(ShipAI ship)
    {
        if (!ships.Contains(ship))
            ships.Add(ship);
    }

    public void RegisterEnemy(Transform enemy, ShipClass shipClass)
    {
        if (!_enemyTargets.Exists(e => e.target == enemy))
            _enemyTargets.Add(new EnemyEntry { target = enemy, shipClass = shipClass });
    }

    public void UnregisterEnemy(Transform enemy)
    {
        _enemyTargets.RemoveAll(e => e.target == enemy);
    }

    // Round-robin assignment within matching ship class; falls back to all enemies if none match.
    public void DistributeTargets()
    {
        _enemyTargets.RemoveAll(e => e.target == null);
        ships.RemoveAll(s => s == null);

        if (_enemyTargets.Count == 0) { OrderClearTargets(); return; }

        for (int i = 0; i < ships.Count; i++)
        {
            _pool.Clear();
            ShipClass cls = ships[i].shipClass;
            foreach (EnemyEntry e in _enemyTargets)
                if (e.shipClass == cls) _pool.Add(e.target);

            if (_pool.Count == 0)
            {
                if (ships[i].shipClass != ShipClass.Fighter)
                    { ships[i].ClearTarget(); continue; }
                foreach (EnemyEntry e in _enemyTargets) _pool.Add(e.target);
            }

            ships[i].AssignTarget(_pool[i % _pool.Count]);
        }
    }

    // Focus every ship on a single priority target.
    public void OrderAttack(Transform priority)
    {
        foreach (ShipAI ship in ships)
            ship?.AssignTarget(priority);
    }

    // Release all assignments; ships resume autonomous self-scanning.
    public void OrderClearTargets()
    {
        foreach (ShipAI ship in ships)
            ship?.ClearTarget();
    }

    // Order all ships to fly to a position and hold; turret assignments are unaffected.
    public void OrderMoveTo(Transform destination)
    {
        foreach (ShipAI ship in ships)
            ship?.AssignMovementTarget(destination);
    }

    // Release movement order; ships resume patrol or combat steering.
    public void OrderClearMovement()
    {
        foreach (ShipAI ship in ships)
            ship?.ClearMovementTarget();
    }

    void LateUpdate()
    {
        ships.RemoveAll(s => s == null);
        int count = ships.Count;
        if (count == 0) return;

        Vector3 center = Vector3.zero;
        Vector3 dir    = Vector3.zero;
        foreach (ShipAI ship in ships)
        {
            center += ship.transform.position;
            dir    += ship.transform.forward;
        }
        center /= count;
        dir     = (dir / count).normalized;

        foreach (ShipAI ship in ships)
        {
            SteeringAgent agent = ship.SteeringAgent;
            if (agent == null) continue;

            agent.groupCenter    = center;
            agent.groupDirection = dir;

            Vector3 nearestPos = ship.transform.position;
            float   nearestSq  = float.MaxValue;
            foreach (ShipAI other in ships)
            {
                if (other == ship) continue;
                float sq = (other.transform.position - ship.transform.position).sqrMagnitude;
                if (sq < nearestSq) { nearestSq = sq; nearestPos = other.transform.position; }
            }
            agent.neighborPosition = nearestPos;
            agent.patrolWaypoints  = _patrolWaypoints;
        }
    }
}

[System.Serializable]
public struct EnemyEntry
{
    public Transform target;
    public ShipClass shipClass;
}
