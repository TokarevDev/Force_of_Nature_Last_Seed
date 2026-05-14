using UnityEngine;

public static class RuntimePerformanceBootstrap
{
    private const int MinimumMobileTargetFrameRate = 60;
    private const int MediumMobileTargetFrameRate = 90;
    private const int MaximumMobileTargetFrameRate = 120;

    private const float MediumRefreshRateThreshold = 89f;
    private const float HighRefreshRateThreshold = 119f;
    private const int MediumTierMinimumMemoryMb = 3072;
    private const int HighTierMinimumMemoryMb = 4096;
    private const int MediumTierMinimumProcessorCount = 4;
    private const int HighTierMinimumProcessorCount = 6;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyBeforeSceneLoad()
    {
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyAfterSceneLoad()
    {
        Apply();
    }

    private static void Apply()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = ResolveMobileTargetFrameRate();
#endif
    }

    private static int ResolveMobileTargetFrameRate()
    {
        float refreshRate = GetScreenRefreshRate();

        if (refreshRate >= HighRefreshRateThreshold && IsHighTierDevice())
            return MaximumMobileTargetFrameRate;

        if (refreshRate >= MediumRefreshRateThreshold && IsMediumTierDevice())
            return MediumMobileTargetFrameRate;

        return MinimumMobileTargetFrameRate;
    }

    private static float GetScreenRefreshRate()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;

        if (refreshRate <= 0d)
            return MinimumMobileTargetFrameRate;

        return (float)refreshRate;
    }

    private static bool IsHighTierDevice()
    {
        return SystemInfo.systemMemorySize >= HighTierMinimumMemoryMb &&
            SystemInfo.processorCount >= HighTierMinimumProcessorCount;
    }

    private static bool IsMediumTierDevice()
    {
        return SystemInfo.systemMemorySize >= MediumTierMinimumMemoryMb &&
            SystemInfo.processorCount >= MediumTierMinimumProcessorCount;
    }
}
