using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entry point for worm enemy generation.
/// Coordinates pattern creation, segment instantiation,
/// and initialization of movement and combat systems.
/// </summary>
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

    [Header("Generation")]
    [Min(3)]
    [SerializeField] private int _totalLength = 60;

    [Header("Cocoon spacing")]
    [SerializeField] private int _minBodyBeforeCocoon = 5;

    [SerializeField] private int _maxBodyBeforeCocoon = 10;

    [Header("Pooling")]
    [SerializeField] private int _poolPadding = 10;

    private WormSegmentPool _segmentPool;
    private WormFactory _wormFactory;

    private bool _isSpawned;

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController not assigned", this);

        if (_wormCombat == null)
            Debug.LogError("WormCombatController not assigned", this);

        if (_minBodyBeforeCocoon > _maxBodyBeforeCocoon)
            (_minBodyBeforeCocoon, _maxBodyBeforeCocoon) = (_maxBodyBeforeCocoon, _minBodyBeforeCocoon);

        int capacity = Mathf.Max(3, _totalLength) + _poolPadding;

        _segmentPool = new WormSegmentPool(
         transform,
         _headPrefab,
         _bodyPrefab,
          _tailPrefab);

        _segmentPool.Prewarm(capacity);

        _wormFactory = new WormFactory(_segmentPool);
    }

    private void Start()
    {
        SpawnWorm();
    }

    /// <summary>
    /// Generates the worm pattern and constructs the segment chain.
    /// Entry point responsible for spawning and initializing the worm enemy.
    /// </summary>
    public void SpawnWorm()
    {
        if (_isSpawned)
            return;

        List<WormPatternEntry> pattern =
            WormPatternBuilder.BuildPattern(
                _totalLength,
                _minBodyBeforeCocoon,
                _maxBodyBeforeCocoon);

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
            WormSectionBuilder.BuildSectionsByCocoons(segments);

        _wormFactory.AttachDamageReceivers(segments, _wormCombat);

        _wormController.Init(segments);
        _wormCombat.Init(head, tail, sections);

        _isSpawned = true;
    }
}