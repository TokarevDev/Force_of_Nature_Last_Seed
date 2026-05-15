using System.Collections.Generic;
using UnityEngine;

public static class WormCocoonRules
{
    public const int SectionSize = 7;

    private const int FirstCocoonSectionIndex = 1;
    private const int EarlyEmptySectionsBetweenCocoons = 2;
    private const int LateEmptySectionsBetweenCocoons = 3;
    private const float LateProgressStart = 0.5f;
    private const float LateWhiteWeightMultiplier = 0.25f;
    private const float LateGreenWeightMultiplier = 1.25f;
    private const float LateBlueWeightMultiplier = 1.6f;
    private const float LateLegendaryWeightMultiplier = 0.45f;

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
        IReadOnlyList<CocoonRewardProfile> profiles = HasSpawnableProfile(cocoonProfiles)
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

            totalWeight += GetEffectiveSpawnWeight(profile, sectionProgress);
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

            current += GetEffectiveSpawnWeight(profile, sectionProgress);

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
        IReadOnlyList<CocoonRewardProfile> profiles)
    {
        if (profiles == null)
            return false;

        for (int i = 0; i < profiles.Count; i++)
        {
            if (IsSpawnableProfile(profiles[i]))
                return true;
        }

        return false;
    }

    private static bool IsSpawnableProfile(CocoonRewardProfile profile)
    {
        return profile != null && profile.SpawnWeight > 0f;
    }

    private static bool IsSpawnableProfile(
        CocoonRewardProfile profile,
        float sectionProgress)
    {
        return IsSpawnableProfile(profile)
            && sectionProgress + Mathf.Epsilon >= profile.MinDestroyedProgressToSpawn;
    }

    private static float GetEffectiveSpawnWeight(
        CocoonRewardProfile profile,
        float sectionProgress)
    {
        float baseWeight = profile.SpawnWeight;

        if (sectionProgress < LateProgressStart)
            return baseWeight;

        float t = Mathf.InverseLerp(
            LateProgressStart,
            1f,
            sectionProgress);
        float quality = GetRewardQuality(profile);
        float lateMultiplier;

        if (quality < 0.25f)
            lateMultiplier = Mathf.Lerp(1f, LateWhiteWeightMultiplier, t);
        else if (quality < 0.75f)
            lateMultiplier = Mathf.Lerp(1f, LateGreenWeightMultiplier, t);
        else if (quality < 1.5f)
            lateMultiplier = Mathf.Lerp(1f, LateBlueWeightMultiplier, t);
        else
            lateMultiplier = Mathf.Lerp(1f, LateLegendaryWeightMultiplier, t);

        return baseWeight * lateMultiplier;
    }

    private static float GetRewardQuality(CocoonRewardProfile profile)
    {
        IReadOnlyList<RewardRaritySlot> slots = profile.RaritySlots;

        if (slots == null || slots.Count == 0)
            return 0f;

        float total = 0f;
        int slotCount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            RewardRaritySlot slot = slots[i];

            if (slot == null)
                continue;

            float alternateChance = slot.AlternateChance;
            total += GetRarityScore(slot.Rarity) * (1f - alternateChance);
            total += GetRarityScore(slot.AlternateRarity) * alternateChance;
            slotCount++;
        }

        return slotCount > 0 ? total / slotCount : 0f;
    }

    private static float GetRarityScore(RewardRarity rarity)
    {
        return rarity switch
        {
            RewardRarity.Rare => 1f,
            RewardRarity.Legendary => 2f,
            _ => 0f
        };
    }
}
