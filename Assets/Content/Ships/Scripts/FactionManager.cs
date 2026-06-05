using System.Collections.Generic;
using UnityEngine;

// Per-faction strategic coordinator. Distributes targets across ships and issues
// faction-wide orders. Ships that have no assigned target fall back to self-scanning.
public class FactionManager : MonoBehaviour
{
    [SerializeField] private List<ShipAI> ships = new();

    private readonly List<Transform> _enemyTargets = new();

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
}
