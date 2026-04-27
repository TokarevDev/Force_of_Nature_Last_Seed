using System.Collections.Generic;

/// <summary>
/// Runtime state of weapon that accumulates all modifier effects.
/// This state is reset per run/session.
/// </summary>
public sealed class WeaponRuntimeState
{
    private readonly List<ShotModifierData> _shotModifiers = new();

    public float DamageMultiplier { get; private set; } = 1f;
    public float FireRateBonus { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamageMultiplier { get; private set; } = 2f;
    public int PenetrationBonus { get; private set; }
    public int BurstExtraShots { get; private set; }
    public float BurstInterval { get; private set; } = 0.25f;
    public IReadOnlyList<ShotModifierData> ShotModifiers => _shotModifiers;

    public void ApplyDamageMultiplier(float multiplier)
    {
        DamageMultiplier *= multiplier;
    }

    public void AddFireRateBonus(float bonus)
    {
        FireRateBonus += bonus;
    }

    public void AddCriticalChance(float chanceBonus, float criticalDamageMultiplier)
    {
        CriticalChance = UnityEngine.Mathf.Clamp01(CriticalChance + chanceBonus);
        CriticalDamageMultiplier = UnityEngine.Mathf.Max(
            CriticalDamageMultiplier,
            criticalDamageMultiplier
        );
    }

    public void AddPenetration(int bonus)
    {
        PenetrationBonus += UnityEngine.Mathf.Max(0, bonus);
    }

    public void AddBurstShots(int extraShots, float interval)
    {
        BurstExtraShots += UnityEngine.Mathf.Max(0, extraShots);
        BurstInterval = UnityEngine.Mathf.Max(0.01f, interval);
    }

    public bool AddShotModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        _shotModifiers.Add(modifier);
        return true;
    }
}
