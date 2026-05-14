using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormSpawner : MonoBehaviour
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    [Header("Prefabs")]
    [SerializeField] private WormSegment _headPrefab;

    [SerializeField] private WormSegment _bodyPrefab;
    [SerializeField] private WormSegment _tailPrefab;

    [Header("Controllers")]
    [SerializeField] private WormController _wormController;

    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private WormSectionHpPresenter _hpPresenter;

    [Header("Rewards")]
    [SerializeField] private RewardInstaller _rewardInstaller;

    [Header("Adaptive HP")]
    [SerializeField] private ProjectileWeapon _weapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [SerializeField] private WormHpScalingConfig _hpScalingConfig;
    [SerializeField][Min(1)] private int _adaptiveRebalanceUpgradeInterval = 1;
    [SerializeField][Min(0f)] private float _adaptiveRebalanceMinInterval = 5f;

    [Header("Generation")]
    [SerializeField][Min(1)] private int _levelNumber = 1;

    [Min(3)]
    [SerializeField] private int _totalLength = 60;

    [Header("Pooling")]
    [SerializeField] private int _poolPadding = 10;
    [SerializeField, Min(1)] private int _prewarmBatchSize = 64;

    private WormSegmentPool _segmentPool;
    private WormFactory _wormFactory;
    private WormSectionHpResolver _hpResolver;
    private readonly List<WormSection> _sections = new();

    private bool _isSpawned;
    private int _bodyPoolCapacity;
    private float _runtimePressureMultiplier = 1f;
    private int _pendingAdaptiveUpgradeChanges;
    private float _lastAdaptiveRebalanceTime;
    private bool _hasAppliedAdaptiveUpgradeRebalance;

    private void OnEnable()
    {
        if (_weapon != null)
            _weapon.RuntimeStatsChanged += OnWeaponRuntimeStatsChanged;

        if (_acaciaThornWeapon != null)
            _acaciaThornWeapon.RuntimeStatsChanged += OnWeaponRuntimeStatsChanged;
    }

    private void OnDisable()
    {
        if (_weapon != null)
            _weapon.RuntimeStatsChanged -= OnWeaponRuntimeStatsChanged;

        if (_acaciaThornWeapon != null)
            _acaciaThornWeapon.RuntimeStatsChanged -= OnWeaponRuntimeStatsChanged;
    }

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController not assigned", this);

        if (_wormCombat == null)
            Debug.LogError("WormCombatController not assigned", this);

        _bodyPoolCapacity = Mathf.Max(1, Mathf.Max(3, _totalLength) - 2 + _poolPadding);
        _hpResolver = new WormSectionHpResolver(_hpScalingConfig);

        _segmentPool = new WormSegmentPool(
            transform,
            _headPrefab,
            _bodyPrefab,
            _tailPrefab);

        _wormFactory = new WormFactory(_segmentPool);
    }

    private IEnumerator Start()
    {
        if (_segmentPool != null)
            yield return _segmentPool.PrewarmRoutine(_bodyPoolCapacity, _prewarmBatchSize);

        SpawnWorm();
    }

    public void SpawnWorm()
    {
        if (_isSpawned)
            return;

        List<WormPatternEntry> pattern =
            WormPatternBuilder.BuildPattern(_totalLength);

        List<WormSegment> segments =
            _wormFactory.CreateSegments(
                pattern,
                out WormSegment head,
                out WormSegment tail);

        if (head == null || tail == null)
        {
            Debug.LogError("Worm spawn failed: head or tail missing", this);
            return;
        }

        List<WormSection> sections =
            WormSectionBuilder.BuildSections(
                segments,
                GetCocoonProfiles());

        AssignSectionsHP(sections);

        _sections.Clear();
        _sections.AddRange(sections);
        _pendingAdaptiveUpgradeChanges = 0;
        _lastAdaptiveRebalanceTime = Time.time;
        _hasAppliedAdaptiveUpgradeRebalance = false;

        _wormFactory.AttachDamageReceivers(segments, _wormCombat);

        _wormController.Init(segments);
        _wormCombat.Init(head, tail, sections);
        _hpPresenter.BindSections(sections);

        _isSpawned = true;
    }

    private void AssignSectionsHP(List<WormSection> sections)
    {
        sections.Sort((a, b) =>
            a.GetCenterSegmentIndex().CompareTo(b.GetCenterSegmentIndex()));

        WeaponPowerSnapshot power = GetWeaponPowerForHp();
        int totalSections = sections.Count;
        int previousHp = 0;
        float headPathPressureMultiplier = 1f;

        for (int i = 0; i < sections.Count; i++)
        {
            sections[i].Index = i;

            int baseHp = WormSectionHPGenerator.GetHP(i, _levelNumber);
            int hp = ResolveSectionHp(
                baseHp,
                i,
                totalSections,
                power,
                headPathPressureMultiplier);
            hp = EnsureHpAbovePrevious(hp, previousHp);

            sections[i].Init(hp);
            previousHp = hp;
        }
    }

    public void SetRuntimePressureMultiplier(float multiplier)
    {
        float clampedMultiplier = Mathf.Max(1f, multiplier);

        if (Mathf.Approximately(_runtimePressureMultiplier, clampedMultiplier))
            return;

        _runtimePressureMultiplier = clampedMultiplier;

        if (UsesDynamicHp())
            RebalanceFutureSections();
    }

    private void OnWeaponRuntimeStatsChanged()
    {
        if (!UsesDynamicHp() || !_isSpawned)
            return;

        _pendingAdaptiveUpgradeChanges++;

        if (ShouldRebalanceAfterUpgradeChange())
            RebalanceAdaptiveHpWave();
    }

    private void RebalanceFutureSections()
    {
        if (!_isSpawned || _sections.Count == 0)
            return;

        WeaponPowerSnapshot power = GetWeaponPowerForHp();

        if (!power.IsValid)
            return;

        int totalSections = _sections.Count;
        int previousHp = 0;
        float headPathPressureMultiplier = GetHeadPathPressureMultiplierForHp();

        for (int i = 0; i < _sections.Count; i++)
        {
            WormSection section = _sections[i];
            int sectionIndex = section != null ? section.Index : i;
            int baseHp = WormSectionHPGenerator.GetHP(sectionIndex, _levelNumber);
            int hp = ResolveSectionHp(
                baseHp,
                sectionIndex,
                totalSections,
                power,
                headPathPressureMultiplier);
            hp = EnsureHpAbovePrevious(hp, previousHp);

            if (CanRebalanceSection(section))
            {
                section.SetHp(hp);
                previousHp = hp;
                continue;
            }

            previousHp = Mathf.Max(previousHp, hp);
        }
    }

    private int ResolveSectionHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        WeaponPowerSnapshot power,
        float headPathPressureMultiplier)
    {
        if (_hpResolver == null)
            return baseHp;

        return _hpResolver.ResolveHp(
            baseHp,
            sectionIndex,
            totalSections,
            _levelNumber,
            power,
            _runtimePressureMultiplier,
            headPathPressureMultiplier);
    }

    private float GetHeadPathPressureMultiplierForHp()
    {
        if (_hpScalingConfig == null || _wormController == null)
            return 1f;

        return _hpScalingConfig.GetHeadPathPressureMultiplier(
            _wormController.HeadControlPointProgressNormalized);
    }

    private WeaponPowerSnapshot GetWeaponPowerForHp()
    {
        return UsesDynamicHp()
            ? WeaponPowerEstimator.Estimate(_weapon, _acaciaThornWeapon)
            : WeaponPowerSnapshot.Invalid;
    }

    private bool UsesDynamicHp()
    {
        return _hpScalingConfig != null && _hpScalingConfig.UsesDynamicHp;
    }

    private bool ShouldRebalanceAfterUpgradeChange()
    {
        if (!_hasAppliedAdaptiveUpgradeRebalance)
            return true;

        if (_pendingAdaptiveUpgradeChanges >= _adaptiveRebalanceUpgradeInterval)
            return true;

        return Time.time - _lastAdaptiveRebalanceTime >= _adaptiveRebalanceMinInterval;
    }

    private void RebalanceAdaptiveHpWave()
    {
        _pendingAdaptiveUpgradeChanges = 0;
        _lastAdaptiveRebalanceTime = Time.time;
        _hasAppliedAdaptiveUpgradeRebalance = true;
        RebalanceFutureSections();
    }

    private static bool CanRebalanceSection(WormSection section)
    {
        return section != null
            && !section.IsDestroyed
            && !section.HasTakenDamage
            && !section.HasVisibleAliveSegment();
    }

    private static int EnsureHpAbovePrevious(int hp, int previousHp)
    {
        if (previousHp <= 0)
            return Mathf.Max(1, hp);

        if (previousHp >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        int minimumIncrease = GetMinimumVisibleHpIncrease(previousHp);

        return Mathf.Min(
            WeaponRuntimeState.MaxProjectileDamage,
            Mathf.Max(hp, previousHp + minimumIncrease));
    }

    private static int GetMinimumVisibleHpIncrease(int previousHp)
    {
        if (previousHp < ThousandHp)
            return 1;

        if (previousHp < TenThousandHp)
            return 100;

        if (previousHp < MillionHp)
            return ThousandHp;

        if (previousHp < TenMillionHp)
            return 100000;

        return MillionHp;
    }

    private IReadOnlyList<CocoonRewardProfile> GetCocoonProfiles()
    {
        return _rewardInstaller != null
            ? _rewardInstaller.CocoonProfiles
            : CocoonRewardProfile.Defaults;
    }
}
