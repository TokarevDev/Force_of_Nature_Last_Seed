using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Damage")]
public sealed class DamageRewardEffect : RewardEffect
{
    [SerializeField] private float _multiplier = 1.3f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanApplyDamageMultiplier(_multiplier);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.ApplyDamageMultiplier(_multiplier);
    }
}
