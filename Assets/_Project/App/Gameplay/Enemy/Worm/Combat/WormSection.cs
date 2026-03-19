using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Groups segments into a single damage unit with shared HP.
/// Responsible for damage processing and segment ownership.
/// </summary>
public sealed class WormSection
{
    public Action<WormSection> HPChanged;
    public Action<WormSection> Destroyed;
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int Index { get; set; }

    private readonly List<WormSegment> _segments = new();

    public IReadOnlyList<WormSegment> Segments => _segments;
    public bool IsDestroyed => CurrentHP <= 0;

    public void Init(int hp)
    {
        MaxHP = hp;
        CurrentHP = hp;
    }

    public void AddSegment(WormSegment segment)
    {
        if (segment == null)
            return;

        _segments.Add(segment);
        segment.Section = this;
    }

    public Transform GetHpAnchor()
    {
        // priority: cocoon => center segment

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].CachedTransform;
        }

        //fallback: center segment
        int centerIndex = _segments.Count / 2;
        return _segments[centerIndex].CachedTransform;
    }

    /// <summary>
    /// Returns index of the segment used as logical center of the section.
    /// Used for sorting sections along the worm path.
    /// Cocoon segment has priority, otherwise geometric center is used.
    /// </summary>
    public int GetCenterSegmentIndex()
    {
        // cocoon has priority
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].Index;
        }

        // fallback: center segment
        int mid = _segments.Count / 2;
        return _segments[mid].Index;
    }

    public void Damage(int damage)
    {
        if (IsDestroyed) return;

        CurrentHP -= damage;

        if (CurrentHP < 0)
            CurrentHP = 0;

        HPChanged?.Invoke(this);

        if (CurrentHP == 0)
            Destroyed?.Invoke(this);
    }

    /// <summary>
    /// Breaks ownership links between this section and its segments.
    /// Must be called exactly once when the section is removed from gameplay.
    /// </summary>
    public List<WormSegment> ReleaseSegments()
    {
        List<WormSegment> released = new(_segments.Count);

        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null)
                continue;

            if (segment.Section == this)
                segment.Section = null;

            released.Add(segment);
        }

        _segments.Clear();
        return released;
    }
}