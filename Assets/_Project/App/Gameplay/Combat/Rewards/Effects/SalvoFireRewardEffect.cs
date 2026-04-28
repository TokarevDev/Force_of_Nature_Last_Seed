using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Salvo")]
public sealed class SalvoFireRewardEffect : RewardEffect
{
    [SerializeField][Min(1)] private int _extraShots = 1;
    [SerializeField][Min(0.01f)] private float _shotInterval = 0.2f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanAddSalvoShots;
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddSalvoShots(_extraShots, _shotInterval);
    }
}
