using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Critical Power")]
public sealed class CriticalPowerRewardEffect : RewardEffect
{
    [SerializeField][Min(0f)] private float _criticalDamageBonus = 0.5f;

    public override void Apply(WeaponRuntimeState state)
    {
        state.AddCriticalDamageBonus(_criticalDamageBonus);
    }
}
