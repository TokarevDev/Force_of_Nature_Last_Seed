using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Projectile Speed")]
public sealed class ProjectileSpeedRewardEffect : RewardEffect
{
    [SerializeField][Min(0f)] private float _bonus = 0.5f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanApplyProjectileSpeedBonus(_bonus);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddProjectileSpeedBonus(_bonus);
    }
}
