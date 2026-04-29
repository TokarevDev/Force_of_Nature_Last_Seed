using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Critical Chance")]
public sealed class CriticalChanceRewardEffect : RewardEffect
{
    [SerializeField][Range(0f, 1f)] private float _chanceBonus = 0.1f;
    [SerializeField][Min(1f)] private float _criticalDamageMultiplier = 2f;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanApplyCriticalChance(_chanceBonus);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        state.AddCriticalChance(_chanceBonus, _criticalDamageMultiplier);
    }
}
