using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Add Modifier")]
public sealed class AddModifierRewardEffect : RewardEffect
{
    [SerializeField] private ShotModifierData _modifier;
    [SerializeField] private bool _extendsParallelLimit;
    [SerializeField][Min(1)] private int _maxParallelProjectilesAfterApply = WeaponRuntimeState.DefaultMaxParallelProjectiles;

    public override bool CanApply(WeaponRuntimeState state)
    {
        if (state == null)
            return false;

        if (_extendsParallelLimit && TryGetParallelBonus(out int bonusProjectiles))
        {
            return state.CanApplyParallelProjectiles(
                bonusProjectiles,
                _maxParallelProjectilesAfterApply);
        }

        return state.CanAddShotModifier(_modifier);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        if (_extendsParallelLimit && _modifier is ParallelModifierData)
            state.ExpandParallelProjectileLimit(_maxParallelProjectilesAfterApply);

        if (!state.AddShotModifier(_modifier))
        {
            Debug.LogWarning("Modifier is null in AddModifierRewardEffect");
        }
    }

    private bool TryGetParallelBonus(out int bonusProjectiles)
    {
        bonusProjectiles = 0;

        ParallelModifierData parallel = _modifier as ParallelModifierData;

        if (parallel == null)
            return false;

        bonusProjectiles = Mathf.Max(0, parallel.Count - 1);

        return bonusProjectiles > 0;
    }
}
