using UnityEngine;

[CreateAssetMenu(fileName = "ShipSensorProfile", menuName = "Scriptable Objects/ShipSensorProfile")]
public class ShipSensorProfile : ScriptableObject
{
    public float detectionRange = 200f;
    public LayerMask enemyMask;
}
