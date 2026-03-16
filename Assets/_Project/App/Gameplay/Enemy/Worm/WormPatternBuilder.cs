using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a single segment entry in the generated worm pattern.
/// Determines both the segment type and whether a cocoon overlay is present.
/// </summary>
public readonly struct WormPatternEntry
{
    public readonly WormSegmentType Type;
    public readonly bool HasCocoon;

    public WormPatternEntry(WormSegmentType type, bool hasCocoon)
    {
        Type = type;
        HasCocoon = hasCocoon;
    }
}

/// <summary>
/// Procedurally generates the worm structure.
/// The algorithm ensures the worm always starts with a head,
/// ends with a tail, and occasionally places cocoons on body segments.
/// </summary>
public static class WormPatternBuilder
{
    /// <summary>
    /// Builds a procedural worm layout.
    /// The result is a sequence of segment entries used by the factory.
    /// </summary>
    public static List<WormPatternEntry> BuildPattern(
        int totalLength,
        int minBodyBeforeCocoon,
        int maxBodyBeforeCocoon)
    {
        int length = Mathf.Max(3, totalLength);

        List<WormPatternEntry> result = new(length)
        {
            new(WormSegmentType.Head, false)
        };

        int sectionsWithoutCocoon = 0;
        int sectionIndex = 0;

        while (result.Count < length - 1)
        {
            int bodyCount = Random.Range(4, 6);

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

            int cocoonBodyLocalIndex = -1;

            if (spawnCocoon && bodyCount >= 3)
            {
                cocoonBodyLocalIndex = Random.Range(1, bodyCount - 1);
                sectionsWithoutCocoon = 0;
            }
            else
            {
                sectionsWithoutCocoon++;
            }

            for (int i = 0; i < bodyCount; i++)
            {
                if (result.Count >= length - 1)
                    break;

                bool hasCocoon = spawnCocoon && i == cocoonBodyLocalIndex;
                result.Add(new WormPatternEntry(WormSegmentType.Body, hasCocoon));
            }

            sectionIndex++;
        }

        result.Add(new WormPatternEntry(WormSegmentType.Tail, false));

        return result;
    }
}