public readonly struct WeaponPowerSnapshot
{
    public static readonly WeaponPowerSnapshot Invalid = new(false, 0f, 0, 0, 0, 0f);

    public readonly bool IsValid;
    public readonly float EstimatedDps;
    public readonly int DamagePerProjectile;
    public readonly int ProjectilesPerShot;
    public readonly int SalvoShots;
    public readonly float ShotCycleTime;

    public WeaponPowerSnapshot(
        bool isValid,
        float estimatedDps,
        int damagePerProjectile,
        int projectilesPerShot,
        int salvoShots,
        float shotCycleTime)
    {
        IsValid = isValid;
        EstimatedDps = estimatedDps;
        DamagePerProjectile = damagePerProjectile;
        ProjectilesPerShot = projectilesPerShot;
        SalvoShots = salvoShots;
        ShotCycleTime = shotCycleTime;
    }
}
