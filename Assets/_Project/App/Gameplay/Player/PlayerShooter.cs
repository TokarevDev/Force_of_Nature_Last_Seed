using UnityEngine;

/// <summary>
/// Controls player's weapon firing logic.
/// Works with any weapon implementation via IWeapon.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerShooter : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private MonoBehaviour _weaponBehaviour;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;

    [Header("Refs")]
    [SerializeField] private PoolRegistry _registry;

    [SerializeField] private Transform _firePoint;

    [Header("Start Weapon")]
    [SerializeField] private WeaponConfig _startConfig;

    private IWeapon _weapon;
    private bool _initialized;

#if UNITY_EDITOR
    public WeaponConfig EditorStartConfig => _startConfig;
#endif

    public void Init(IScreenBounds screenBounds)
    {
        if (_initialized)
            return;

        _weapon = _weaponBehaviour as IWeapon;

        if (_weapon == null)
        {
            Debug.LogError("Assigned weapon does not implement IWeapon.", this);
            return;
        }

        if (_startConfig == null)
        {
            Debug.LogError("Start Weapon Config is missing.", this);
            return;
        }

        if (_startConfig.Projectile == null)
        {
            Debug.LogError("ProjectileConfig is missing.", this);
            return;
        }

        if (_registry == null)
        {
            Debug.LogError("Pool registry is missing.", this);
            return;
        }

        if (_firePoint == null)
        {
            Debug.LogError("Fire point is missing.", this);
            return;
        }

        var projectilePrefab = _startConfig.Projectile.Prefab;
        var pool = _registry.GetPool(projectilePrefab);

        if (pool == null)
            return;

        _weapon.Init(pool, _firePoint);
        _weapon.ApplyConfig(_startConfig);

        if (_acaciaThornWeapon != null)
            _acaciaThornWeapon.Init(_firePoint, screenBounds, _registry.transform);

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        if (GameplayInputBlocker.IsBlocked)
            return;

        if (!CombatState.CanShoot)
            return;

        _weapon.Tick();
        _acaciaThornWeapon?.Tick();
    }
}
