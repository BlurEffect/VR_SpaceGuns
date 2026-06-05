using UnityEngine;

[CreateAssetMenu(fileName = "ShipSteeringProfile", menuName = "Scriptable Objects/ShipSteeringProfile")]
public class ShipSteeringProfile : ScriptableObject
{
    [Header("Avoidance")]
    public float     avoidDistance = 15f;
    public float     shipRadius    =  0.5f;
    public LayerMask obstacleMask;

    [Header("Separation")]
    public float separationDistance = 10f;

    [Header("Steering")]
    public float maxSteeringAngle = 90f;

    [Header("Wander")]
    public float wanderJitter          = 0.5f;
    public float wanderRadius          = 1.5f;
    public float wanderProjectDistance = 2f;

    [Header("Patrol")]
    public float waypointReachRadius = 3f;

    [Header("Arrive")]
    public float slowRadius   = 30f;
    public float arriveRadius =  5f;
}
