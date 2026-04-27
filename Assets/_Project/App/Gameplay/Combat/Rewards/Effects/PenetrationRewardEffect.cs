using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Penetration")]
public sealed class PenetrationRewardEffect : RewardEffect
{
    [SerializeField][Min(1)] private int _bonusPenetration = 1;

    public override void Apply(WeaponRuntimeState state)
    {
        state.AddPenetration(_bonusPenetration);
    }
}
