using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthProfile profile;

    public UnityEvent OnShieldsDown;
    public UnityEvent OnDestroyed;

    private float _currentShields;
    private float _currentHull;
    private float _lastHitTime = float.MinValue;
    private bool _shieldsDownFired;
    private bool _destroyed;

    public float CurrentShields => _currentShields;
    public float CurrentHull => _currentHull;
    public float MaxShields => profile != null ? profile.maxShields : 0f;
    public float MaxHull   => profile != null ? profile.maxHull   : 0f;

    void Awake()
    {
        if (profile == null) return;
        _currentShields = profile.maxShields;
        _currentHull = profile.maxHull;
    }

    public void TakeDamage(float shieldDamage, float hullDamage)
    {
        if (_destroyed) return;

        _lastHitTime = Time.time;

        _currentShields = Mathf.Max(0f, _currentShields - shieldDamage);

        if (_currentShields == 0f)
        {
            if (!_shieldsDownFired)
            {
                _shieldsDownFired = true;
                OnShieldsDown.Invoke();
            }
            _currentHull = Mathf.Max(0f, _currentHull - hullDamage);
        }

        if (_currentHull <= 0f)
        {
            _destroyed = true;
            OnDestroyed.Invoke();
        }
    }

    void Update()
    {
        if (profile == null || profile.shieldRechargeRate <= 0f) return;
        if (_currentShields >= profile.maxShields) return;
        if (Time.time - _lastHitTime < profile.shieldRechargeDelay) return;

        _currentShields = Mathf.Min(profile.maxShields, _currentShields + profile.shieldRechargeRate * Time.deltaTime);

        if (_currentShields > 0f)
            _shieldsDownFired = false;
    }
}
