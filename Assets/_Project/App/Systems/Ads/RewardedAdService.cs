using System;
using UnityEngine;

public abstract class RewardedAdService : MonoBehaviour
{
    public abstract bool IsReady { get; }

    public abstract void ShowRewardedAd(Action<bool> onCompleted);
}
