using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates and initializes worm segment views from pattern data.
/// Does not contain gameplay logic.
/// </summary>
public sealed class WormFactory
{
    private readonly WormSegmentPool _pool;

    private const int BASE_SORT_ORDER = 2500;

    public WormFactory(WormSegmentPool pool)
    {
        _pool = pool;
    }

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

            seg.PrepareForWorm();
            seg.Index = i;

            int order = entry.Type == WormSegmentType.Head
                ? BASE_SORT_ORDER
                : Mathf.Max(1, BASE_SORT_ORDER - i);

            seg.SetSortingOrder(order);

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

            InitializeDamageReceivers(seg, combat);
        }
    }

    private static void InitializeDamageReceivers(WormSegment segment, WormCombatController combat)
    {
        if (!segment.TryGetComponent<WormSegmentDamageReceiver>(out _))
            segment.gameObject.AddComponent<WormSegmentDamageReceiver>();

        WormSegmentDamageReceiver[] receivers =
            segment.GetComponentsInChildren<WormSegmentDamageReceiver>(true);

        for (int i = 0; i < receivers.Length; i++)
        {
            WormSegmentDamageReceiver receiver = receivers[i];

            if (receiver != null)
                receiver.Initialize(combat, segment);
        }
    }
}
