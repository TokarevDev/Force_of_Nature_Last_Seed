using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Weapon Config")]
public sealed class WeaponConfig : ScriptableObject
{
    [Header("Base")]
    public float FireRate = 2.2f;

    [Header("Projectile")]
    public ProjectileConfig Projectile;
}