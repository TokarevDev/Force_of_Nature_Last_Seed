using System.Collections.Generic;

/// <summary>
/// Represents a logical damage group inside the worm chain.
/// Owns the segments that belong to it and releases them when destroyed.
/// </summary>
public sealed class WormSection
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }

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

    public void Damage(int damage)
    {
        CurrentHP -= damage;

        if (CurrentHP < 0)
            CurrentHP = 0;
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