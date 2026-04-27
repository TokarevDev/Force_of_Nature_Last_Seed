using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies damage to worm sections and coordinates section removal
/// with the movement chain once a section is destroyed.
/// </summary>
[DisallowMultipleComponent]
public sealed class WormCombatController : MonoBehaviour
{
    public event Action<WormSection, int> SectionDamaged;

    public event Action<DamageViewRequest> DamageDealt;

    public static event Action OnWormDied;

    [SerializeField] private WormController _wormController;
    [SerializeField] private RewardInstaller _rewardInstaller;

    private readonly List<WormSection> _sections = new();

    private WormSegment _head;
    private WormSegment _tail;

    public void Init(WormSegment head, WormSegment tail, List<WormSection> sections)
    {
        _head = head;
        _tail = tail;

        _sections.Clear();
        _sections.AddRange(sections);
    }

    public void RegisterHit(WormSegment segment, in DamageInfo damageInfo)
    {
        WormSection section = ResolveDamageSection(segment);

        if (section == null || section.IsDestroyed)
            return;

        section.Damage(damageInfo.Amount);
        SectionDamaged?.Invoke(section, damageInfo.Amount);
        DamageDealt?.Invoke(DamageViewRequest.FromDamageInfo(damageInfo));

        if (!section.IsDestroyed)
            return;

        DestroySection(section);
    }

    private void DestroySection(WormSection section)
    {
        bool rewardTriggered = section.HasReward;

        List<WormSegment> removedSegments = section.ReleaseSegments();

        for (int i = 0; i < removedSegments.Count; i++)
        {
            WormSegment seg = removedSegments[i];

            if (seg == null || !seg.IsAlive)
                continue;

            if (seg.Type is WormSegmentType.Head or WormSegmentType.Tail)
                continue;

            seg.KillVisualAndCollision();
        }

        _sections.Remove(section);

        int removedFromChain = 0;
        int firstRemovedIndex = -1;

        if (_wormController != null)
            removedFromChain = _wormController.RemoveDestroyedSectionSegments(removedSegments, out firstRemovedIndex);

        if (_wormController != null && removedFromChain > 0)
            _wormController.RollbackDestroyedGap(removedFromChain, firstRemovedIndex);

        if (rewardTriggered && _rewardInstaller != null)
        {
            _rewardInstaller.OpenReward();
        }

        if (_sections.Count == 0)
        {
            if (_head != null && _head.IsAlive)
                _head.KillVisualAndCollision();

            if (_tail != null && _tail.IsAlive)
                _tail.KillVisualAndCollision();

            OnWormDied?.Invoke();
        }
    }

    public WormSection ResolveDamageSection(WormSegment segment)
    {
        if (segment == null || !segment.IsAlive)
            return null;

        if (segment.Type == WormSegmentType.Head)
            return GetFirstAliveSection();

        if (segment.Type == WormSegmentType.Tail)
            return null;

        WormSection section = segment.Section;

        if (section == null || section.IsDestroyed)
            return null;

        return section;
    }

    private WormSection GetFirstAliveSection()
    {
        for (int i = 0; i < _sections.Count; i++)
        {
            WormSection section = _sections[i];

            if (section != null && !section.IsDestroyed)
                return section;
        }

        return null;
    }
}
