using System;
using UnityEngine;

public sealed class AcaciaThornRuntimeState
{
    private const float FloatEpsilon = 0.0001f;

    public const float DefaultMaxFireRateBonus = 100f;
    public const float MaxDamageMultiplier = 100000f;
    public const int MaxExtraSplitProjectiles = 8;

    private float _maxDamageMultiplier = MaxDamageMultiplier;
    private int _maxExtraSplitProjectiles = MaxExtraSplitProjectiles;

    public bool IsUnlocked { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateBonus { get; private set; }
    public int ExtraSplitProjectiles { get; private set; }
    public float MaxFireRateBonus { get; private set; } = DefaultMaxFireRateBonus;

    public bool CanUnlock => !IsUnlocked;

    public void SetProgressionLimits(
        float maxDamageMultiplier,
        float maxFireRateBonus,
        int maxExtraSplitProjectiles)
    {
        _maxDamageMultiplier = Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier);

        MaxFireRateBonus = Mathf.Clamp(
            maxFireRateBonus,
            0f,
            DefaultMaxFireRateBonus);

        _maxExtraSplitProjectiles = Mathf.Clamp(
            maxExtraSplitProjectiles,
            0,
            MaxExtraSplitProjectiles);

        DamageMultiplier = Mathf.Min(DamageMultiplier, _maxDamageMultiplier);
        FireRateBonus = Mathf.Min(FireRateBonus, MaxFireRateBonus);
        ExtraSplitProjectiles = Mathf.Min(
            ExtraSplitProjectiles,
            _maxExtraSplitProjectiles);
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

    public bool CanApplyExtraSplitProjectiles(int extraProjectiles)
    {
        if (!IsUnlocked || extraProjectiles <= 0)
            return false;

        return ExtraSplitProjectiles + extraProjectiles <= _maxExtraSplitProjectiles;
    }

    public void Unlock()
    {
        IsUnlocked = true;
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

    public int AddExtraSplitProjectiles(int extraProjectiles)
    {
        int accepted = Mathf.Min(
            Mathf.Max(0, extraProjectiles),
            _maxExtraSplitProjectiles - ExtraSplitProjectiles);

        ExtraSplitProjectiles += Mathf.Max(0, accepted);
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
