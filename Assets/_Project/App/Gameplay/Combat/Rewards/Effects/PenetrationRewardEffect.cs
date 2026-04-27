using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Penetration")]
public sealed class PenetrationRewardEffect : RewardEffect
{
    [SerializeField][Min(1)] private int _bonusPenetration = 1;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanAddPenetration;
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddPenetration(_bonusPenetration);
    }
}
