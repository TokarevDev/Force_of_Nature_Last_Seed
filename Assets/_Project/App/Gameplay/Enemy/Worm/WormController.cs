using System;
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
    [Tooltip("RailPath control point index. Use RailPath Scene View point labels.")]
    [SerializeField][Min(0)] private int _catchUpRailPointIndex;
    [SerializeField][Min(0f)] private float _catchUpSpeed = 6f;
    [SerializeField][Min(0f)] private float _catchUpStopOffset = 0f;
    [SerializeField][Min(0f)] private float _catchUpExtraDistance = 1.5f;

    [Header("Combat Speed Bursts")]
    [SerializeField] private bool _enableCombatSpeedBursts = true;
    [SerializeField][Min(0f)] private float _combatBurstSpeed = 2f;
    [SerializeField][Min(0.1f)] private float _combatBurstInterval = 10f;
    [SerializeField][Min(0.1f)] private float _combatBurstDuration = 2.5f;

    [Header("Segments")]
    [SerializeField] private float _segmentSpacing = 0.5f;
    [SerializeField][Min(0.01f)] private float _tailVisualSpacingMultiplier = 1f;

    [Header("Optimization")]
    [SerializeField][Min(0f)] private float _activeDistancePadding = 0.5f;

    [Header("Wave")]
    [SerializeField] private float _waveAmplitude = 0.15f;

    [SerializeField] private float _waveFrequency = 6f;
    [SerializeField] private float _waveSpeed = 2f;

    [Header("Rollback")]
    [SerializeField] private float _rollbackSpeed = 8f;

    [Header("Revive")]
    [Tooltip("RailPath control point index. Set -1 to use Catch Up Rail Point Index.")]
    [SerializeField][Min(-1)] private int _reviveRollbackRailPointIndex = -1;
    [SerializeField][Min(0f)] private float _reviveRollbackSpeed = 35f;

    private readonly List<WormSegment> _segments = new();
    private readonly Dictionary<WormSegment, float> _rollbackAnchoredDistances = new();

    private float _headDistance;
    private Coroutine _rollbackRoutine;
    private Coroutine _reviveRollbackRoutine;
    private int _activeStartIndex = -1;
    private int _activeEndIndex = -1;

    private bool _isSectionRollback;
    private bool _isReviveRollback;
    private float _sectionRollbackTargetDistance;
    private bool _hasReachedCombatStart;
    private bool _hasReachedPathEnd;
    private float _combatBurstTimer;
    private float _combatBurstRemainingTime;

    private Vector3 _tmpEuler;
    private RailPath _cachedCatchUpRail;
    private int _cachedCatchUpRailPointIndex = -1;
    private float _cachedCatchUpRailPointDistance;
    private RailPath _cachedReviveRollbackRail;
    private int _cachedReviveRollbackRailPointIndex = -2;
    private float _cachedReviveRollbackRailPointDistance;

    public event Action PathCompleted;

    public bool HasWorm => _segments.Count > 0;
    public bool IsCatchingUpToCombatStart { get; private set; }

    public float HeadPathProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.TotalLength <= 0f)
                return 0f;

            return Mathf.Clamp01(_headDistance / _rail.TotalLength);
        }
    }

    public float HeadControlPointProgressNormalized
    {
        get
        {
            if (_rail == null || _rail.PointCount <= 1)
                return HeadPathProgressNormalized;

            return _rail.GetControlPointProgressNormalized(_headDistance);
        }
    }

    private void OnValidate()
    {
        if (_catchUpRailPointIndex < 0)
            _catchUpRailPointIndex = 0;

        if (_rail != null && _rail.PointCount > 0)
        {
            _catchUpRailPointIndex = Mathf.Min(_catchUpRailPointIndex, _rail.PointCount - 1);
            if (_reviveRollbackRailPointIndex >= 0)
            {
                _reviveRollbackRailPointIndex = Mathf.Min(
                    _reviveRollbackRailPointIndex,
                    _rail.PointCount - 1);
            }
        }

        ClearTargetDistanceCaches();
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
        _isReviveRollback = false;
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
        _hasReachedCombatStart = false;
        _hasReachedPathEnd = false;
        _combatBurstTimer = 0f;
        _combatBurstRemainingTime = 0f;
        ClearTargetDistanceCaches();
        IsCatchingUpToCombatStart = TryGetCatchUpTargetDistance(out _);

        UpdateSegments();
    }

    private void Update()
    {
        if (_segments.Count == 0 || _rail == null)
            return;

        if (!_isSectionRollback && !_isReviveRollback)
            MoveForward(Time.deltaTime);

        UpdateSegments();
    }

    private void MoveForward(float deltaTime)
    {
        if (deltaTime <= 0f || _rail.TotalLength <= 0f)
            return;

        float previousDistance = _headDistance;
        float targetDistance = _rail.TotalLength;

        _headDistance = Mathf.Min(
            targetDistance,
            _headDistance + GetForwardSpeed(deltaTime) * deltaTime);

        if (_hasReachedPathEnd)
            return;

        if (previousDistance < targetDistance && _headDistance >= targetDistance)
        {
            _hasReachedPathEnd = true;
            PathCompleted?.Invoke();
        }
    }

    private float GetForwardSpeed(float deltaTime)
    {
        IsCatchingUpToCombatStart = ShouldCatchUp();

        if (IsCatchingUpToCombatStart)
            return Mathf.Max(_speed, _catchUpSpeed);

        UpdateCombatBurst(deltaTime);

        if (_combatBurstRemainingTime > 0f)
            return Mathf.Max(_speed, _combatBurstSpeed);

        return _speed;
    }

    private bool ShouldCatchUp()
    {
        if (_rail == null)
            return false;

        if (!TryGetCatchUpTargetDistance(out float targetDistance))
            return false;

        targetDistance = Mathf.Max(
            0f,
            targetDistance - _catchUpStopOffset + _catchUpExtraDistance);

        return _headDistance < targetDistance;
    }

    private bool TryGetCatchUpTargetDistance(out float targetDistance)
    {
        targetDistance = 0f;

        if (_rail == null)
            return false;

        if (_cachedCatchUpRail == _rail &&
            _cachedCatchUpRailPointIndex == _catchUpRailPointIndex)
        {
            targetDistance = _cachedCatchUpRailPointDistance;
            return true;
        }

        if (!_rail.TryGetControlPointDistance(_catchUpRailPointIndex, out targetDistance))
            return false;

        _cachedCatchUpRail = _rail;
        _cachedCatchUpRailPointIndex = _catchUpRailPointIndex;
        _cachedCatchUpRailPointDistance = targetDistance;

        return true;
    }

    private bool TryGetReviveRollbackTargetDistance(out float targetDistance)
    {
        targetDistance = 0f;

        if (_rail == null)
            return false;

        int targetIndex = _reviveRollbackRailPointIndex >= 0
            ? _reviveRollbackRailPointIndex
            : _catchUpRailPointIndex;

        if (_cachedReviveRollbackRail == _rail &&
            _cachedReviveRollbackRailPointIndex == targetIndex)
        {
            targetDistance = _cachedReviveRollbackRailPointDistance;
            return true;
        }

        if (!_rail.TryGetControlPointDistance(targetIndex, out targetDistance))
            return false;

        _cachedReviveRollbackRail = _rail;
        _cachedReviveRollbackRailPointIndex = targetIndex;
        _cachedReviveRollbackRailPointDistance = targetDistance;

        return true;
    }

    private void ClearTargetDistanceCaches()
    {
        _cachedCatchUpRail = null;
        _cachedCatchUpRailPointIndex = -1;
        _cachedCatchUpRailPointDistance = 0f;
        _cachedReviveRollbackRail = null;
        _cachedReviveRollbackRailPointIndex = -2;
        _cachedReviveRollbackRailPointDistance = 0f;
    }

    private void UpdateCombatBurst(float deltaTime)
    {
        if (!_enableCombatSpeedBursts || deltaTime <= 0f)
            return;

        if (!_hasReachedCombatStart)
        {
            _hasReachedCombatStart = true;
            _combatBurstTimer = 0f;
            _combatBurstRemainingTime = 0f;
            return;
        }

        if (_combatBurstRemainingTime > 0f)
        {
            _combatBurstRemainingTime = Mathf.Max(
                0f,
                _combatBurstRemainingTime - deltaTime);
            return;
        }

        _combatBurstTimer += deltaTime;

        if (_combatBurstTimer < _combatBurstInterval)
            return;

        _combatBurstTimer = 0f;
        _combatBurstRemainingTime = _combatBurstDuration;
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

            float distance = GetSegmentDistance(i, segment);
            Vector3 position = CalculatePositionAtDistance(distance, waveTime);

            UpdateSegmentPosition(segment, position);

            if (i > startIndex && !segment.HasTailVisualChain)
                UpdateSegmentRotation(i, segment, position);

            UpdateTailVisualChain(i, segment, distance, waveTime);
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

            float distance = GetSegmentDistance(i, segment);

            if (distance < 0f || distance > maxDistance)
            {
                segment.SetRuntimeVisible(false);
                continue;
            }

            Vector3 position = CalculatePositionAtDistance(distance, waveTime);
            UpdateSegmentPosition(segment, position);

            if (i > 0 && !segment.HasTailVisualChain)
                UpdateSegmentRotation(i, segment, position);

            UpdateTailVisualChain(i, segment, distance, waveTime);
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

    private Vector3 CalculatePositionAtDistance(float distance, float waveTime)
    {
        Vector3 pos = _rail.GetPoint(distance);

        float wave = Mathf.Sin(distance * _waveFrequency + waveTime);
        pos.y += wave * _waveAmplitude;

        return pos;
    }

    private void UpdateTailVisualChain(
        int index,
        WormSegment segment,
        float tailDistance,
        float waveTime)
    {
        if (segment == null || !segment.HasTailVisualChain)
            return;

        segment.ResetTailVisualRootRotation();

        float spacing = Mathf.Max(0.01f, _segmentSpacing * _tailVisualSpacingMultiplier);
        Vector3 leaderPosition = ResolveTailLeaderPosition(index, segment);
        Vector3 previousPosition = leaderPosition;

        for (int i = 0; i < segment.TailVisualPartCount; i++)
        {
            float visualDistance = Mathf.Max(0f, tailDistance - (i * spacing));
            Vector3 visualPosition = CalculatePositionAtDistance(visualDistance, waveTime);
            float angle = CalculateLookAngle(visualPosition, previousPosition);

            segment.SetTailVisualPartPose(i, visualPosition, angle);
            previousPosition = visualPosition;
        }
    }

    private Vector3 ResolveTailLeaderPosition(int index, WormSegment tail)
    {
        int previousIndex = index - 1;

        if (previousIndex >= 0 && previousIndex < _segments.Count)
        {
            WormSegment previous = _segments[previousIndex];

            if (previous != null)
                return previous.CachedTransform.position;
        }

        return tail.CachedTransform.position;
    }

    private static float CalculateLookAngle(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;

        if (dir.sqrMagnitude <= 0.0001f)
            return 0f;

        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
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
    /// During rollback, detached rear segments keep their anchor distance
    /// until the moving front chain reaches and reconnects with them.
    /// </summary>
    private float GetSegmentDistance(int index, WormSegment segment)
    {
        float distance = _headDistance - (index * _segmentSpacing);

        if (!_isSectionRollback || segment == null)
            return distance;

        return _rollbackAnchoredDistances.TryGetValue(segment, out float anchoredDistance)
            ? Mathf.Min(distance, anchoredDistance)
            : distance;
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

        for (int i = 0; i < destroyed.Count; i++)
        {
            WormSegment segment = destroyed[i];

            if (segment != null)
                _rollbackAnchoredDistances.Remove(segment);
        }

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

        if (_isReviveRollback)
            return;

        float rollbackDistance = destroyedCount * Mathf.Max(0.01f, _segmentSpacing);
        bool rollbackInProgress = _isSectionRollback || _rollbackRoutine != null;

        AnchorRollbackTail(splitIndex, destroyedCount);

        _sectionRollbackTargetDistance = Mathf.Max(
            0f,
            (rollbackInProgress
                ? _sectionRollbackTargetDistance
                : _headDistance) - rollbackDistance);

        if (rollbackInProgress)
            return;

        _rollbackRoutine = StartCoroutine(SectionRollbackRoutine());
    }

    public bool RollbackToReviveStart(Action onComplete)
    {
        if (_segments.Count == 0 || _rail == null)
            return false;

        float target = GetReviveRollbackTargetDistance();

        if (_reviveRollbackRoutine != null)
            StopCoroutine(_reviveRollbackRoutine);

        if (_rollbackRoutine != null)
        {
            StopCoroutine(_rollbackRoutine);
            _rollbackRoutine = null;
        }

        ClearSectionRollbackState();
        _isReviveRollback = false;
        _hasReachedPathEnd = false;

        if (_headDistance <= target)
        {
            _headDistance = target;
            UpdateSegments();
            onComplete?.Invoke();
            return true;
        }

        _reviveRollbackRoutine = StartCoroutine(ReviveRollbackRoutine(target, onComplete));
        return true;
    }

    /// <summary>
    /// Performs smooth rollback of the worm head until
    /// the destroyed gap is closed.
    /// Additional destroyed sections can extend the target distance
    /// without restarting the animation.
    /// Uses unscaled time so the chain can visually reconnect while
    /// the reward popup keeps gameplay paused through Time.timeScale.
    /// </summary>
    private IEnumerator SectionRollbackRoutine()
    {
        _isSectionRollback = true;

        while (_headDistance > _sectionRollbackTargetDistance)
        {
            float target = _sectionRollbackTargetDistance;

            _headDistance = Mathf.MoveTowards(
                _headDistance,
                target,
                _rollbackSpeed * Time.unscaledDeltaTime
            );

            UpdateSegments();
            yield return null;
        }

        _headDistance = _sectionRollbackTargetDistance;
        UpdateSegments();

        _isSectionRollback = false;
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
        _rollbackRoutine = null;
    }

    private IEnumerator ReviveRollbackRoutine(float target, Action onComplete)
    {
        _isReviveRollback = true;
        _activeStartIndex = -1;
        _activeEndIndex = -1;

        while (_headDistance > target)
        {
            _headDistance = Mathf.MoveTowards(
                _headDistance,
                target,
                _reviveRollbackSpeed * Time.unscaledDeltaTime);

            UpdateSegments();
            yield return null;
        }

        _headDistance = target;
        UpdateSegments();

        _isReviveRollback = false;
        _reviveRollbackRoutine = null;
        onComplete?.Invoke();
    }

    private float GetReviveRollbackTargetDistance()
    {
        if (_rail == null)
            return 0f;

        return TryGetReviveRollbackTargetDistance(out float targetDistance)
            ? Mathf.Clamp(targetDistance, 0f, _rail.TotalLength)
            : 0f;
    }

    private void ClearSectionRollbackState()
    {
        _isSectionRollback = false;
        _activeStartIndex = -1;
        _activeEndIndex = -1;
        _rollbackAnchoredDistances.Clear();
        _sectionRollbackTargetDistance = 0f;
    }

    private void AnchorRollbackTail(int splitIndex, int destroyedCount)
    {
        float spacing = Mathf.Max(0.01f, _segmentSpacing);
        int startIndex = Mathf.Clamp(splitIndex, 0, _segments.Count);

        for (int i = startIndex; i < _segments.Count; i++)
        {
            WormSegment segment = _segments[i];

            if (segment == null || _rollbackAnchoredDistances.ContainsKey(segment))
                continue;

            float anchoredDistance = _headDistance - ((i + destroyedCount) * spacing);
            _rollbackAnchoredDistances.Add(segment, anchoredDistance);
        }

        _activeStartIndex = -1;
        _activeEndIndex = -1;
    }
}
