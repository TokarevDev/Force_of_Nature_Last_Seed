using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormSpawner : MonoBehaviour
{
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

        WeaponPowerSnapshot power = WeaponPowerEstimator.Estimate(_weapon, _acaciaThornWeapon);
        int totalSections = sections.Count;

        for (int i = 0; i < sections.Count; i++)
        {
            sections[i].Index = i;

            int baseHp = WormSectionHPGenerator.GetHP(i, _levelNumber);
            int hp = ResolveSectionHp(baseHp, i, totalSections, power);

            sections[i].Init(hp);
        }
    }

    public void SetRuntimePressureMultiplier(float multiplier)
    {
        float clampedMultiplier = Mathf.Max(1f, multiplier);

        if (Mathf.Approximately(_runtimePressureMultiplier, clampedMultiplier))
            return;

        _runtimePressureMultiplier = clampedMultiplier;
        RebalanceFutureSections();
    }

    private void OnWeaponRuntimeStatsChanged()
    {
        RebalanceFutureSections();
    }

    private void RebalanceFutureSections()
    {
        if (!_isSpawned || _sections.Count == 0)
            return;

        WeaponPowerSnapshot power = WeaponPowerEstimator.Estimate(_weapon, _acaciaThornWeapon);

        if (!power.IsValid)
            return;

        int totalSections = _sections.Count;

        for (int i = 0; i < _sections.Count; i++)
        {
            WormSection section = _sections[i];

            if (!CanRebalanceSection(section))
                continue;

            int baseHp = WormSectionHPGenerator.GetHP(i, _levelNumber);
            int hp = ResolveSectionHp(baseHp, i, totalSections, power);

            section.SetHp(hp);
        }
    }

    private int ResolveSectionHp(
        int baseHp,
        int sectionIndex,
        int totalSections,
        WeaponPowerSnapshot power)
    {
        if (_hpResolver == null)
            return baseHp;

        return _hpResolver.ResolveHp(
            baseHp,
            sectionIndex,
            totalSections,
            _levelNumber,
            power,
            _runtimePressureMultiplier);
    }

    private static bool CanRebalanceSection(WormSection section)
    {
        return section != null
            && !section.IsDestroyed
            && !section.HasTakenDamage
            && !section.HasVisibleAliveSegment();
    }

    private IReadOnlyList<CocoonRewardProfile> GetCocoonProfiles()
    {
        return _rewardInstaller != null
            ? _rewardInstaller.CocoonProfiles
            : CocoonRewardProfile.Defaults;
    }
}
