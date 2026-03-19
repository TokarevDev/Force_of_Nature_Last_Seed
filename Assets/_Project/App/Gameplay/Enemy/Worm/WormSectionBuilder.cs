using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds fixed-size sections with deterministic center placement.
///
/// Rules:
/// - every section has a constant size when possible
/// - HP is always anchored to the center segment
/// - if a cocoon exists inside a section, it is normalized to the center
/// - only one cocoon is allowed per section after normalization
/// </summary>
public static class WormSectionBuilder
{
    private const int SECTION_SIZE = 7;
    private const int CENTER_INDEX = SECTION_SIZE / 2;

    public static List<WormSection> BuildSectionsByCocoons(List<WormSegment> segments)
    {
        List<WormSection> sections = new();
        List<WormSegment> buffer = new();

        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            buffer.Add(seg);

            if (buffer.Count == SECTION_SIZE)
            {
                CreateSection(buffer, sections);
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            CreateSection(buffer, sections);
        }

        return sections;
    }

    private static void CreateSection(List<WormSegment> buffer, List<WormSection> sections)
    {
        WormSection section = new();

        for (int i = 0; i < buffer.Count; i++)
        {
            section.AddSegment(buffer[i]);
        }

        NormalizeCocoonPlacement(buffer);

        sections.Add(section);
    }

    /// <summary>
    /// Ensures that a section contains at most one cocoon
    /// and that this cocoon is always placed on the center segment.
    /// </summary>
    private static void NormalizeCocoonPlacement(List<WormSegment> buffer)
    {
        if (buffer.Count == 0)
            return;

        int centerIndex = buffer.Count / 2;
        WormSegment centerSegment = buffer[centerIndex];

        bool hasAnyCocoon = false;
        bool hasReward = false;

        for (int i = 0; i < buffer.Count; i++)
        {
            WormSegment seg = buffer[i];

            if (!seg.HasCocoon)
                continue;

            hasAnyCocoon = true;

            if (seg.HasReward)
                hasReward = true;

            seg.DisableCocoon();
            seg.SetHasReward(false);
        }

        if (!hasAnyCocoon)
            return;

        centerSegment.EnableCocoon();
        centerSegment.SetHasReward(hasReward);
    }
}