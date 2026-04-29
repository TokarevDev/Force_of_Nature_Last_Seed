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
        ProjectileWeapon mainWeapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        WeaponPowerSnapshot mainPower = Estimate(mainWeapon);
        WeaponPowerSnapshot acaciaPower = Estimate(acaciaThornWeapon);

        if (!mainPower.IsValid)
            return acaciaPower;

        if (!acaciaPower.IsValid)
            return mainPower;

        return new WeaponPowerSnapshot(
            true,
            mainPower.EstimatedDps + acaciaPower.EstimatedDps,
            Mathf.Max(mainPower.DamagePerProjectile, acaciaPower.DamagePerProjectile),
            mainPower.ProjectilesPerShot + acaciaPower.ProjectilesPerShot,
            Mathf.Max(mainPower.SalvoShots, acaciaPower.SalvoShots),
            Mathf.Min(mainPower.ShotCycleTime, acaciaPower.ShotCycleTime));
    }

    public static WeaponPowerSnapshot Estimate(AcaciaThornWeapon weapon)
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
        return Mathf.Max(
            1,
            runtimeState.ParallelProjectileCount);
    }

    private static WeaponPowerSnapshot Estimate(
        AcaciaThornWeaponConfig config,
        AcaciaThornRuntimeState runtimeState)
    {
        if (config == null || runtimeState == null || !runtimeState.IsUnlocked)
            return WeaponPowerSnapshot.Invalid;

        int damagePerProjectile = AcaciaThornRuntimeState.ClampDamage(
            Mathf.Max(1, config.Damage) * (double)runtimeState.DamageMultiplier);

        int splitCount = Mathf.Max(
            0,
            config.BaseSplitCount + runtimeState.ExtraSplitProjectiles);

        float estimatedHitsPerShot = 1f +
            splitCount * config.EstimatedSplitHitChance +
            config.BounceCount * config.EstimatedBounceHitChance;

        float shotCycleTime = EstimateShotCycleTime(config, runtimeState);

        float estimatedDps = damagePerProjectile *
            Mathf.Max(1f, estimatedHitsPerShot) /
            shotCycleTime;

        return new WeaponPowerSnapshot(
            true,
            estimatedDps,
            damagePerProjectile,
            1 + splitCount,
            1,
            shotCycleTime);
    }

    private static float EstimateShotCycleTime(
        WeaponConfig config,
        WeaponRuntimeState runtimeState,
        int salvoShots)
    {
        float fireRateBonus = Mathf.Min(
            runtimeState.FireRateBonus * config.FireRateBonusEffectiveness,
            config.MaxFireRateBonus * config.FireRateBonusEffectiveness);

        float shotCooldown = Mathf.Max(
            config.MinShotCooldown,
            config.FireRate / (1f + fireRateBonus));

        float salvoTime = Mathf.Max(0, salvoShots - 1) *
            Mathf.Max(0.01f, runtimeState.SalvoInterval);

        return Mathf.Max(0.01f, shotCooldown + salvoTime);
    }

    private static float EstimateShotCycleTime(
        AcaciaThornWeaponConfig config,
        AcaciaThornRuntimeState runtimeState)
    {
        float fireRateBonus = Mathf.Min(
            runtimeState.FireRateBonus * config.FireRateBonusEffectiveness,
            config.MaxFireRateBonus * config.FireRateBonusEffectiveness);

        return Mathf.Max(
            0.01f,
            Mathf.Max(
                config.MinCooldown,
                config.Cooldown / (1f + fireRateBonus)));
    }
}
