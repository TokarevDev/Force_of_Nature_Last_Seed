using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for creating worm segments from a generated pattern.
/// Handles segment activation, rendering order setup,
/// cocoon enabling and damage receiver attachment.
/// </summary>
public sealed class WormFactory
{
    private readonly WormSegmentPool _pool;

    // High sorting order ensures the head stays visually above the chain.
    private const int BASE_SORT_ORDER = 2500;

    public WormFactory(WormSegmentPool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Instantiates worm segments according to the generated pattern.
    /// Also configures sorting order and cocoon overlays.
    /// </summary>
    public List<WormSegment> CreateSegments(
        List<WormPatternEntry> pattern,
        out WormSegment head,
        out WormSegment tail)
    {
        List<WormSegment> segments = new(pattern.Count);

        head = null;
        tail = null;

        for (int i = 0; i < pattern.Count; i++)
        {
            WormPatternEntry entry = pattern[i];

            WormSegment seg = _pool.Get(entry.Type);

            if (seg == null)
            {
                Debug.LogError($"Failed to get segment of type {entry.Type}");
                continue;
            }

            seg.Activate();
            seg.SetHasReward(entry.HasCocoon);

            seg.Index = i;

            int order = entry.Type == WormSegmentType.Head
                ? BASE_SORT_ORDER
                : Mathf.Max(1, BASE_SORT_ORDER - i);

            seg.SetSortingOrder(order);

            if (entry.HasCocoon)
                seg.EnableCocoon();

            if (entry.Type == WormSegmentType.Head)
                head = seg;

            if (entry.Type == WormSegmentType.Tail)
                tail = seg;

            segments.Add(seg);
        }

        return segments;
    }

    public void AttachDamageReceivers(
        List<WormSegment> segments,
        WormCombatController combat)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            WormSegment seg = segments[i];

            var receiver = seg.GetComponent<WormSegmentDamageReceiver>();

            if (receiver == null)
                receiver = seg.gameObject.AddComponent<WormSegmentDamageReceiver>();

            receiver.Initialize(combat, seg);
        }
    }
}