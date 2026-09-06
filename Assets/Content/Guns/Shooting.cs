using UnityEngine;

public class Shooting : MonoBehaviour
{
    [SerializeField] private GunProfile gunProfile;
    public GunProfile GunProfile => gunProfile;

    [SerializeField] private Targeting targeting;

    private float _cooldownTimer = float.MaxValue;
    private float _burstTimer;
    private int _burstCount;

    void Update()
    {
        if (gunProfile == null) return;

        _cooldownTimer += Time.deltaTime;
        _burstTimer += Time.deltaTime;

        if (_cooldownTimer >= gunProfile.cooldownInterval)
        {
            if (gunProfile.mode == FiringMode.Burst)
            {
                if (_burstTimer >= gunProfile.burstInterval)
                {
                    FireShot();
                    ++_burstCount;

                    if (_burstCount >= gunProfile.projectilesPerBurst)
                    {
                        _burstCount = 0;
                        _cooldownTimer = 0f;
                    }

                    _burstTimer = 0f;
                }
            }
            else
            {
                FireShot();
                _cooldownTimer = 0f;
            }
        }
    }

    private void FireShot()
    {
        if (targeting == null) return;

        foreach (Barrel b in targeting.Barrels)
        {
            if (b.muzzle == null) continue;

            ProjectileManager.Instance.SpawnProjectile(
                b.muzzle.position, b.muzzle.forward, gunProfile.projectileSize,
                gunProfile.projectileSpeed, gunProfile.projectileColor,
                gunProfile.effectiveRange,
                gunProfile.shieldDamage, gunProfile.hullDamage);
        }
    }
}
