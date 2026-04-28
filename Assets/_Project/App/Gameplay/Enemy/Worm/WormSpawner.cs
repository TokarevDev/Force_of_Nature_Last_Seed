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

    [Header("Generation")]
    [Min(3)]
    [SerializeField] private int _totalLength = 60;

    [Header("Pooling")]
    [SerializeField] private int _poolPadding = 10;
    [SerializeField, Min(1)] private int _prewarmBatchSize = 64;

    private WormSegmentPool _segmentPool;
    private WormFactory _wormFactory;

    private bool _isSpawned;
    private int _bodyPoolCapacity;

    private void Awake()
    {
        if (_wormController == null)
            Debug.LogError("WormController not assigned", this);

        if (_wormCombat == null)
            Debug.LogError("WormCombatController not assigned", this);

        _bodyPoolCapacity = Mathf.Max(1, Mathf.Max(3, _totalLength) - 2 + _poolPadding);

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
            WormSectionBuilder.BuildSections(segments);

        AssignSectionsHP(sections);

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

        for (int i = 0; i < sections.Count; i++)
        {
            int hp = WormSectionHPGenerator.GetHP(i);
            sections[i].Init(hp);
        }
    }
}
