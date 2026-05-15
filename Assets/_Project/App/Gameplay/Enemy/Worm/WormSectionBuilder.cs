using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds fixed-size sections and handles cocoon + reward generation.
/// Section is the single source of truth for gameplay.
/// </summary>
public static class WormSectionBuilder
{
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

            if (buffer.Count == WormCocoonRules.SectionSize)
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
        return WormCocoonRules.ShouldPlaceCocoon(
            sectionIndex,
            totalSections,
            sectionProgress,
            sectionsWithoutCocoon);
    }

    private static CocoonRewardProfile RollCocoonProfile(
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        float sectionProgress)
    {
        return WormCocoonRules.RollCocoonProfile(
            cocoonProfiles,
            sectionProgress);
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

        return WormCocoonRules.CountGameplaySections(gameplaySegmentCount);
    }

    private static float GetSectionProgress(int sectionIndex, int totalSections)
    {
        return WormCocoonRules.GetSectionProgress(sectionIndex, totalSections);
    }
}
