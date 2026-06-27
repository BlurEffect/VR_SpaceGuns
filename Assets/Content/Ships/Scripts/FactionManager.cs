using System.Collections.Generic;
using UnityEngine;

// Per-faction strategic coordinator. Distributes targets across ships and issues
// faction-wide orders. Ships that have no assigned target fall back to self-scanning.
public class FactionManager : MonoBehaviour
{
    [SerializeField] private List<ShipAI> ships = new();

    [Header("Patrol")]
    [SerializeField] private Transform patrolRouteRoot;

    private Transform[] _patrolWaypoints = System.Array.Empty<Transform>();

    private readonly List<Transform> _enemyTargets = new();

    void Start()
    {
        if (patrolRouteRoot == null) return;
        _patrolWaypoints = new Transform[patrolRouteRoot.childCount];
        for (int i = 0; i < patrolRouteRoot.childCount; i++)
            _patrolWaypoints[i] = patrolRouteRoot.GetChild(i);
    }

    public void RegisterShip(ShipAI ship)
    {
        if (!ships.Contains(ship))
            ships.Add(ship);
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!_enemyTargets.Contains(enemy))
            _enemyTargets.Add(enemy);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        _enemyTargets.Remove(enemy);
    }

    // Round-robin assignment — each ship gets a different target where possible.
    // Ships already assigned by this method keep their assignment until redistributed.
    public void DistributeTargets()
    {
        _enemyTargets.RemoveAll(t => t == null);
        ships.RemoveAll(s => s == null);

        if (_enemyTargets.Count == 0)
        {
            OrderClearTargets();
            return;
        }

        for (int i = 0; i < ships.Count; i++)
            ships[i].AssignTarget(_enemyTargets[i % _enemyTargets.Count]);
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

            // Find nearest other friendly; default to own position so separation force stays zero when alone.
            Vector3 nearestPos = ship.transform.position;
            float   nearestSq  = float.MaxValue;
            foreach (ShipAI other in ships)
            {
                if (other == ship) continue;
                float sq = (other.transform.position - ship.transform.position).sqrMagnitude;
                if (sq < nearestSq)
                {
                    nearestSq  = sq;
                    nearestPos = other.transform.position;
                }
            }
            agent.neighborPosition = nearestPos;
            agent.patrolWaypoints  = _patrolWaypoints;
        }
    }
}
