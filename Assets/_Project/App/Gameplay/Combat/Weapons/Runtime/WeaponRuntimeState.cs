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
    public IReadOnlyList<ShotModifierData> ShotModifiers => _shotModifiers;

    public void ApplyDamageMultiplier(float multiplier)
    {
        DamageMultiplier *= multiplier;
    }

    public void AddFireRateBonus(float bonus)
    {
        FireRateBonus += bonus;
    }

    public bool AddShotModifier(ShotModifierData modifier)
    {
        if (modifier == null)
            return false;

        _shotModifiers.Add(modifier);
        return true;
    }
}
