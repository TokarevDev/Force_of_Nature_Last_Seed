using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Critical Power")]
public sealed class CriticalPowerRewardEffect : RewardEffect
{
    [SerializeField][Min(0f)] private float _criticalDamageBonus = 0.5f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null
            && state.CriticalChance > 0f
            && state.CanAddCriticalDamage;
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddCriticalDamageBonus(_criticalDamageBonus);
    }
}
