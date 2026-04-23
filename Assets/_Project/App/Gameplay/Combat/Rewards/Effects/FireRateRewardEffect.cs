using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Fire Rate")]
public sealed class FireRateRewardEffect : RewardEffect
{
    [SerializeField] private float _bonus = 0.1f;

    public override void Apply(WeaponRuntimeState state)
    {
        state.FireRateBonus += _bonus;

        Debug.Log($"Fire rate applied. Bonus = {state.FireRateBonus}");
    }
}