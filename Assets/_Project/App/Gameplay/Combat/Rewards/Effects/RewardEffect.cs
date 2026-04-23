using UnityEngine;

/// <summary>
/// Base class for all reward effects.
/// Contains gameplay logic only (no UI, no state mutation outside RuntimeState).
/// </summary>
public abstract class RewardEffect : ScriptableObject
{
    public abstract void Apply(WeaponRuntimeState state);
}