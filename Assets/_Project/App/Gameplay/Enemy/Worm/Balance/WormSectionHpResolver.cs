using UnityEngine;

public sealed class WormSectionHpResolver
{
    private readonly WormHpScalingConfig _config;

    public WormSectionHpResolver(WormHpScalingConfig config)
    {
        _config = config;
    }

    public int ResolveHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        int levelNumber,
        WeaponPowerSnapshot power,
        float runtimePressureMultiplier)
    {
        if (_config == null || !_config.Enabled || !power.IsValid)
            return baseHp;

        float dynamicHp =
            power.EstimatedDps *
            _config.TargetSectionLifetime *
            _config.GetLevelMultiplier(levelNumber) *
            _config.GetPressureMultiplier(sectionIndex, totalSections) *
            Mathf.Max(1f, runtimePressureMultiplier);

        if (_config.UseBaseHpAsFloor)
            dynamicHp = Mathf.Max(baseHp, dynamicHp);

        float blendedHp = Mathf.Lerp(
            baseHp,
            dynamicHp,
            _config.DynamicHpWeight);

        return Mathf.Clamp(
            Mathf.RoundToInt(blendedHp),
            _config.MinHp,
            _config.MaxHp);
    }
}
