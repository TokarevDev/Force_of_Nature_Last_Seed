using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Burst Fire")]
public sealed class BurstFireRewardEffect : RewardEffect
{
    [SerializeField][Min(1)] private int _extraShots = 1;
    [SerializeField][Min(0.01f)] private float _shotInterval = 0.25f;

    public override void Apply(WeaponRuntimeState state)
    {
        state.AddBurstShots(_extraShots, _shotInterval);
    }
}
