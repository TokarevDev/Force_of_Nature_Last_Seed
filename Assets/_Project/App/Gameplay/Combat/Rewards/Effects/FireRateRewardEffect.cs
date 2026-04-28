using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Fire Rate")]
public sealed class FireRateRewardEffect : RewardEffect
{
    [SerializeField] private float _bonus = 0.1f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanAddFireRateBonus;
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddFireRateBonus(_bonus);
    }
}
