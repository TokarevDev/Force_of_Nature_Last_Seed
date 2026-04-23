using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Add Modifier")]
public sealed class AddModifierRewardEffect : RewardEffect
{
    [SerializeField] private ShotModifierData _modifier;

    public override void Apply(WeaponRuntimeState state)
    {
        if (_modifier == null)
        {
            Debug.LogWarning("Modifier is null in AddModifierRewardEffect");
            return;
        }

        state.ShotModifiers.Add(_modifier);
    }
}