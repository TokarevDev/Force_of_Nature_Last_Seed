using System;
using UnityEngine;

public sealed class AcaciaThornRuntimeState
{
    private const float FloatEpsilon = 0.0001f;

    public const float DefaultMaxFireRateBonus = 3f;
    public const float DefaultMaxProjectileSpeedBonus = 2f;
    public const float MaxDamageMultiplier = 100000f;
    public const int MaxSalvoShots = 6;
    public const int MaxSalvoExtraShots = MaxSalvoShots - 1;
    public const float MaxCriticalChance = 1f;
    public const float MaxCriticalDamageMultiplier = 100f;

    private float _maxDamageMultiplier = MaxDamageMultiplier;
    private int _maxSalvoExtraShots = MaxSalvoExtraShots;
    private float _maxCriticalChance = MaxCriticalChance;
    private float _maxCriticalDamageMultiplier = MaxCriticalDamageMultiplier;

    public bool IsUnlocked { get; private set; }
    public int BaseDamage { get; private set; } = 1;
    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateBonus { get; private set; }
    public int SalvoExtraShots { get; private set; }
    public float ProjectileSpeedBonus { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamageMultiplier { get; private set; } = 2f;
    public float MaxFireRateBonus { get; private set; } = DefaultMaxFireRateBonus;
    public float MaxProjectileSpeedBonus { get; private set; } = DefaultMaxProjectileSpeedBonus;

    public bool CanUnlock => !IsUnlocked;

    public void SetProgressionLimits(
        float maxDamageMultiplier,
        float maxFireRateBonus,
        int maxSalvoExtraShots,
        float maxProjectileSpeedBonus,
        float maxCriticalChance,
        float criticalDamageMultiplier,
        float maxCriticalDamageMultiplier)
    {
        _maxDamageMultiplier = Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier);

        MaxFireRateBonus = Mathf.Clamp(
            maxFireRateBonus,
            0f,
            DefaultMaxFireRateBonus);

        _maxSalvoExtraShots = Mathf.Clamp(
            maxSalvoExtraShots,
            0,
            MaxSalvoExtraShots);

        MaxProjectileSpeedBonus = Mathf.Clamp(
            maxProjectileSpeedBonus,
            0f,
            DefaultMaxProjectileSpeedBonus);

        _maxCriticalChance = Mathf.Clamp(
            maxCriticalChance,
            0f,
            MaxCriticalChance);

        _maxCriticalDamageMultiplier = Mathf.Clamp(
            maxCriticalDamageMultiplier,
            1f,
            MaxCriticalDamageMultiplier);

        CriticalDamageMultiplier = Mathf.Clamp(
            Mathf.Max(1f, criticalDamageMultiplier),
            1f,
            _maxCriticalDamageMultiplier);

        DamageMultiplier = Mathf.Min(DamageMultiplier, _maxDamageMultiplier);
        FireRateBonus = Mathf.Min(FireRateBonus, MaxFireRateBonus);
        SalvoExtraShots = Mathf.Min(SalvoExtraShots, _maxSalvoExtraShots);
        ProjectileSpeedBonus = Mathf.Min(ProjectileSpeedBonus, MaxProjectileSpeedBonus);
        CriticalChance = Mathf.Min(CriticalChance, _maxCriticalChance);
        CriticalDamageMultiplier = Mathf.Min(CriticalDamageMultiplier, _maxCriticalDamageMultiplier);
    }

    public bool CanApplyDamageMultiplier(float multiplier)
    {
        if (!IsUnlocked || multiplier <= 1f)
            return false;

        return DamageMultiplier * multiplier <= _maxDamageMultiplier + FloatEpsilon;
    }

    public bool CanApplyFireRateBonus(float bonus)
    {
        if (!IsUnlocked || bonus <= 0f)
            return false;

        return FireRateBonus + bonus <= MaxFireRateBonus + FloatEpsilon;
    }

    public bool CanApplySalvoShots(int extraShots)
    {
        if (!IsUnlocked || extraShots <= 0)
            return false;

        return SalvoExtraShots + extraShots <= _maxSalvoExtraShots;
    }

    public bool CanApplyProjectileSpeedBonus(float bonus)
    {
        if (!IsUnlocked || bonus <= 0f)
            return false;

        return ProjectileSpeedBonus + bonus <= MaxProjectileSpeedBonus + FloatEpsilon;
    }

    public bool CanApplyCriticalChance(float chanceBonus)
    {
        if (!IsUnlocked || chanceBonus <= 0f)
            return false;

        return CriticalChance + chanceBonus <= _maxCriticalChance + FloatEpsilon;
    }

    public bool CanApplyCriticalDamageBonus(float damageBonus)
    {
        if (!IsUnlocked || CriticalChance <= 0f || damageBonus <= 0f)
            return false;

        return CriticalDamageMultiplier + damageBonus <= _maxCriticalDamageMultiplier + FloatEpsilon;
    }

    public void Unlock(int baseDamage)
    {
        SetBaseDamage(baseDamage);
        IsUnlocked = true;
    }

    public void SetBaseDamage(int baseDamage)
    {
        BaseDamage = Mathf.Max(BaseDamage, Mathf.Max(1, baseDamage));
    }

    public float ApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f)
            return 0f;

        float previousMultiplier = DamageMultiplier;
        DamageMultiplier = Mathf.Min(
            DamageMultiplier * multiplier,
            _maxDamageMultiplier);

        return DamageMultiplier - previousMultiplier;
    }

    public float AddFireRateBonus(float bonus)
    {
        float accepted = Mathf.Min(
            Mathf.Max(0f, bonus),
            MaxFireRateBonus - FireRateBonus);

        FireRateBonus += Mathf.Max(0f, accepted);
        return accepted;
    }

    public int AddSalvoShots(int extraShots)
    {
        int accepted = Mathf.Min(
            Mathf.Max(0, extraShots),
            _maxSalvoExtraShots - SalvoExtraShots);

        SalvoExtraShots += Mathf.Max(0, accepted);
        return accepted;
    }

    public float AddProjectileSpeedBonus(float bonus)
    {
        float accepted = Mathf.Min(
            Mathf.Max(0f, bonus),
            MaxProjectileSpeedBonus - ProjectileSpeedBonus);

        ProjectileSpeedBonus += Mathf.Max(0f, accepted);
        return accepted;
    }

    public float AddCriticalChance(float chanceBonus)
    {
        float accepted = Mathf.Min(
            Mathf.Max(0f, chanceBonus),
            _maxCriticalChance - CriticalChance);

        CriticalChance = Mathf.Clamp(
            CriticalChance + Mathf.Max(0f, accepted),
            0f,
            _maxCriticalChance);

        return accepted;
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        float accepted = Mathf.Min(
            Mathf.Max(0f, damageBonus),
            _maxCriticalDamageMultiplier - CriticalDamageMultiplier);

        CriticalDamageMultiplier += Mathf.Max(0f, accepted);
        return accepted;
    }

    public static int ClampDamage(double rawDamage)
    {
        if (double.IsNaN(rawDamage) || rawDamage <= 1d)
            return 1;

        if (rawDamage >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        return Mathf.Max(1, (int)Math.Round(rawDamage));
    }
}
