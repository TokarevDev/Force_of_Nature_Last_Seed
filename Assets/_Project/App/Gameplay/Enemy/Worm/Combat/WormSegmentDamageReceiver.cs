using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormSegmentDamageReceiver : MonoBehaviour, IDamageable
{
    private WormCombatController _combat;
    private WormSegment _segment;

    public void Initialize(WormCombatController combat, WormSegment segment)
    {
        _combat = combat;
        _segment = segment;
    }

    public WormSegment GetSegment()
    {
        return _segment;
    }

    public WormSection GetDamageSection()
    {
        if (_combat == null)
            return null;

        return _combat.ResolveDamageSection(_segment);
    }

    public void TakeDamage(in DamageInfo damageInfo)
    {
        if (_combat == null || _segment == null)
            return;

        if (!_segment.IsAlive)
            return;

        _combat.RegisterHit(_segment, damageInfo);
    }
}
