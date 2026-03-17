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

    [Header("Refs")]
    [SerializeField] private PoolRegistry _registry;

    [SerializeField] private Transform _firePoint;

    [Header("Start Weapon")]
    [SerializeField] private WeaponConfig _startConfig;

    private IWeapon _weapon;
    private bool _canShoot;

    private void Awake()
    {
        _weapon = _weaponBehaviour as IWeapon;

        if (_weapon == null)
        {
            Debug.LogError("Assigned weapon does not implement IWeapon.", this);
            return;
        }

        var runtimeConfig = _startConfig.CreateRuntimeInstance();

        if (runtimeConfig.Projectile == null)
        {
            Debug.LogError("ProjectileConfig is missing", this);
            return;
        }

        var projectilePrefab = runtimeConfig.Projectile.Prefab;
        var pool = _registry.GetPool(projectilePrefab);

        _weapon.Init(pool, _firePoint);
        _weapon.ApplyConfig(runtimeConfig);
    }

    private void Start()
    {
        _canShoot = true;
    }

    private void Update()
    {
        if (!_canShoot) return;
        _weapon.Tick();
    }

    public void EnableShooting() => _canShoot = true;

    public void DisableShooting() => _canShoot = false;
}