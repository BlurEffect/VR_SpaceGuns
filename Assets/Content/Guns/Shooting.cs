using UnityEngine;

public class Shooting : MonoBehaviour
{
    [SerializeField] private GunProfile gunProfile;
    public GunProfile GunProfile => gunProfile;

    [SerializeField] private Transform muzzleLeft;
    [SerializeField] private Transform muzzleRight;

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
        ProjectileManager.Instance.SpawnProjectile(
            muzzleRight.position, muzzleRight.forward,
            gunProfile.projectileSpeed, gunProfile.projectileColor,
            gunProfile.projectileLifetime,
            gunProfile.shieldDamage, gunProfile.hullDamage);

        ProjectileManager.Instance.SpawnProjectile(
            muzzleLeft.position, muzzleLeft.forward,
            gunProfile.projectileSpeed, gunProfile.projectileColor,
            gunProfile.projectileLifetime,
            gunProfile.shieldDamage, gunProfile.hullDamage);
    }
}
