using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Damage")]
public sealed class DamageRewardEffect : RewardEffect
{
    [SerializeField] private float _multiplier = 1.3f;

    public override void Apply(WeaponRuntimeState state)
    {
        state.ApplyDamageMultiplier(_multiplier);
    }
}
