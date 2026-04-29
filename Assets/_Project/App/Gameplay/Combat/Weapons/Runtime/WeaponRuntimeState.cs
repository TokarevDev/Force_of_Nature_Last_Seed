using System;
using System.Collections.Generic;

/// <summary>
/// Runtime state of weapon that accumulates all modifier effects.
/// This state is reset per run/session.
/// </summary>
public sealed class WeaponRuntimeState
{
    private const float FloatEpsilon = 0.0001f;

    public const int MaxParallelProjectiles = 10;
    public const int MaxSalvoShots = 5;
    public const int MaxSalvoExtraShots = MaxSalvoShots - 1;
    public const int MaxProjectileDamage = 9999999;
    public const float DefaultMaxFireRateBonus = 2f;
    public const float MaxDamageMultiplier = 80f;
    public const float MaxCriticalDamageMultiplier = 10f;
    public const int MaxPenetrationBonus = 5;
    public const float MaxCriticalChance = 1f;

    private readonly List<ShotModifierData> _shotModifiers = new();

    private float _maxDamageMultiplier = MaxDamageMultiplier;
    private float _maxCriticalDamageMultiplier = MaxCriticalDamageMultiplier;
    private int _maxPenetrationBonus = MaxPenetrationBonus;
    private float _maxCriticalChance = MaxCriticalChance;
    private int _maxParallelProjectiles = MaxParallelProjectiles;
    private int _maxSalvoExtraShots = MaxSalvoExtraShots;

    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateBonus { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamageMultiplier { get; private set; } = 2f;
    public int PenetrationBonus { get; private set; }
    public int ParallelProjectileCount { get; private set; } = 1;
    public float ParallelSpacing { get; private set; } = 0.5f;
    public int SalvoExtraShots { get; private set; }
    public float SalvoInterval { get; private set; } = 0.2f;
    public float MaxFireRateBonus { get; private set; } = DefaultMaxFireRateBonus;
    public IReadOnlyList<ShotModifierData> ShotModifiers => _shotModifiers;

    public bool CanAddDamageMultiplier => DamageMultiplier < _maxDamageMultiplier;
    public bool CanAddFireRateBonus => FireRateBonus < MaxFireRateBonus;
    public bool CanAddCriticalChance => CriticalChance < _maxCriticalChance;
    public bool CanAddCriticalDamage => CriticalDamageMultiplier < _maxCriticalDamageMultiplier;
    public bool CanAddPenetration => PenetrationBonus < _maxPenetrationBonus;
    public bool CanAddParallelProjectiles => ParallelProjectileCount < _maxParallelProjectiles;
    public bool CanAddSalvoShots => SalvoExtraShots < _maxSalvoExtraShots;

    public bool CanApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f)
            return false;

        return DamageMultiplier * multiplier <= _maxDamageMultiplier + FloatEpsilon;
    }

    public bool CanApplyFireRateBonus(float bonus)
    {
        if (bonus <= 0f)
            return false;

        return FireRateBonus + bonus <= MaxFireRateBonus + FloatEpsilon;
    }

    public bool CanApplyCriticalChance(float chanceBonus)
    {
        if (chanceBonus <= 0f)
            return false;

        return CriticalChance + chanceBonus <= _maxCriticalChance + FloatEpsilon;
    }

    public bool CanApplyCriticalDamageBonus(float damageBonus)
    {
        if (damageBonus <= 0f)
            return false;

        return CriticalDamageMultiplier + damageBonus <= _maxCriticalDamageMultiplier + FloatEpsilon;
    }

    public bool CanApplyPenetrationBonus(int bonus)
    {
        if (bonus <= 0)
            return false;

        return PenetrationBonus + bonus <= _maxPenetrationBonus;
    }

    public bool CanApplyParallelProjectiles(int bonusProjectiles)
    {
        if (bonusProjectiles <= 0)
            return false;

        return ParallelProjectileCount + bonusProjectiles <= _maxParallelProjectiles;
    }

    public bool CanApplySalvoShots(int extraShots)
    {
        if (extraShots <= 0)
            return false;

        return SalvoExtraShots + extraShots <= _maxSalvoExtraShots;
    }

    public float ApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f)
            return 0f;

        float previousMultiplier = DamageMultiplier;
        DamageMultiplier = UnityEngine.Mathf.Min(
            DamageMultiplier * multiplier,
            _maxDamageMultiplier);

        return DamageMultiplier - previousMultiplier;
    }

    public void SetProgressionLimits(
        float maxDamageMultiplier,
        float maxCriticalChance,
        float maxCriticalDamageMultiplier,
        int maxPenetrationBonus,
        int maxParallelProjectiles,
        int maxSalvoExtraShots)
    {
        _maxDamageMultiplier = UnityEngine.Mathf.Clamp(
            maxDamageMultiplier,
            1f,
            MaxDamageMultiplier);

        _maxCriticalChance = UnityEngine.Mathf.Clamp(
            maxCriticalChance,
            0f,
            MaxCriticalChance);

        _maxCriticalDamageMultiplier = UnityEngine.Mathf.Clamp(
            maxCriticalDamageMultiplier,
            1f,
            MaxCriticalDamageMultiplier);

        _maxPenetrationBonus = UnityEngine.Mathf.Clamp(
            maxPenetrationBonus,
            0,
            MaxPenetrationBonus);

        _maxParallelProjectiles = UnityEngine.Mathf.Clamp(
            maxParallelProjectiles,
            1,
            MaxParallelProjectiles);

        _maxSalvoExtraShots = UnityEngine.Mathf.Clamp(
            maxSalvoExtraShots,
            0,
            MaxSalvoExtraShots);

        DamageMultiplier = UnityEngine.Mathf.Min(DamageMultiplier, _maxDamageMultiplier);
        CriticalChance = UnityEngine.Mathf.Min(CriticalChance, _maxCriticalChance);
        CriticalDamageMultiplier = UnityEngine.Mathf.Min(
            CriticalDamageMultiplier,
            _maxCriticalDamageMultiplier);

        PenetrationBonus = UnityEngine.Mathf.Min(PenetrationBonus, _maxPenetrationBonus);
        ParallelProjectileCount = UnityEngine.Mathf.Min(
            ParallelProjectileCount,
            _maxParallelProjectiles);

        SalvoExtraShots = UnityEngine.Mathf.Min(SalvoExtraShots, _maxSalvoExtraShots);
    }

    public void SetFireRateBonusLimit(float maxFireRateBonus)
    {
        MaxFireRateBonus = UnityEngine.Mathf.Max(0f, maxFireRateBonus);
        FireRateBonus = UnityEngine.Mathf.Min(FireRateBonus, MaxFireRateBonus);
    }

    public float AddFireRateBonus(float bonus)
    {
        float accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0f, bonus),
            MaxFireRateBonus - FireRateBonus);

        FireRateBonus += UnityEngine.Mathf.Max(0f, accepted);
        return accepted;
    }

    public float AddCriticalChance(float chanceBonus, float minimumCriticalDamageMultiplier)
    {
        float accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0f, chanceBonus),
            _maxCriticalChance - CriticalChance);

        CriticalChance = UnityEngine.Mathf.Clamp(
            CriticalChance + UnityEngine.Mathf.Max(0f, accepted),
            0f,
            _maxCriticalChance);

        CriticalDamageMultiplier = UnityEngine.Mathf.Clamp(
            UnityEngine.Mathf.Max(CriticalDamageMultiplier, minimumCriticalDamageMultiplier),
            1f,
            _maxCriticalDamageMultiplier);

        return accepted;
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        float accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0f, damageBonus),
            _maxCriticalDamageMultiplier - CriticalDamageMultiplier);

        CriticalDamageMultiplier += UnityEngine.Mathf.Max(0f, accepted);
        return accepted;
    }

    public int AddPenetration(int bonus)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, bonus),
            _maxPenetrationBonus - PenetrationBonus);

        PenetrationBonus += UnityEngine.Mathf.Max(0, accepted);
        return accepted;
    }

    public int AddSalvoShots(int extraShots, float interval)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, extraShots),
            _maxSalvoExtraShots - SalvoExtraShots);

        SalvoExtraShots += UnityEngine.Mathf.Max(0, accepted);

        if (accepted > 0)
            SalvoInterval = UnityEngine.Mathf.Max(0.01f, interval);

        return accepted;
    }

    public bool AddShotModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        if (modifier is ParallelModifierData parallel)
        {
            int bonusProjectiles = UnityEngine.Mathf.Max(0, parallel.Count - 1);
            return AddParallelProjectiles(bonusProjectiles, parallel.Spacing) > 0;
        }

        _shotModifiers.Add(modifier);
        return true;
    }

    public bool CanAddShotModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        if (modifier is ParallelModifierData parallel)
        {
            int bonusProjectiles = UnityEngine.Mathf.Max(0, parallel.Count - 1);
            return CanApplyParallelProjectiles(bonusProjectiles);
        }

        return true;
    }

    public int AddParallelProjectiles(int bonusProjectiles, float spacing)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, bonusProjectiles),
            _maxParallelProjectiles - ParallelProjectileCount);

        ParallelProjectileCount += UnityEngine.Mathf.Max(0, accepted);

        if (accepted > 0)
            ParallelSpacing = UnityEngine.Mathf.Max(0.1f, spacing);

        return accepted;
    }

    public static int ClampDamage(double rawDamage)
    {
        if (double.IsNaN(rawDamage) || rawDamage <= 1d)
            return 1;

        if (rawDamage >= MaxProjectileDamage)
            return MaxProjectileDamage;

        return UnityEngine.Mathf.Max(1, (int)Math.Round(rawDamage));
    }
}
