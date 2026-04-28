using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight object pool for worm segments.
/// Maintains separate queues for head, body and tail segments
/// to avoid runtime allocations during worm generation.
/// </summary>
public sealed class WormSegmentPool
{
    private readonly Transform _parent;

    private readonly WormSegment _headPrefab;
    private readonly WormSegment _bodyPrefab;
    private readonly WormSegment _tailPrefab;

    private readonly Queue<WormSegment> _headPool = new();
    private readonly Queue<WormSegment> _bodyPool = new();
    private readonly Queue<WormSegment> _tailPool = new();
    private int _prewarmCreatedThisFrame;

    public WormSegmentPool(
        Transform parent,
        WormSegment head,
        WormSegment body,
        WormSegment tail)
    {
        _parent = parent;

        _headPrefab = head;
        _bodyPrefab = body;
        _tailPrefab = tail;
    }

    /// <summary>
    /// Instantiates a predefined number of pooled objects ahead of time
    /// to avoid runtime allocations during gameplay.
    /// </summary>
    public void Prewarm(int bodyCapacity)
    {
        Prewarm(_headPrefab, 1, _headPool);
        Prewarm(_tailPrefab, 1, _tailPool);

        Prewarm(_bodyPrefab, bodyCapacity, _bodyPool);
    }

    public IEnumerator PrewarmRoutine(int bodyCapacity, int batchSize)
    {
        int safeBatchSize = Mathf.Max(1, batchSize);
        _prewarmCreatedThisFrame = 0;

        yield return PrewarmRoutine(_headPrefab, 1, _headPool, safeBatchSize);
        yield return PrewarmRoutine(_tailPrefab, 1, _tailPool, safeBatchSize);
        yield return PrewarmRoutine(_bodyPrefab, bodyCapacity, _bodyPool, safeBatchSize);
    }

    /// <summary>
    /// Retrieves a segment instance from the pool or instantiates a new one
    /// if the pool is exhausted.
    /// </summary>
    public WormSegment Get(WormSegmentType type)
    {
        Queue<WormSegment> pool = GetPool(type);

        if (pool.Count > 0)
            return pool.Dequeue();

        WormSegment prefab = GetPrefab(type);

        if (prefab == null)
        {
            Debug.LogError($"Prefab for {type} is not assigned");
            return null;
        }

        WormSegment created = Object.Instantiate(prefab, _parent);
        created.gameObject.SetActive(false);

        return created;
    }

    private void Prewarm(WormSegment prefab, int count, Queue<WormSegment> pool)
    {
        if (prefab == null)
        {
            Debug.LogWarning("WormSegment prefab missing in pool");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            WormSegment seg = Object.Instantiate(prefab, _parent);

            seg.gameObject.SetActive(false);

            pool.Enqueue(seg);
        }
    }

    private IEnumerator PrewarmRoutine(
        WormSegment prefab,
        int count,
        Queue<WormSegment> pool,
        int batchSize)
    {
        if (prefab == null)
        {
            Debug.LogWarning("WormSegment prefab missing in pool");
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            WormSegment seg = Object.Instantiate(prefab, _parent);
            seg.gameObject.SetActive(false);
            pool.Enqueue(seg);

            _prewarmCreatedThisFrame++;

            if (_prewarmCreatedThisFrame >= batchSize)
            {
                _prewarmCreatedThisFrame = 0;
                yield return null;
            }
        }
    }

    private Queue<WormSegment> GetPool(WormSegmentType type) => type switch
    {
        WormSegmentType.Head => _headPool,
        WormSegmentType.Body => _bodyPool,
        WormSegmentType.Tail => _tailPool,
        _ => _bodyPool
    };

    private WormSegment GetPrefab(WormSegmentType type) => type switch
    {
        WormSegmentType.Head => _headPrefab,
        WormSegmentType.Body => _bodyPrefab,
        WormSegmentType.Tail => _tailPrefab,
        _ => _bodyPrefab
    };
}
