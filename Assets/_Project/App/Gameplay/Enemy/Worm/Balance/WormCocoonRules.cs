using System.Collections.Generic;
using UnityEngine;

public static class WormCocoonRules
{
    public const int SectionSize = 7;

    private const int FirstCocoonSectionIndex = 1;
    private const int EarlyEmptySectionsBetweenCocoons = 1;
    private const int LateEmptySectionsBetweenCocoons = 2;
    private const float LateProgressStart = 0.5f;

    public static int CountGameplaySections(int gameplaySegmentCount)
    {
        return Mathf.CeilToInt(Mathf.Max(0, gameplaySegmentCount) / (float)SectionSize);
    }

    public static float GetSectionProgress(int sectionIndex, int totalSections)
    {
        if (totalSections <= 1)
            return 0f;

        return Mathf.Clamp01(sectionIndex / (float)(totalSections - 1));
    }

    public static bool ShouldPlaceCocoon(
        int sectionIndex,
        int totalSections,
        int sectionsWithoutCocoon)
    {
        return ShouldPlaceCocoon(
            sectionIndex,
            totalSections,
            GetSectionProgress(sectionIndex, totalSections),
            sectionsWithoutCocoon);
    }

    public static bool ShouldPlaceCocoon(
        int sectionIndex,
        int totalSections,
        float sectionProgress,
        int sectionsWithoutCocoon)
    {
        if (sectionIndex <= 0 || sectionIndex >= totalSections - 1)
            return false;

        if (sectionIndex == FirstCocoonSectionIndex)
            return true;

        int requiredEmptySections = sectionProgress < LateProgressStart
            ? EarlyEmptySectionsBetweenCocoons
            : LateEmptySectionsBetweenCocoons;

        return sectionsWithoutCocoon >= requiredEmptySections;
    }

    public static CocoonRewardProfile RollCocoonProfile(
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        float sectionProgress)
    {
        sectionProgress = Mathf.Clamp01(sectionProgress);

        IReadOnlyList<CocoonRewardProfile> profiles = HasSpawnableProfile(cocoonProfiles, sectionProgress)
            ? cocoonProfiles
            : CocoonRewardProfile.Defaults;

        if (TryRollFixedSpawnChanceProfile(
                profiles,
                sectionProgress,
                out CocoonRewardProfile fixedChanceProfile))
        {
            return fixedChanceProfile;
        }

        float totalWeight = 0f;

        for (int i = 0; i < profiles.Count; i++)
        {
            CocoonRewardProfile profile = profiles[i];

            if (!IsSpawnableProfile(profile, sectionProgress))
                continue;

            if (profile.UseFixedSpawnChance)
                continue;

            totalWeight += profile.SpawnWeight;
        }

        if (totalWeight <= 0f)
            return CocoonRewardProfile.Default;

        float roll = Random.value * totalWeight;
        float current = 0f;

        for (int i = 0; i < profiles.Count; i++)
        {
            CocoonRewardProfile profile = profiles[i];

            if (!IsSpawnableProfile(profile, sectionProgress))
                continue;

            if (profile.UseFixedSpawnChance)
                continue;

            current += profile.SpawnWeight;

            if (roll <= current)
                return profile;
        }

        return CocoonRewardProfile.Default;
    }

    private static bool TryRollFixedSpawnChanceProfile(
        IReadOnlyList<CocoonRewardProfile> profiles,
        float sectionProgress,
        out CocoonRewardProfile selected)
    {
        selected = null;

        if (profiles == null)
            return false;

        for (int i = 0; i < profiles.Count; i++)
        {
            CocoonRewardProfile profile = profiles[i];

            if (!IsSpawnableProfile(profile, sectionProgress))
                continue;

            if (!profile.UseFixedSpawnChance)
                continue;

            if (Random.value >= profile.FixedSpawnChance)
                continue;

            selected = profile;
            return true;
        }

        return false;
    }

    private static bool HasSpawnableProfile(
        IReadOnlyList<CocoonRewardProfile> profiles,
        float sectionProgress)
    {
        if (profiles == null)
            return false;

        for (int i = 0; i < profiles.Count; i++)
        {
            if (IsSpawnableProfile(profiles[i], sectionProgress))
                return true;
        }

        return false;
    }

    private static bool IsSpawnableProfile(
        CocoonRewardProfile profile,
        float sectionProgress)
    {
        return profile != null
            && profile.SpawnWeight > 0f
            && sectionProgress + Mathf.Epsilon >= profile.MinDestroyedProgressToSpawn;
    }

}
