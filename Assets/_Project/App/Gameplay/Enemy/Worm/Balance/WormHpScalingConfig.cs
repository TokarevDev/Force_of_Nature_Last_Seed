using UnityEngine;

[CreateAssetMenu(menuName = "Game/Worm/HP Scaling Config")]
public sealed class WormHpScalingConfig : ScriptableObject
{
    [SerializeField] private bool _enabled = true;

    [Header("Independent HP")]
    [SerializeField][Min(1)] private int _baseSectionHp = 8;
    [SerializeField][Min(0.1f)] private float _baseHpStartMultiplier = 1f;
    [SerializeField][Min(0.1f)] private float _baseHpEndMultiplier = 3.2f;

    [Header("Adaptive HP")]
    [SerializeField][Min(0.1f)] private float _targetSectionLifetime = 8.5f;
    [SerializeField][Range(0f, 1f)] private float _dynamicHpWeight = 0.82f;
    [SerializeField][Min(1f)] private float _maxDynamicHpMultiplier = 8f;
    [SerializeField] private bool _useBaseHpAsFloor = true;

    [Header("Level")]
    [SerializeField][Min(1f)] private float _levelMultiplier = 1.12f;

    [Header("Pressure")]
    [SerializeField][Min(0.1f)] private float _startPressureMultiplier = 1f;
    [SerializeField][Min(0.1f)] private float _endPressureMultiplier = 4.2f;

    [Header("Limits")]
    [SerializeField][Min(1)] private int _minHp = 3;
    [SerializeField][Min(1)] private int _maxHp = WeaponRuntimeState.MaxProjectileDamage;

    public bool Enabled => _enabled;
    public bool UsesDynamicHp => _enabled && _dynamicHpWeight > 0f;
    public int BaseSectionHp => _baseSectionHp;
    public float TargetSectionLifetime => _targetSectionLifetime;
    public float DynamicHpWeight => _dynamicHpWeight;
    public float MaxDynamicHpMultiplier => _maxDynamicHpMultiplier;
    public bool UseBaseHpAsFloor => _useBaseHpAsFloor;
    public int MinHp => _minHp;
    public int MaxHp => _maxHp;

    public float GetLevelMultiplier(int levelNumber)
    {
        return Mathf.Pow(
            _levelMultiplier,
            Mathf.Max(0, levelNumber - 1));
    }

    public float GetBaseHpMultiplier(int sectionIndex, int totalSections)
    {
        if (totalSections <= 1)
            return _baseHpStartMultiplier;

        float normalized = Mathf.Clamp01(sectionIndex / (float)(totalSections - 1));

        return Mathf.Lerp(
            _baseHpStartMultiplier,
            _baseHpEndMultiplier,
            normalized);
    }

    public float GetPressureMultiplier(int sectionIndex, int totalSections)
    {
        if (totalSections <= 1)
            return _startPressureMultiplier;

        float normalized = Mathf.Clamp01(sectionIndex / (float)(totalSections - 1));

        return Mathf.Lerp(
            _startPressureMultiplier,
            _endPressureMultiplier,
            normalized);
    }

    private void OnValidate()
    {
        _baseSectionHp = Mathf.Max(1, _baseSectionHp);
        _baseHpStartMultiplier = Mathf.Max(0.1f, _baseHpStartMultiplier);
        _baseHpEndMultiplier = Mathf.Max(0.1f, _baseHpEndMultiplier);
        _targetSectionLifetime = Mathf.Max(0.1f, _targetSectionLifetime);
        _dynamicHpWeight = Mathf.Clamp01(_dynamicHpWeight);
        _maxDynamicHpMultiplier = Mathf.Max(1f, _maxDynamicHpMultiplier);
        _levelMultiplier = Mathf.Max(1f, _levelMultiplier);
        _startPressureMultiplier = Mathf.Max(0.1f, _startPressureMultiplier);
        _endPressureMultiplier = Mathf.Max(0.1f, _endPressureMultiplier);
        _minHp = Mathf.Max(1, _minHp);
        _maxHp = Mathf.Max(_minHp, _maxHp);
    }
}
