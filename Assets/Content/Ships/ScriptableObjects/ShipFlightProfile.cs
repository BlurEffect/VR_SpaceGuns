using UnityEngine;

[CreateAssetMenu(fileName = "ShipFlightProfile", menuName = "Scriptable Objects/ShipFlightProfile")]
public class ShipFlightProfile : ScriptableObject
{
    [Header("Speed")]
    public float maxSpeed     = 20f;
    public float acceleration = 15f;

    [Header("Turning")]
    public float turnRate           =  3f;
    [Range(1f, 5f)] public float driftDamping        =  2f;
    [Range(0f, 1f)] public float driftDampingFactor  =  1f;

    [Header("Banking")]
    public float bankAmount = 60f;
    public float bankSmooth =  3f;
}
