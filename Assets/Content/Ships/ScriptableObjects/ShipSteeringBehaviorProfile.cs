using UnityEngine;

// Contains a collection weights for the different steering behaviors for the space ships
// Tweak these weights to define different behavior for different situations

[CreateAssetMenu(fileName = "ShipSteeringBehaviorProfile", menuName = "Scriptable Objects/ShipSteeringBehaviorProfile")]
public class ShipSteeringBehaviorProfile : ScriptableObject
{
    public float seekWeight = 0f;
    public float fleeWeight = 0f;
    public float arriveWeight = 0f;
    
    
    
    
    
    
    public float wanderWeight = 0f;
    public float avoidWeight = 0f;
    public float cohesionWeight = 0f;
    public float separationWeight = 0f;
    public float alignmentWeight = 0f;
    public float orbitWeight = 0f;
    public float pursueWeight = 0f;
    public float evadeWeight = 0f;
}
