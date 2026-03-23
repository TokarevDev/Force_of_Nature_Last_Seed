using UnityEngine;

public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly Vector3 HitPosition;
    public readonly DamageKind Kind;
    public readonly Object Source;
    public readonly bool IsCritical;

    public DamageInfo(int amount,
        Vector3 hitPosition,
        DamageKind kind = DamageKind.Normal,
        Object source = null,
        bool isCritical = false)
    {
        Amount = amount;
        HitPosition = hitPosition;
        Kind = kind;
        Source = source;
        IsCritical = isCritical;
    }
}