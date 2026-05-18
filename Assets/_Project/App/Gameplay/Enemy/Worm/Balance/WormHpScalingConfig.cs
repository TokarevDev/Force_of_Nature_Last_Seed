using UnityEngine;

[CreateAssetMenu(menuName = "Game/Worm/HP Scaling Config")]
public sealed class WormHpScalingConfig : ScriptableObject
{
    [SerializeField] private bool _enabled = true;

    [Header("Independent HP")]
    [SerializeField][Min(1)] private int _baseSectionHp = 8;
    [SerializeField][Min(0.1f)] private float _baseHpStartMultiplier = 1f;
    [SerializeField][Min(0.1f)] private float _baseHpEndMultiplier = 1.6f;

    [Header("Adaptive HP")]
    [SerializeField][Min(0.1f)] private float _targetSectionLifetime = 5.5f;
    [SerializeField] private bool _useTargetLifetimeCurve = true;
    [SerializeField] private AnimationCurve _targetSectionLifetimeByProgress = CreateDefaultTargetLifetimeCurve();
    [SerializeField][Range(0f, 1f)] private float _dynamicHpWeight = 0.55f;
    [SerializeField][Min(1f)] private float _maxDynamicHpMultiplier = 900f;
    [SerializeField] private bool _useBaseHpAsFloor = true;

    [Header("Level")]
    [SerializeField][Min(1f)] private float _levelMultiplier = 1.12f;

    [Header("Global")]
    [SerializeField][Min(0.1f)] private float _hpMultiplier = 2f;

    [Header("Pressure")]
    [SerializeField][Min(0.1f)] private float _startPressureMultiplier = 1f;
    [SerializeField][Min(0.1f)] private float _endPressureMultiplier = 2.6f;
    [SerializeField] private bool _usePressureCurve = true;
    [SerializeField] private AnimationCurve _pressureByProgress = CreateDefaultPressureCurve();

    [Header("Head Path Pressure")]
    [SerializeField] private bool _useHeadPathPressure = true;
    [SerializeField][Range(0f, 1f)] private float _strongHeadPressureUntilProgress = 0.3f;
    [SerializeField][Range(0f, 1f)] private float _minimumHeadPressureFromProgress = 0.85f;
    [SerializeField][Min(0.1f)] private float _earlyHeadPressureMultiplier = 1f;
    [SerializeField][Min(0.1f)] private float _lateHeadPressureMultiplier = 1.15f;

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
    public float HpMultiplier => _hpMultiplier;

    private void OnEnable()
    {
        EnsureCurves();
    }

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

    public float GetTargetSectionLifetime(int sectionIndex, int totalSections)
    {
        if (!_useTargetLifetimeCurve || !HasCurve(_targetSectionLifetimeByProgress))
            return _targetSectionLifetime;

        return Mathf.Max(
            0.1f,
            _targetSectionLifetimeByProgress.Evaluate(GetSectionProgress(sectionIndex, totalSections)));
    }

    public float GetPressureMultiplier(int sectionIndex, int totalSections)
    {
        if (_usePressureCurve && HasCurve(_pressureByProgress))
        {
            return Mathf.Max(
                0.1f,
                _pressureByProgress.Evaluate(GetSectionProgress(sectionIndex, totalSections)));
        }

        if (totalSections <= 1)
            return _startPressureMultiplier;

        float normalized = GetSectionProgress(sectionIndex, totalSections);

        return Mathf.Lerp(
            _startPressureMultiplier,
            _endPressureMultiplier,
            normalized);
    }

    public float GetHeadPathPressureMultiplier(float headProgressNormalized)
    {
        if (!_useHeadPathPressure)
            return 1f;

        float start = Mathf.Min(
            _strongHeadPressureUntilProgress,
            _minimumHeadPressureFromProgress);

        float end = Mathf.Max(
            _strongHeadPressureUntilProgress,
            _minimumHeadPressureFromProgress);

        float progress = Mathf.Clamp01(headProgressNormalized);

        if (Mathf.Approximately(start, end))
        {
            return progress <= start
                ? _earlyHeadPressureMultiplier
                : _lateHeadPressureMultiplier;
        }

        float t = Mathf.InverseLerp(start, end, progress);
        t = t * t * (3f - 2f * t);

        return Mathf.Lerp(
            _earlyHeadPressureMultiplier,
            _lateHeadPressureMultiplier,
            t);
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
        _hpMultiplier = Mathf.Max(0.1f, _hpMultiplier);
        _startPressureMultiplier = Mathf.Max(0.1f, _startPressureMultiplier);
        _endPressureMultiplier = Mathf.Max(0.1f, _endPressureMultiplier);
        _strongHeadPressureUntilProgress = Mathf.Clamp01(_strongHeadPressureUntilProgress);
        _minimumHeadPressureFromProgress = Mathf.Clamp01(_minimumHeadPressureFromProgress);
        _earlyHeadPressureMultiplier = Mathf.Max(0.1f, _earlyHeadPressureMultiplier);
        _lateHeadPressureMultiplier = Mathf.Max(0.1f, _lateHeadPressureMultiplier);
        _minHp = Mathf.Max(1, _minHp);
        _maxHp = Mathf.Max(_minHp, _maxHp);
        EnsureCurves();
    }

    private void EnsureCurves()
    {
        if (!HasCurve(_targetSectionLifetimeByProgress))
        {
            _targetSectionLifetimeByProgress = CreateDefaultTargetLifetimeCurve();
        }

        if (!HasCurve(_pressureByProgress))
        {
            _pressureByProgress = CreateDefaultPressureCurve();
        }
    }

    private static AnimationCurve CreateDefaultTargetLifetimeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.08f, 1.1f),
            new Keyframe(0.18f, 2.1f),
            new Keyframe(0.32f, 4.5f),
            new Keyframe(0.55f, 10f),
            new Keyframe(0.78f, 18f),
            new Keyframe(1f, 32f));
    }

    private static AnimationCurve CreateDefaultPressureCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.12f, 0.5f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.45f, 2.4f),
            new Keyframe(0.72f, 5f),
            new Keyframe(1f, 8f));
    }

    private static float GetSectionProgress(int sectionIndex, int totalSections)
    {
        if (totalSections <= 1)
            return 0f;

        return Mathf.Clamp01(sectionIndex / (float)(totalSections - 1));
    }

    private static bool HasCurve(AnimationCurve curve)
    {
        return curve != null && curve.length > 0;
    }
}
