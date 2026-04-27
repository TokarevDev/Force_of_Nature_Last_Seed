using UnityEngine;

public readonly struct ProjectileRuntimeStats
{
    public readonly int Damage;
    public readonly int ExtraPenetration;
    public readonly float CriticalChance;
    public readonly float CriticalDamageMultiplier;

    public ProjectileRuntimeStats(
        int damage,
        int extraPenetration,
        float criticalChance,
        float criticalDamageMultiplier)
    {
        Damage = Mathf.Max(0, damage);
        ExtraPenetration = Mathf.Max(0, extraPenetration);
        CriticalChance = Mathf.Clamp01(criticalChance);
        CriticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
    }
}
