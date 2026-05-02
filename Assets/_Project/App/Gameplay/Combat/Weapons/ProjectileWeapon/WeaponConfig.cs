using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Weapon Config")]
public sealed class WeaponConfig : ScriptableObject
{
    [Header("Base")]
    public float FireRate = 2.2f;

    [Header("Fire Rate Limits")]
    [Min(0.05f)] public float MinShotCooldown = 0.5f;
    [Min(0f)] public float MaxFireRateBonus = WeaponRuntimeState.DefaultMaxFireRateBonus;

    [Header("Projectile Speed Limits")]
    [Min(0f)] public float MaxProjectileSpeedBonus = WeaponRuntimeState.DefaultMaxProjectileSpeedBonus;

    [Header("Progression Limits")]
    [Min(1f)] public float MaxDamageMultiplier = WeaponRuntimeState.MaxDamageMultiplier;
    [Range(0f, WeaponRuntimeState.MaxCriticalChance)] public float MaxCriticalChance = WeaponRuntimeState.MaxCriticalChance;
    [Min(1f)] public float MaxCriticalDamageMultiplier = WeaponRuntimeState.MaxCriticalDamageMultiplier;
    [Range(0, WeaponRuntimeState.MaxPenetrationBonus)] public int MaxPenetrationBonus = WeaponRuntimeState.MaxPenetrationBonus;
    [Range(1, WeaponRuntimeState.MaxParallelProjectiles)] public int MaxParallelProjectiles = WeaponRuntimeState.DefaultMaxParallelProjectiles;
    [Range(0, WeaponRuntimeState.MaxSalvoExtraShots)] public int MaxSalvoExtraShots = WeaponRuntimeState.DefaultMaxSalvoExtraShots;

    [Header("Projectile")]
    public ProjectileConfig Projectile;
}
