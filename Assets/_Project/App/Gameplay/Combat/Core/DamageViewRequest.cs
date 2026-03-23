using UnityEngine;

public readonly struct DamageViewRequest
{
    public readonly int Amount;
    public readonly Vector3 WorldPosition;
    public readonly DamageKind Kind;
    public readonly bool IsCritical;

    public DamageViewRequest(int amount, Vector3 worldPosition, DamageKind kind, bool isCritical)
    {
        Amount = amount;
        WorldPosition = worldPosition;
        Kind = kind;
        IsCritical = isCritical;
    }

    public static DamageViewRequest FromDamageInfo(DamageInfo info)
    {
        return new DamageViewRequest(
            info.Amount,
            info.HitPosition,
            info.Kind,
            info.IsCritical);
    }
}