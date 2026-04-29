using UnityEngine;

public static class WeaponPowerEstimator
{
    public static WeaponPowerSnapshot Estimate(ProjectileWeapon weapon)
    {
        if (weapon == null)
            return WeaponPowerSnapshot.Invalid;

        return Estimate(weapon.Config, weapon.RuntimeState);
    }

    public static WeaponPowerSnapshot Estimate(
        WeaponConfig config,
        WeaponRuntimeState runtimeState)
    {
        if (config == null || config.Projectile == null || runtimeState == null)
            return WeaponPowerSnapshot.Invalid;

        int damagePerProjectile = EstimateDamagePerProjectile(
            config.Projectile.Damage,
            runtimeState);

        int projectilesPerShot = EstimateProjectilesPerShot(runtimeState);
        int salvoShots = Mathf.Max(1, 1 + runtimeState.SalvoExtraShots);
        float shotCycleTime = EstimateShotCycleTime(config, runtimeState, salvoShots);

        float estimatedDps =
            damagePerProjectile *
            projectilesPerShot *
            salvoShots /
            shotCycleTime;

        return new WeaponPowerSnapshot(
            true,
            estimatedDps,
            damagePerProjectile,
            projectilesPerShot,
            salvoShots,
            shotCycleTime);
    }

    private static int EstimateDamagePerProjectile(
        int baseDamage,
        WeaponRuntimeState runtimeState)
    {
        double rawDamage = Mathf.Max(1, baseDamage) * (double)runtimeState.DamageMultiplier;
        int damage = WeaponRuntimeState.ClampDamage(rawDamage);

        float criticalChance = Mathf.Clamp01(runtimeState.CriticalChance);
        float criticalBonus = Mathf.Max(0f, runtimeState.CriticalDamageMultiplier - 1f);
        float expectedCriticalMultiplier = 1f + criticalChance * criticalBonus;

        return WeaponRuntimeState.ClampDamage(damage * (double)expectedCriticalMultiplier);
    }

    private static int EstimateProjectilesPerShot(WeaponRuntimeState runtimeState)
    {
        int spreadCount = 1;

        var modifiers = runtimeState.ShotModifiers;

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] is SpreadModifierData spread)
                spreadCount += Mathf.Max(0, spread.Count - 1);
        }

        return Mathf.Max(
            1,
            runtimeState.ParallelProjectileCount * spreadCount);
    }

    private static float EstimateShotCycleTime(
        WeaponConfig config,
        WeaponRuntimeState runtimeState,
        int salvoShots)
    {
        float fireRateBonus = Mathf.Min(
            runtimeState.FireRateBonus,
            config.MaxFireRateBonus);

        float shotCooldown = Mathf.Max(
            config.MinShotCooldown,
            config.FireRate / (1f + fireRateBonus));

        float salvoTime = Mathf.Max(0, salvoShots - 1) *
            Mathf.Max(0.01f, runtimeState.SalvoInterval);

        return Mathf.Max(0.01f, shotCooldown + salvoTime);
    }
}
