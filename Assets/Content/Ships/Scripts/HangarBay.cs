using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns a capital ship's fighter/drone complement: launch slots, faction registration,
// live-count tracking, and auto-respawn top-up during battle. GameManager only issues
// LaunchFighters/LaunchDrones and StartAutoRespawn/StopAutoRespawn commands.
public class HangarBay : MonoBehaviour
{
    [Header("Slot Roots (children = individual launch slots)")]
    [SerializeField] private Transform fighterSlotsRoot;
    [SerializeField] private Transform droneSlotsRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject fighterPrefab;
    [SerializeField] private GameObject dronePrefab;

    [Header("Auto-Respawn (Battle-phase top-up)")]
    [SerializeField] private int   maxFighters     = 6;
    [SerializeField] private int   maxDrones       = 3;
    [SerializeField] private float respawnInterval = 25f;

    private FactionManager _ownFaction;
    private FactionManager _enemyFaction;
    private Transform      _containmentAnchor;
    private readonly List<ShipAI> _liveFighters = new();
    private readonly List<ShipAI> _liveDrones   = new();
    private int _fighterSlotIdx;
    private int _droneSlotIdx;
    private Coroutine _autoRespawnRoutine;

    public int DestroyedCount { get; private set; }

    public void SetFactions(FactionManager ownFaction, FactionManager enemyFaction)
    {
        _ownFaction   = ownFaction;
        _enemyFaction = enemyFaction;
    }

    // Point where launched fighters/drones get steered back toward (SteeringAgent.containmentCenter)
    // so dogfights stay near the player instead of drifting off wherever combat targets lead them.
    public void SetContainmentAnchor(Transform anchor)
    {
        _containmentAnchor = anchor;
    }

    public void LaunchFighters(int count)
    {
        for (int i = 0; i < count; i++)
            LaunchOne(fighterPrefab, fighterSlotsRoot, ref _fighterSlotIdx, _liveFighters);
    }

    public void LaunchDrones(int count)
    {
        if (dronePrefab == null || droneSlotsRoot == null) return;
        for (int i = 0; i < count; i++)
            LaunchOne(dronePrefab, droneSlotsRoot, ref _droneSlotIdx, _liveDrones);
    }

    public void StartAutoRespawn()
    {
        if (_autoRespawnRoutine != null) return;
        _autoRespawnRoutine = StartCoroutine(AutoRespawnLoop());
    }

    public void StopAutoRespawn()
    {
        if (_autoRespawnRoutine == null) return;
        StopCoroutine(_autoRespawnRoutine);
        _autoRespawnRoutine = null;
    }

    IEnumerator AutoRespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnInterval);

            _liveFighters.RemoveAll(ai => ai == null);
            _liveDrones.RemoveAll(ai => ai == null);

            if (_liveFighters.Count < maxFighters)
                LaunchOne(fighterPrefab, fighterSlotsRoot, ref _fighterSlotIdx, _liveFighters);

            if (dronePrefab != null && droneSlotsRoot != null && _liveDrones.Count < maxDrones)
                LaunchOne(dronePrefab, droneSlotsRoot, ref _droneSlotIdx, _liveDrones);

            _ownFaction?.DistributeTargets();
        }
    }

    ShipAI LaunchOne(GameObject prefab, Transform slotsRoot, ref int slotIdx, List<ShipAI> trackingList)
    {
        if (prefab == null || slotsRoot == null || slotsRoot.childCount == 0 || _ownFaction == null) return null;

        Transform slot = slotsRoot.GetChild(slotIdx % slotsRoot.childCount);
        slotIdx++;

        GameObject go = Instantiate(prefab, slot.position, slot.rotation);
        ShipAI ai = go.GetComponent<ShipAI>();

        if (ai != null)
        {
            _ownFaction.RegisterShip(ai);
            if (_containmentAnchor != null)
                ai.SteeringAgent.containmentCenter = _containmentAnchor;
        }
        _enemyFaction?.RegisterEnemy(go.transform, ShipClass.Fighter);
        trackingList.Add(ai);

        HealthComponent hp = go.GetComponent<HealthComponent>();
        if (hp != null)
            hp.OnDestroyed.AddListener(() => HandleLaunchedShipDestroyed(go, ai, trackingList));

        return ai;
    }

    void HandleLaunchedShipDestroyed(GameObject ship, ShipAI ai, List<ShipAI> trackingList)
    {
        _enemyFaction?.UnregisterEnemy(ship.transform);
        trackingList.Remove(ai);
        DestroyedCount++;
    }
}
