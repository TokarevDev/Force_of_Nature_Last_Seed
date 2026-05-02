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
        if (_config == null || !_config.Enabled)
            return baseHp;

        float independentHp = ResolveIndependentHp(
            baseHp,
            sectionIndex,
            totalSections,
            levelNumber);

        if (!_config.UsesDynamicHp || !power.IsValid)
            return ClampHp(independentHp);

        float dynamicHp =
            power.EstimatedDps *
            _config.TargetSectionLifetime *
            _config.GetLevelMultiplier(levelNumber) *
            _config.GetPressureMultiplier(sectionIndex, totalSections) *
            Mathf.Max(1f, runtimePressureMultiplier);

        if (_config.UseBaseHpAsFloor)
            dynamicHp = Mathf.Max(independentHp, dynamicHp);

        dynamicHp = ClampDynamicHp(independentHp, dynamicHp);

        float blendedHp = Mathf.Lerp(
            independentHp,
            dynamicHp,
            _config.DynamicHpWeight);

        return ClampHp(blendedHp);
    }

    private float ResolveIndependentHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        int levelNumber)
    {
        float configuredBaseHp = Mathf.Max(baseHp, _config.BaseSectionHp);

        return configuredBaseHp *
            _config.GetLevelMultiplier(levelNumber) *
            _config.GetBaseHpMultiplier(sectionIndex, totalSections) *
            _config.GetPressureMultiplier(sectionIndex, totalSections);
    }

    private float ClampDynamicHp(float independentHp, float dynamicHp)
    {
        float safeIndependentHp = Mathf.Max(1f, independentHp);
        float maxDynamicHp = safeIndependentHp * _config.MaxDynamicHpMultiplier;

        return Mathf.Min(dynamicHp, maxDynamicHp);
    }

    private int ClampHp(float hp)
    {
        return Mathf.Clamp(
            Mathf.RoundToInt(hp),
            _config.MinHp,
            _config.MaxHp);
    }
}
