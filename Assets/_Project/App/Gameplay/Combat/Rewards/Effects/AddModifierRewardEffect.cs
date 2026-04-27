using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Add Modifier")]
public sealed class AddModifierRewardEffect : RewardEffect
{
    [SerializeField] private ShotModifierData _modifier;

    public override bool CanApply(WeaponRuntimeState state)
    {
        return state != null && state.CanAddShotModifier(_modifier);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        if (!state.AddShotModifier(_modifier))
        {
            Debug.LogWarning("Modifier is null in AddModifierRewardEffect");
        }
    }
}
