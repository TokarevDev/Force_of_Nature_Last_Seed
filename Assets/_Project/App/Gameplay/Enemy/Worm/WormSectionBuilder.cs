using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds fixed-size sections and handles cocoon + reward generation.
/// Section is the single source of truth for gameplay.
/// </summary>
public static class WormSectionBuilder
{
    private const int SECTION_SIZE = 7;

    public static List<WormSection> BuildSections(List<WormSegment> segments)
    {
        List<WormSection> sections = new();
        List<WormSegment> buffer = new();

        int sectionIndex = 0;
        int sectionsWithoutCocoon = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            buffer.Add(seg);

            if (buffer.Count == SECTION_SIZE)
            {
                CreateSection(buffer, sections, sectionIndex, ref sectionsWithoutCocoon);
                buffer.Clear();
                sectionIndex++;
            }
        }

        if (buffer.Count > 0)
        {
            CreateSection(buffer, sections, sectionIndex, ref sectionsWithoutCocoon);
        }

        return sections;
    }

    private static void CreateSection(
        List<WormSegment> buffer,
        List<WormSection> sections,
        int sectionIndex,
        ref int sectionsWithoutCocoon)
    {
        WormSection section = new();

        for (int i = 0; i < buffer.Count; i++)
        {
            section.AddSegment(buffer[i]);
        }

        TryPlaceCocoon(buffer, section, sectionIndex, ref sectionsWithoutCocoon);

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
        ref int sectionsWithoutCocoon)
    {
        if (buffer.Count == 0)
            return;

        int centerIndex = buffer.Count / 2;
        WormSegment centerSegment = buffer[centerIndex];

        bool spawnCocoon;

        if (sectionIndex < 2)
        {
            spawnCocoon = true;
        }
        else
        {
            spawnCocoon =
                sectionsWithoutCocoon >= 2 ||
                (sectionsWithoutCocoon >= 1 && Random.value < 0.35f);
        }

        if (!spawnCocoon)
        {
            sectionsWithoutCocoon++;
            return;
        }

        sectionsWithoutCocoon = 0;

        centerSegment.EnableCocoon();

        section.SetCocoon(true);
    }
}