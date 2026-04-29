using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WormSection
{
    public Action<WormSection> HPChanged;
    public Action<WormSection> Destroyed;

    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int Index { get; set; }

    public CocoonRewardProfile CocoonProfile { get; private set; }
    public bool HasCocoon => CocoonProfile != null;
    public bool HasReward => HasCocoon;

    private readonly List<WormSegment> _segments = new();

    public IReadOnlyList<WormSegment> Segments => _segments;
    public bool IsDestroyed => CurrentHP <= 0;
    public bool HasTakenDamage => CurrentHP < MaxHP;

    public void Init(int hp)
    {
        SetHp(hp, notify: false);
    }

    public void SetHp(int hp)
    {
        SetHp(hp, notify: true);
    }

    public bool HasVisibleAliveSegment()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment != null && segment.IsAlive && segment.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    public void SetCocoon(CocoonRewardProfile profile)
    {
        CocoonProfile = profile;
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
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].CachedTransform;
        }

        int centerIndex = _segments.Count / 2;
        return _segments[centerIndex].CachedTransform;
    }

    public int GetCenterSegmentIndex()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].HasCocoon)
                return _segments[i].Index;
        }

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

    private void SetHp(int hp, bool notify)
    {
        int clampedHp = Mathf.Max(1, hp);

        MaxHP = clampedHp;
        CurrentHP = clampedHp;

        if (notify)
            HPChanged?.Invoke(this);
    }

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
