using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Salvo")]
public sealed class SalvoFireRewardEffect : RewardEffect
{
    [SerializeField][Min(1)] private int _extraShots = 1;
    [SerializeField][Min(0.01f)] private float _shotInterval = 0.2f;
    [SerializeField] private bool _extendsSalvoLimit;
    [SerializeField][Min(1)] private int _maxSalvoShotsAfterApply = WeaponRuntimeState.DefaultMaxSalvoShots;

    public override bool CanApply(WeaponRuntimeState state)
    {
        if (state == null)
            return false;

        return _extendsSalvoLimit
            ? state.CanApplySalvoShots(_extraShots, GetMaxSalvoExtraShotsAfterApply())
            : state.CanApplySalvoShots(_extraShots);
    }

    public override void Apply(WeaponRuntimeState state)
    {
        if (state == null)
            return;

        if (_extendsSalvoLimit)
            state.ExpandSalvoExtraShotLimit(GetMaxSalvoExtraShotsAfterApply());

        state.AddSalvoShots(_extraShots, _shotInterval);
    }

    private int GetMaxSalvoExtraShotsAfterApply()
    {
        return Mathf.Max(0, _maxSalvoShotsAfterApply - 1);
    }
}
