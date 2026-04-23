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

    private void Awake()
    {
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

        var projectilePrefab = _startConfig.Projectile.Prefab;
        var pool = _registry.GetPool(projectilePrefab);

        _weapon.Init(pool, _firePoint);
        _weapon.ApplyConfig(_startConfig);
    }

    private void Update()
    {
        if (!CombatState.CanShoot)
            return;

        _weapon.Tick();
    }
}