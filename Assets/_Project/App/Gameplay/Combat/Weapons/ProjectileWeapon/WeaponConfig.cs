using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Weapon Config")]
public sealed class WeaponConfig : ScriptableObject
{
    [Header("Base")]
    public float FireRate = 2.2f;

    [Header("Fire Rate Limits")]
    [Min(0.05f)] public float MinShotCooldown = 0.5f;
    [Min(0f)] public float MaxFireRateBonus = WeaponRuntimeState.DefaultMaxFireRateBonus;

    [Header("Projectile")]
    public ProjectileConfig Projectile;
}
