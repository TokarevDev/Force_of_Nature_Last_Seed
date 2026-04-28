using System;
using System.Collections.Generic;

/// <summary>
/// Runtime state of weapon that accumulates all modifier effects.
/// This state is reset per run/session.
/// </summary>
public sealed class WeaponRuntimeState
{
    public const int MaxParallelProjectiles = 10;
    public const int MaxSalvoShots = 5;
    public const int MaxSalvoExtraShots = MaxSalvoShots - 1;
    public const int MaxProjectileDamage = 9999999;
    public const float DefaultMaxFireRateBonus = 2f;
    public const float MaxDamageMultiplier = 80f;
    public const float MaxCriticalDamageMultiplier = 10f;
    public const int MaxPenetrationBonus = 5;
    public const float MaxCriticalChance = 0.8f;

    private readonly List<ShotModifierData> _shotModifiers = new();

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

    public bool CanAddDamageMultiplier => DamageMultiplier < MaxDamageMultiplier;
    public bool CanAddFireRateBonus => FireRateBonus < MaxFireRateBonus;
    public bool CanAddCriticalChance => CriticalChance < MaxCriticalChance;
    public bool CanAddCriticalDamage => CriticalDamageMultiplier < MaxCriticalDamageMultiplier;
    public bool CanAddPenetration => PenetrationBonus < MaxPenetrationBonus;
    public bool CanAddParallelProjectiles => ParallelProjectileCount < MaxParallelProjectiles;
    public bool CanAddSalvoShots => SalvoExtraShots < MaxSalvoExtraShots;

    public float ApplyDamageMultiplier(float multiplier)
    {
        if (multiplier <= 1f)
            return 0f;

        float previousMultiplier = DamageMultiplier;
        DamageMultiplier = UnityEngine.Mathf.Min(
            DamageMultiplier * multiplier,
            MaxDamageMultiplier);

        return DamageMultiplier - previousMultiplier;
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
            MaxCriticalChance - CriticalChance);

        CriticalChance = UnityEngine.Mathf.Clamp(
            CriticalChance + UnityEngine.Mathf.Max(0f, accepted),
            0f,
            MaxCriticalChance);

        CriticalDamageMultiplier = UnityEngine.Mathf.Clamp(
            UnityEngine.Mathf.Max(CriticalDamageMultiplier, minimumCriticalDamageMultiplier),
            1f,
            MaxCriticalDamageMultiplier);

        return accepted;
    }

    public float AddCriticalDamageBonus(float damageBonus)
    {
        float accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0f, damageBonus),
            MaxCriticalDamageMultiplier - CriticalDamageMultiplier);

        CriticalDamageMultiplier += UnityEngine.Mathf.Max(0f, accepted);
        return accepted;
    }

    public int AddPenetration(int bonus)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, bonus),
            MaxPenetrationBonus - PenetrationBonus);

        PenetrationBonus += UnityEngine.Mathf.Max(0, accepted);
        return accepted;
    }

    public int AddSalvoShots(int extraShots, float interval)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, extraShots),
            MaxSalvoExtraShots - SalvoExtraShots);

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

        if (modifier is ParallelModifierData)
            return CanAddParallelProjectiles;

        return true;
    }

    public int AddParallelProjectiles(int bonusProjectiles, float spacing)
    {
        int accepted = UnityEngine.Mathf.Min(
            UnityEngine.Mathf.Max(0, bonusProjectiles),
            MaxParallelProjectiles - ParallelProjectileCount);

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
