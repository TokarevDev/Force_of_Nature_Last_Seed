using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Acacia Thorn Weapon Config")]
public sealed class AcaciaThornWeaponConfig : ScriptableObject
{
    [Header("Base")]
    public AcaciaThornProjectile ProjectilePrefab;
    public Sprite Sprite;
    [Min(1)] public int Damage = 6;
    [Min(0.05f)] public float Cooldown = 3f;

    [Header("Fire Rate Limits")]
    [Min(0.05f)] public float MinCooldown = 0.75f;
    [Min(0f)] public float MaxFireRateBonus = AcaciaThornRuntimeState.DefaultMaxFireRateBonus;
    [Min(0f)] public float FireRateBonusEffectiveness = 0.1f;

    [Header("Projectile")]
    [Min(0.1f)] public float Speed = 10f;
    [Min(0.05f)] public float LifeTime = 4f;
    public float RotateSprite;
    [Min(0.01f)] public float SpriteScale = 1f;
    [Min(0f)] public float SpawnOffset = 0.3f;
    [Min(1)] public int BaseSplitCount = 2;
    [Min(0)] public int BounceCount = 2;
    [Min(1)] public int PrewarmCount = 48;

    [Header("Progression Limits")]
    [Min(1f)] public float MaxDamageMultiplier = AcaciaThornRuntimeState.MaxDamageMultiplier;
    [Range(0, AcaciaThornRuntimeState.MaxExtraSplitProjectiles)]
    public int MaxExtraSplitProjectiles = 4;

    [Header("Power Estimate")]
    [Range(0f, 1f)] public float EstimatedSplitHitChance = 0.55f;
    [Range(0f, 1f)] public float EstimatedBounceHitChance = 0.25f;
}
