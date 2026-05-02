using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds fixed-size sections and handles cocoon + reward generation.
/// Section is the single source of truth for gameplay.
/// </summary>
public static class WormSectionBuilder
{
    private const int SECTION_SIZE = 7;
    private const int FirstCocoonSectionIndex = 1;
    private const int EarlyEmptySectionsBetweenCocoons = 2;
    private const int LateEmptySectionsBetweenCocoons = 3;
    private const float LateProgressStart = 0.5f;
    private const float LateWhiteWeightMultiplier = 0.25f;
    private const float LateGreenWeightMultiplier = 1.25f;
    private const float LateBlueWeightMultiplier = 1.6f;
    private const float LateLegendaryWeightMultiplier = 2.3f;

    public static List<WormSection> BuildSections(
        List<WormSegment> segments,
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles = null)
    {
        List<WormSection> sections = new();
        List<WormSegment> buffer = new();

        int sectionIndex = 0;
        int sectionsWithoutCocoon = 0;
        int totalSections = CountGameplaySections(segments);

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            buffer.Add(seg);

            if (buffer.Count == SECTION_SIZE)
            {
                CreateSection(
                    buffer,
                    sections,
                    sectionIndex,
                    totalSections,
                    cocoonProfiles,
                    ref sectionsWithoutCocoon);

                buffer.Clear();
                sectionIndex++;
            }
        }

        if (buffer.Count > 0)
        {
            CreateSection(
                buffer,
                sections,
                sectionIndex,
                totalSections,
                cocoonProfiles,
                ref sectionsWithoutCocoon);
        }

        return sections;
    }

    private static void CreateSection(
        List<WormSegment> buffer,
        List<WormSection> sections,
        int sectionIndex,
        int totalSections,
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        ref int sectionsWithoutCocoon)
    {
        WormSection section = new();

        for (int i = 0; i < buffer.Count; i++)
        {
            section.AddSegment(buffer[i]);
        }

        TryPlaceCocoon(
            buffer,
            section,
            sectionIndex,
            totalSections,
            cocoonProfiles,
            ref sectionsWithoutCocoon);

        sections.Add(section);
    }

    /// <summary>
    /// Determines if this section should have a cocoon.
    /// If yes — places it in center and assigns reward to the section.
    /// </summary>
    private static void TryPlaceCocoon(
        List<WormSegment> buffer,
        WormSection section,
        int sectionIndex,
        int totalSections,
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        ref int sectionsWithoutCocoon)
    {
        if (buffer.Count == 0)
            return;

        int centerIndex = buffer.Count / 2;
        WormSegment centerSegment = buffer[centerIndex];
        float sectionProgress = GetSectionProgress(sectionIndex, totalSections);
        bool spawnCocoon = ShouldPlaceCocoon(
            sectionIndex,
            totalSections,
            sectionProgress,
            sectionsWithoutCocoon);

        if (!spawnCocoon)
        {
            sectionsWithoutCocoon++;
            return;
        }

        sectionsWithoutCocoon = 0;

        CocoonRewardProfile profile = RollCocoonProfile(
            cocoonProfiles,
            sectionProgress);
        centerSegment.EnableCocoon(profile.VisualColor);

        section.SetCocoon(profile);
    }

    private static bool ShouldPlaceCocoon(
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

    private static CocoonRewardProfile RollCocoonProfile(
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        float sectionProgress)
    {
        IReadOnlyList<CocoonRewardProfile> profiles = HasSpawnableProfile(cocoonProfiles)
            ? cocoonProfiles
            : CocoonRewardProfile.Defaults;

        float totalWeight = 0f;

        for (int i = 0; i < profiles.Count; i++)
        {
            CocoonRewardProfile profile = profiles[i];

            if (!IsSpawnableProfile(profile, sectionProgress))
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

            current += GetEffectiveSpawnWeight(profile, sectionProgress);

            if (roll <= current)
                return profile;
        }

        return CocoonRewardProfile.Default;
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

    private static int CountGameplaySections(List<WormSegment> segments)
    {
        if (segments == null || segments.Count == 0)
            return 0;

        int gameplaySegmentCount = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment segment = segments[i];

            if (segment != null && segment.Type is not (WormSegmentType.Head or WormSegmentType.Tail))
                gameplaySegmentCount++;
        }

        return Mathf.CeilToInt(gameplaySegmentCount / (float)SECTION_SIZE);
    }

    private static float GetSectionProgress(int sectionIndex, int totalSections)
    {
        if (totalSections <= 1)
            return 0f;

        return Mathf.Clamp01(sectionIndex / (float)(totalSections - 1));
    }
}
