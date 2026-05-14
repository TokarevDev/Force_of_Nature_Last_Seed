using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ImmediateRewardedAdService : RewardedAdService
{
    [SerializeField] private bool _grantReward = true;
    [SerializeField] private bool _allowInPlayerBuilds = true;

    public override bool IsReady => _grantReward && IsAllowedInCurrentBuild();

    private void Awake()
    {
        if (!IsEditorOrDevelopmentBuild() && _allowInPlayerBuilds)
        {
            Debug.LogWarning(
                "ImmediateRewardedAdService is granting rewards in a player build. " +
                "Replace it with a production rewarded ad service before release.",
                this);
        }
    }

    public override void ShowRewardedAd(Action<bool> onCompleted)
    {
        onCompleted?.Invoke(IsReady);
    }

    private bool IsAllowedInCurrentBuild()
    {
        return IsEditorOrDevelopmentBuild() || _allowInPlayerBuilds;
    }

    private static bool IsEditorOrDevelopmentBuild()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return false;
#endif
    }
}
