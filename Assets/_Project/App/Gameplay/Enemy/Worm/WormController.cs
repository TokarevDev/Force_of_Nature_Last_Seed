using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls movement and positioning of the worm segments along a rail path.
///
/// The worm is represented as a chain of segments that follow the head
/// using a fixed spacing distance.
///
/// Movement is rail-based which allows efficient positioning without
/// physics simulation.
///
/// The controller also handles the rollback mechanic which occurs when
/// a group of segments is destroyed. In this case the worm head moves
/// backwards until the remaining segments reconnect.
/// </summary>
public sealed class WormController : MonoBehaviour
{
    [Header("Rail")]
    [SerializeField] private RailPath _rail;

    [Header("Movement")]
    [SerializeField] private float _speed = 3f;

    [Header("Catch Up")]
    [SerializeField] private Transform _catchUpTarget;
    [SerializeField][Min(0f)] private float _catchUpSpeed = 6f;
    [SerializeField][Min(0f)] private float _catchUpStopOffset = 0f;

    [Header("Segments")]
    [SerializeField] private float _segmentSpacing = 0.5f;

    [Header("Optimization")]
    [SerializeField][Min(0f)] private float _activeDistancePadding = 0.5f;

    [Header("Wave")]
    [SerializeField] private float _waveAmplitude = 0.15f;

    [SerializeField] private float _waveFrequency = 6f;
    [SerializeField] private float _waveSpeed = 2f;

    [Header("Rollback")]
    [SerializeField] private float _rollbackSpeed = 8f;

    private readonly List<WormSegment> _segments = new();

    private float _headDistance;
    private Coroutine _rollbackRoutine;
    private int _activeStartIndex = -1;
    private int _activeEndIndex = -1;

    private bool _isSectionRollback;
    private int _rollbackSplitIndex = -1;
    private int _rollbackRemovedCount;
    private float _rollbackStartHeadDistance;

    private Vector3 _tmpEuler;

    public bool HasWorm => _segments.Count > 0;

    public float HeadPathProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(_headDistance / _rail.TotalLength);
        }
    }

    /// <summary>
    /// Initializes worm movement with the generated segment list.
    /// Called by WormSpawner after all segments are created.
    /// </summary>
    public void Init(List<WormSegment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);

        _headDistance = 0f;
        _activeStartIndex = -1;
        _activeEndIndex = -1;

        _isSectionRollback = false;
        _rollbackSplitIndex = -1;
        _rollbackRemovedCount = 0;
        _rollbackStartHeadDistance = 0f;

        UpdateSegments();
    }

    private void Update()
    {
        if (_segments.Count == 0 || _rail == null)
            return;

        if (!_isSectionRollback)
            _headDistance += GetForwardSpeed() * Time.deltaTime;

        UpdateSegments();
    }

    private float GetForwardSpeed()
    {
        if (!ShouldCatchUp())
            return _speed;

        return Mathf.Max(_speed, _catchUpSpeed);
    }

    private bool ShouldCatchUp()
    {
        if (_catchUpTarget == null || _rail == null)
            return false;

        float targetDistance = _rail.GetClosestDistance(_catchUpTarget.position);
        targetDistance = Mathf.Max(0f, targetDistance - _catchUpStopOffset);

        return _headDistance < targetDistance;
    }

    /// <summary>
    /// Updates position and rotation of all worm segments.
    /// Each segment samples a position along the rail using
    /// its offset distance relative to the head.
    /// </summary>
    private void UpdateSegments()
    {
        if (_isSectionRollback)
        {
            UpdateSegmentsDuringRollback();
            return;
        }

        if (!TryGetActiveRange(out int startIndex, out int endIndex))
        {
            HidePreviousActiveRange(-1, -1);
            return;
        }

        HidePreviousActiveRange(startIndex, endIndex);

        float waveTime = Time.time * _waveSpeed;

        for (int i = startIndex; i <= endIndex; i++)
        {
            WormSegment segment = _segments[i];
            if (segment == null)
                continue;

            Vector3 position = CalculateSegmentPosition(i, waveTime);

            UpdateSegmentPosition(segment, position);

            if (i > startIndex)
                UpdateSegmentRotation(i, segment, position);

            segment.SetRuntimeVisible(true);
        }

        _activeStartIndex = startIndex;
        _activeEndIndex = endIndex;
    }

    private void UpdateSegmentsDuringRollback()
    {
        float waveTime = Time.time * _waveSpeed;
        float maxDistance = _rail.TotalLength + _activeDistancePadding;

        for (int i = 0; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null)
                continue;

            float distance = GetSegmentDistance(i);

            if (distance < 0f || distance > maxDistance)
            {
                segment.SetRuntimeVisible(false);
                continue;
            }

            Vector3 position = CalculateSegmentPosition(i, waveTime);
            UpdateSegmentPosition(segment, position);

            if (i > 0)
                UpdateSegmentRotation(i, segment, position);

            segment.SetRuntimeVisible(true);
        }

        _activeStartIndex = -1;
        _activeEndIndex = -1;
    }

    private bool TryGetActiveRange(out int startIndex, out int endIndex)
    {
        startIndex = -1;
        endIndex = -1;

        if (_segments.Count == 0 || _rail == null)
            return false;

        float spacing = Mathf.Max(0.01f, _segmentSpacing);
        float maxDistance = _rail.TotalLength + _activeDistancePadding;

        startIndex = Mathf.Max(0, Mathf.CeilToInt((_headDistance - maxDistance) / spacing));
        endIndex = Mathf.Min(_segments.Count - 1, Mathf.FloorToInt(_headDistance / spacing));

        return startIndex <= endIndex;
    }

    private void HidePreviousActiveRange(int nextStartIndex, int nextEndIndex)
    {
        if (_activeStartIndex < 0 || _activeEndIndex < _activeStartIndex)
            return;

        for (int i = _activeStartIndex; i <= _activeEndIndex; i++)
        {
            if (i >= nextStartIndex && i <= nextEndIndex)
                continue;

            if (i < 0 || i >= _segments.Count)
                continue;

            WormSegment segment = _segments[i];

            if (segment != null)
                segment.SetRuntimeVisible(false);
        }
    }

    /// <summary>
    /// Calculates world position for a segment along the rail
    /// including the procedural wave offset.
    /// </summary>
    private Vector3 CalculateSegmentPosition(int index, float waveTime)
    {
        float distance = GetSegmentDistance(index);

        Vector3 pos = _rail.GetPoint(distance);

        float wave = Mathf.Sin(distance * _waveFrequency + waveTime);
        pos.y += wave * _waveAmplitude;

        return pos;
    }

    /// <summary>
    /// Moves segment transform if the position changed.
    /// Small threshold avoids unnecessary transform updates.
    /// </summary>
    private void UpdateSegmentPosition(WormSegment segment, Vector3 pos)
    {
        Transform tr = segment.CachedTransform;
        Vector3 currentPos = tr.position;

        if ((currentPos - pos).sqrMagnitude > 0.000001f)
            tr.position = pos;
    }

    /// <summary>
    /// Rotates segment visual towards the previous segment
    /// creating the worm bending effect.
    /// </summary>
    private void UpdateSegmentRotation(int index, WormSegment segment, Vector3 pos)
    {
        WormSegment previous = _segments[index - 1];
        if (previous == null)
            return;

        Vector3 dir = previous.CachedTransform.position - pos;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Transform visual = segment.VisualRoot;
        if (visual == null)
            return;

        Vector3 currentEuler = visual.localEulerAngles;

        if (Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, angle)) > 0.1f)
        {
            _tmpEuler.z = angle;
            visual.localEulerAngles = _tmpEuler;
        }
    }

    /// <summary>
    /// Calculates rail distance for the specified segment index.
    /// During rollback a split logic is used to keep front and rear
    /// segments visually separated until rollback finishes.
    /// </summary>
    private float GetSegmentDistance(int index)
    {
        if (!_isSectionRollback)
            return _headDistance - (index * _segmentSpacing);

        if (index < _rollbackSplitIndex)
            return _headDistance - (index * _segmentSpacing);

        return _rollbackStartHeadDistance - ((index + _rollbackRemovedCount) * _segmentSpacing);
    }

    /// <summary>
    /// Removes destroyed segments from the internal segment list
    /// and returns how many segments were removed.
    /// </summary>
    public int RemoveDestroyedSectionSegments(List<WormSegment> destroyed, out int firstRemovedIndex)
    {
        firstRemovedIndex = -1;

        if (destroyed == null || destroyed.Count == 0)
            return 0;

        HashSet<WormSegment> destroyedSet = new(destroyed);

        for (int i = 0; i < _segments.Count; i++)
        {
            if (destroyedSet.Contains(_segments[i]))
            {
                firstRemovedIndex = i;
                break;
            }
        }

        int removed = _segments.RemoveAll(seg => seg != null && destroyedSet.Contains(seg));

        return removed;
    }

    /// <summary>
    /// Starts rollback movement after a section of segments
    /// has been destroyed.
    /// </summary>
    public void RollbackDestroyedGap(int destroyedCount, int splitIndex)
    {
        if (destroyedCount <= 0)
            return;

        if (splitIndex < 0)
            return;

        _rollbackSplitIndex = splitIndex;
        _rollbackRemovedCount = destroyedCount;
        _rollbackStartHeadDistance = _headDistance;

        if (_rollbackRoutine != null)
            StopCoroutine(_rollbackRoutine);

        float rollbackDistance = destroyedCount * _segmentSpacing;
        _rollbackRoutine = StartCoroutine(SectionRollbackRoutine(rollbackDistance));
    }

    /// <summary>
    /// Performs smooth rollback of the worm head until
    /// the destroyed gap is closed.
    /// Uses unscaled time so the chain can visually reconnect while
    /// the reward popup keeps gameplay paused through Time.timeScale.
    /// </summary>
    private IEnumerator SectionRollbackRoutine(float rollbackDistance)
    {
        _isSectionRollback = true;

        float target = Mathf.Max(0f, _rollbackStartHeadDistance - rollbackDistance);

        while (_headDistance > target)
        {
            _headDistance = Mathf.MoveTowards(
                _headDistance,
                target,
                _rollbackSpeed * Time.unscaledDeltaTime
            );

            UpdateSegments();
            yield return null;
        }

        _headDistance = target;
        UpdateSegments();

        _isSectionRollback = false;
        _rollbackSplitIndex = -1;
        _rollbackRemovedCount = 0;
    }
}
