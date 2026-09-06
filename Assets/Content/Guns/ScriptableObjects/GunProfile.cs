using UnityEngine;

public enum FiringMode
{
    Constant,
    Burst
}

[CreateAssetMenu(fileName = "GunProfile", menuName = "Scriptable Objects/GunProfile")]
public class GunProfile : ScriptableObject
{
    [Header("Firing")]
    public FiringMode mode;
    public float cooldownInterval = 0.5f;
    public int projectilesPerBurst = 3;
    public float burstInterval = 0.1f;

    [Header("Projectile")] public Vector3 projectileSize = new Vector3(1.0f, 1.0f, 12.0f);
    public float projectileSpeed = 100f;
    public float projectileLifetime = 5f;
    public float effectiveRange = 500f;
    [ColorUsage(false, true)]
    public Color projectileColor = Color.white;

    [Header("Damage")]
    public float shieldDamage = 10f;
    public float hullDamage = 10f;
}
