using DG.Tweening;
using UnityEngine;

public static class DOTweenBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        DOTween.SetTweensCapacity(500, 100);
    }
}