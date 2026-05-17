using UnityEngine;

// Contains a collection weights for the different steering behaviors for the space ships
// Tweak these weights to define different behavior for different situations

[CreateAssetMenu(fileName = "ShipSteeringSettings", menuName = "Scriptable Objects/ShipSteeringSettings")]
public class ShipSteeringSettings : ScriptableObject
{
    [Header("Parameters")]
    public float slowRadius = 30f;
    public float arriveRadius = 5f;
    
}
