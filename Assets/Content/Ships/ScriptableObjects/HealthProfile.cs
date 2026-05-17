using UnityEngine;

[CreateAssetMenu(fileName = "HealthProfile", menuName = "Scriptable Objects/HealthProfile")]
public class HealthProfile : ScriptableObject
{
    [Header("Hull")]
    public float maxHull = 100f;

    [Header("Shields")]
    public float maxShields = 50f;
    [Tooltip("Seconds after last hit before shields start recharging. 0 = recharge immediately.")]
    public float shieldRechargeDelay = 3f;
    [Tooltip("Shield points restored per second. 0 = no recharge.")]
    public float shieldRechargeRate = 10f;
}
