using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ImmediateRewardedAdService : RewardedAdService
{
    [SerializeField] private bool _grantReward = true;

    public override bool IsReady
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    public override void ShowRewardedAd(Action<bool> onCompleted)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        onCompleted?.Invoke(_grantReward);
#else
        Debug.LogError("ImmediateRewardedAdService is for editor/development testing only. Assign a production rewarded ad service.");
        onCompleted?.Invoke(false);
#endif
    }
}
