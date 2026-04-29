using UnityEngine;

/// <summary>
/// Base class for all reward effects.
/// Contains gameplay logic only (no UI, no state mutation outside RuntimeState).
/// </summary>
public abstract class RewardEffect : ScriptableObject
{
    public virtual bool CanApply(RewardRuntimeContext context)
    {
        return context != null && CanApply(context.MainWeaponState);
    }

    public virtual bool CanApply(WeaponRuntimeState state)
    {
        return state != null;
    }

    public virtual void Apply(RewardRuntimeContext context)
    {
        if (context == null)
            return;

        Apply(context.MainWeaponState);
    }

    public abstract void Apply(WeaponRuntimeState state);
}
