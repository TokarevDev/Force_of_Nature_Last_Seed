using UnityEngine;

/// <summary>
/// Common interface for all weapon types.
/// Allows Player systems to operate on weapons without knowing implementation details.
/// </summary>
public interface IWeapon
{
    void Init(ProjectilePool pool, Transform firePoint);

    void Tick();

    void ApplyConfig(WeaponConfig config);
}