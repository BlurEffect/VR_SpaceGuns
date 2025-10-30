using System;
using UnityEngine;


public enum FiringMode
{
    Constant,
    Burst
}

[Serializable]
public struct FiringPattern
{
    public FiringMode mode;
    public float cooldownInterval;
    public int projectilesPerBurst;
    public float burstInterval;
    public float projectileSpeed;
}

public class Shooting : MonoBehaviour
{
    [SerializeField] public FiringPattern pattern;
    
    private float cooldownTimer = 0.0f;
    private float burstTimer = 0.0f;
    private int burstCount = 0;
    
    [ColorUsage(false, true)]
    [SerializeField] private Color projectileColor;
    
    [SerializeField] private Transform muzzleLeft;       // Barrel - rotates vertically (pitch)
    [SerializeField] private Transform muzzleRight;       // Barrel - rotates vertically (pitch)
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cooldownTimer += Time.deltaTime;
        burstTimer += Time.deltaTime;


        if (cooldownTimer >= pattern.cooldownInterval)
        {
            if (pattern.mode == FiringMode.Burst)
            {
                if (burstTimer >= pattern.burstInterval)
                {
                    FireShot();
                    ++burstCount;

                    if (burstCount >= pattern.projectilesPerBurst)
                    {
                        // Shot all projectiles for this burst
                        burstCount = 0;
                        // Start cooldown
                        cooldownTimer = 0.0f;
                    }

                    burstTimer = 0.0f;
                }
            }
            else
            {
                FireShot();
                cooldownTimer = 0.0f;
            }
        }
    }

    private void FireShot()
    {
        ProjectileManager.Instance.SpawnProjectile(muzzleRight.position, muzzleRight.forward, pattern.projectileSpeed, projectileColor,10.0f);
        ProjectileManager.Instance.SpawnProjectile(muzzleLeft.position, muzzleLeft.forward, pattern.projectileSpeed, projectileColor,10.0f);
    }
}
