using System.Collections.Generic;

/// <summary>
/// Runtime state of weapon that accumulates all modifier effects.
/// This state is reset per run/session.
/// </summary>
public sealed class WeaponRuntimeState
{
    public float FireRateMultiplier = 1f;

    public readonly List<ShotModifierData> ShotModifiers = new();
}