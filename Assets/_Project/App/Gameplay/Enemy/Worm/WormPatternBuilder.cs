using System.Collections.Generic;
using UnityEngine;

public readonly struct WormPatternEntry
{
    public readonly WormSegmentType Type;

    public WormPatternEntry(WormSegmentType type)
    {
        Type = type;
    }
}

/// <summary>
/// Generates only structural layout (Head → Body → Tail).
/// No gameplay logic (cocoons/rewards) here.
/// </summary>
public static class WormPatternBuilder
{
    public static List<WormPatternEntry> BuildPattern(int totalLength)
    {
        int length = Mathf.Max(3, totalLength);

        List<WormPatternEntry> result = new(length)
        {
            new(WormSegmentType.Head)
        };

        while (result.Count < length - 1)
        {
            int bodyCount = Random.Range(4, 6);

            for (int i = 0; i < bodyCount; i++)
            {
                if (result.Count >= length - 1)
                    break;

                result.Add(new WormPatternEntry(WormSegmentType.Body));
            }
        }

        result.Add(new WormPatternEntry(WormSegmentType.Tail));

        return result;
    }
}