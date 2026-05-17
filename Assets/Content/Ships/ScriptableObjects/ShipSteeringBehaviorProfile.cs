using UnityEngine;

// Weight profile for a ship AI role — tweak per role (fighter attack, patrol, evade, etc.)
[CreateAssetMenu(fileName = "ShipSteeringBehaviorProfile", menuName = "Scriptable Objects/ShipSteeringBehaviorProfile")]
public class ShipSteeringBehaviorProfile : ScriptableObject
{
    [Header("Basic")]
    public float seekWeight      = 0f;
    public float fleeWeight      = 0f;
    public float arriveWeight    = 0f;

    [Header("Wander")]
    public float wanderWeight    = 0f;

    [Header("Avoidance")]
    public float avoidWeight     = 0f;

    [Header("Flocking")]
    public float cohesionWeight  = 0f;
    public float separationWeight= 0f;
    public float alignmentWeight = 0f;

    [Header("Targeting")]
    public float orbitWeight     = 0f;
    public float pursueWeight    = 0f;
    public float evadeWeight     = 0f;
    public float attackRunWeight = 0f;

    [Header("Navigation")]
    public float containmentWeight = 0f;
    public float formationWeight   = 0f;
    public float patrolWeight      = 0f;

    [Header("Arrive Params")]
    public float slowRadius   = 30f;
    public float arriveRadius =  5f;

    [Header("Attack Run Params")]
    public float attackRange   = 20f;
    public float breakOffRange =  5f;
}
