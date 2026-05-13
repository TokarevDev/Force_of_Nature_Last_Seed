using System.Collections.Generic;
using UnityEngine;

public enum RailPathInterpolationMode
{
    Linear = 0,
    Smooth = 1
}

[DisallowMultipleComponent]
public sealed class RailPath : MonoBehaviour
{
    private const float DefaultSampleStep = 0.1f;
    private const float MinSampleStep = 0.01f;
    private const float MinSegmentLength = 0.0001f;

    [SerializeField] private List<Vector3> _localPoints = new();
    [SerializeField][Min(MinSampleStep)] private float _sampleStep = DefaultSampleStep;
    [SerializeField] private RailPathInterpolationMode _interpolationMode = RailPathInterpolationMode.Linear;

    [Header("Smoothing")]
    [SerializeField][Min(0f)] private float _cornerRadius = 0.35f;
    [SerializeField][Range(2, 16)] private int _cornerSamples = 6;

    [SerializeField][HideInInspector] private Transform[] _waypoints;

    private Vector3[] _worldPoints;
    private Vector3[] _samples;
    private float[] _distances;
    private float _totalLength;

    public int PointCount => _localPoints != null ? _localPoints.Count : 0;
    public float TotalLength => _totalLength;

    private void Reset()
    {
        _localPoints = new List<Vector3>
        {
            new(-1f, 0f, 0f),
            new(1f, 0f, 0f)
        };

        Invalidate();
    }

    private void Awake()
    {
        if (!EnsureBuilt())
        {
            Debug.LogError("RailPath requires at least 2 points.", this);
            return;
        }

        if (_totalLength <= MinSegmentLength)
            Debug.LogError("RailPath total length must be greater than zero.", this);
    }

    private void OnValidate()
    {
        if (_localPoints == null)
            _localPoints = new List<Vector3>();

        if (_sampleStep < MinSampleStep)
            _sampleStep = DefaultSampleStep;

        if (_cornerRadius < 0f)
            _cornerRadius = 0f;

        _cornerSamples = Mathf.Clamp(_cornerSamples, 2, 16);

        Invalidate();
    }

    public Vector3 GetPoint(float distance)
    {
        if (!EnsureBuilt())
            return transform.position;

        distance = Mathf.Clamp(distance, 0f, _totalLength);

        float fIndex = distance / _sampleStep;
        int index = Mathf.FloorToInt(fIndex);
        float t = fIndex - index;

        if (index >= _samples.Length - 1)
            return _samples[^1];

        return Vector3.Lerp(
            _samples[index],
            _samples[index + 1],
            t);
    }

    public float GetClosestDistance(Vector3 worldPosition)
    {
        if (!EnsureBuilt() || _samples == null || _samples.Length == 0)
            return 0f;

        int closestIndex = 0;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < _samples.Length; i++)
        {
            float sqrDistance = Vector3.SqrMagnitude(_samples[i] - worldPosition);
            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestIndex = i;
        }

        return Mathf.Clamp(closestIndex * _sampleStep, 0f, _totalLength);
    }

    public bool TryGetControlPointDistance(int pointIndex, out float distance)
    {
        distance = 0f;

        if (!TryGetControlPointWorldPosition(pointIndex, out Vector3 worldPosition))
            return false;

        distance = GetClosestDistance(worldPosition);
        return true;
    }

    public bool TryGetControlPointWorldPosition(int pointIndex, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (pointIndex < 0)
            return false;

        if (_localPoints != null && _localPoints.Count >= 2)
        {
            if (pointIndex >= _localPoints.Count)
                return false;

            worldPosition = transform.TransformPoint(_localPoints[pointIndex]);
            return true;
        }

        if (_waypoints == null)
            return false;

        int validPointIndex = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
                continue;

            if (validPointIndex == pointIndex)
            {
                worldPosition = waypoint.position;
                return true;
            }

            validPointIndex++;
        }

        return false;
    }

    private bool EnsureBuilt()
    {
        if (_samples != null && _samples.Length > 0)
            return true;

        if (!TryBuildWorldPoints())
            return false;

        Vector3[] pathPoints = BuildPathPoints();
        if (pathPoints == null || pathPoints.Length < 2)
            return false;

        _sampleStep = Mathf.Max(MinSampleStep, _sampleStep);
        CalculateDistances(pathPoints);
        BuildSamples(pathPoints);

        return _samples != null && _samples.Length > 0;
    }

    private bool TryBuildWorldPoints()
    {
        if (_localPoints != null && _localPoints.Count >= 2)
        {
            _worldPoints = new Vector3[_localPoints.Count];

            for (int i = 0; i < _localPoints.Count; i++)
                _worldPoints[i] = transform.TransformPoint(_localPoints[i]);

            return true;
        }

        int legacyWaypointCount = CountValidLegacyWaypoints();
        if (legacyWaypointCount < 2)
            return false;

        _worldPoints = new Vector3[legacyWaypointCount];

        int pointIndex = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
                continue;

            _worldPoints[pointIndex] = waypoint.position;
            pointIndex++;
        }

        return true;
    }

    private Vector3[] BuildPathPoints()
    {
        if (_interpolationMode != RailPathInterpolationMode.Smooth ||
            _worldPoints.Length < 3 ||
            _cornerRadius <= MinSegmentLength)
        {
            return _worldPoints;
        }

        return BuildSmoothedPathPoints();
    }

    private Vector3[] BuildSmoothedPathPoints()
    {
        var points = new List<Vector3>(_worldPoints.Length * (_cornerSamples + 1));
        AddPointIfSeparated(points, _worldPoints[0]);

        for (int i = 1; i < _worldPoints.Length - 1; i++)
        {
            Vector3 previous = _worldPoints[i - 1];
            Vector3 corner = _worldPoints[i];
            Vector3 next = _worldPoints[i + 1];

            float previousLength = Vector3.Distance(previous, corner);
            float nextLength = Vector3.Distance(corner, next);
            float cornerDistance = Mathf.Min(
                _cornerRadius,
                previousLength * 0.45f,
                nextLength * 0.45f);

            if (cornerDistance <= MinSegmentLength)
            {
                AddPointIfSeparated(points, corner);
                continue;
            }

            Vector3 entry = corner + (previous - corner).normalized * cornerDistance;
            Vector3 exit = corner + (next - corner).normalized * cornerDistance;

            AddPointIfSeparated(points, entry);

            for (int sample = 1; sample <= _cornerSamples; sample++)
            {
                float t = sample / (float)_cornerSamples;
                AddPointIfSeparated(points, EvaluateQuadraticBezier(entry, corner, exit, t));
            }
        }

        AddPointIfSeparated(points, _worldPoints[^1]);
        return points.ToArray();
    }

    private static Vector3 EvaluateQuadraticBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float inverseT = 1f - t;

        return inverseT * inverseT * start +
               2f * inverseT * t * control +
               t * t * end;
    }

    private static void AddPointIfSeparated(List<Vector3> points, Vector3 point)
    {
        if (points.Count > 0 &&
            Vector3.SqrMagnitude(points[^1] - point) <= MinSegmentLength * MinSegmentLength)
        {
            return;
        }

        points.Add(point);
    }

    private void CalculateDistances(Vector3[] pathPoints)
    {
        _distances = new float[pathPoints.Length];
        _distances[0] = 0f;

        for (int i = 1; i < pathPoints.Length; i++)
        {
            float distance = Vector3.Distance(
                pathPoints[i - 1],
                pathPoints[i]);

            _distances[i] = _distances[i - 1] + distance;
        }

        _totalLength = _distances[^1];
    }

    private void BuildSamples(Vector3[] pathPoints)
    {
        if (_totalLength <= MinSegmentLength)
        {
            _samples = new[]
            {
                pathPoints[0],
                pathPoints[^1]
            };

            return;
        }

        int count = Mathf.Max(2, Mathf.CeilToInt(_totalLength / _sampleStep) + 1);
        _samples = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float distance = Mathf.Min(i * _sampleStep, _totalLength);
            _samples[i] = GetPointRaw(pathPoints, distance);
        }
    }

    private Vector3 GetPointRaw(Vector3[] pathPoints, float distance)
    {
        distance = Mathf.Clamp(distance, 0f, _totalLength);

        for (int i = 1; i < _distances.Length; i++)
        {
            if (distance > _distances[i])
                continue;

            float segmentLength = _distances[i] - _distances[i - 1];
            if (segmentLength <= MinSegmentLength)
                return pathPoints[i];

            float t = (distance - _distances[i - 1]) / segmentLength;

            return Vector3.Lerp(
                pathPoints[i - 1],
                pathPoints[i],
                t);
        }

        return pathPoints[^1];
    }

    private int CountValidLegacyWaypoints()
    {
        if (_waypoints == null)
            return 0;

        int count = 0;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] != null)
                count++;
        }

        return count;
    }

    private void Invalidate()
    {
        _worldPoints = null;
        _samples = null;
        _distances = null;
        _totalLength = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (_localPoints == null || _localPoints.Count < 2)
        {
            DrawLegacyWaypointGizmos();
            return;
        }

        Gizmos.color = new Color(0.1f, 1f, 0.25f, 0.9f);

        Vector3[] previewPoints = GetPreviewWorldPoints();
        if (previewPoints == null || previewPoints.Length < 2)
            return;

        Vector3 previous = previewPoints[0];
        Gizmos.DrawSphere(previous, 0.08f);

        for (int i = 1; i < previewPoints.Length; i++)
        {
            Vector3 current = previewPoints[i];
            Gizmos.DrawLine(previous, current);
            previous = current;
        }

        for (int i = 0; i < _localPoints.Count; i++)
            Gizmos.DrawSphere(transform.TransformPoint(_localPoints[i]), 0.08f);
    }

    private void DrawLegacyWaypointGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2)
            return;

        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.9f);

        Transform previous = null;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform current = _waypoints[i];
            if (current == null)
                continue;

            Gizmos.DrawSphere(current.position, 0.08f);

            if (previous != null)
                Gizmos.DrawLine(previous.position, current.position);

            previous = current;
        }
    }

#if UNITY_EDITOR
    public int LegacyWaypointCount => CountValidLegacyWaypoints();

    public int ChildTransformCount => transform.childCount;

    public Vector3 GetEditorWorldPoint(int index)
    {
        return transform.TransformPoint(_localPoints[index]);
    }

    public Vector3[] GetEditorPreviewWorldPoints()
    {
        return GetPreviewWorldPoints();
    }

    public void SetEditorWorldPoint(int index, Vector3 worldPosition)
    {
        EnsureLocalPoints();

        if (index < 0 || index >= _localPoints.Count)
            return;

        _localPoints[index] = transform.InverseTransformPoint(worldPosition);
        Invalidate();
    }

    public void AddEditorWorldPoint(Vector3 worldPosition)
    {
        EnsureLocalPoints();
        _localPoints.Add(transform.InverseTransformPoint(worldPosition));
        Invalidate();
    }

    public void InsertEditorWorldPoint(int index, Vector3 worldPosition)
    {
        EnsureLocalPoints();

        index = Mathf.Clamp(index, 0, _localPoints.Count);
        _localPoints.Insert(index, transform.InverseTransformPoint(worldPosition));
        Invalidate();
    }

    public void RemoveEditorPointAt(int index)
    {
        EnsureLocalPoints();

        if (index < 0 || index >= _localPoints.Count)
            return;

        _localPoints.RemoveAt(index);
        Invalidate();
    }

    public void ReverseEditorPoints()
    {
        EnsureLocalPoints();
        _localPoints.Reverse();
        Invalidate();
    }

    public void ClearEditorPoints()
    {
        EnsureLocalPoints();
        _localPoints.Clear();
        Invalidate();
    }

    public void FlattenEditorLocalZ()
    {
        EnsureLocalPoints();

        for (int i = 0; i < _localPoints.Count; i++)
        {
            Vector3 point = _localPoints[i];
            point.z = 0f;
            _localPoints[i] = point;
        }

        Invalidate();
    }

    public int ImportLegacyWaypointsToLocalPoints()
    {
        int count = CountValidLegacyWaypoints();
        if (count < 2)
            return 0;

        EnsureLocalPoints();
        _localPoints.Clear();

        for (int i = 0; i < _waypoints.Length; i++)
        {
            Transform waypoint = _waypoints[i];
            if (waypoint == null)
                continue;

            _localPoints.Add(transform.InverseTransformPoint(waypoint.position));
        }

        Invalidate();
        return _localPoints.Count;
    }

    public int ImportChildTransformsToLocalPoints()
    {
        int childCount = transform.childCount;
        if (childCount < 2)
            return 0;

        EnsureLocalPoints();
        _localPoints.Clear();

        for (int i = 0; i < childCount; i++)
            _localPoints.Add(transform.InverseTransformPoint(transform.GetChild(i).position));

        Invalidate();
        return _localPoints.Count;
    }

    public void ClearLegacyWaypoints()
    {
        _waypoints = null;
        Invalidate();
    }

    private void EnsureLocalPoints()
    {
        if (_localPoints == null)
            _localPoints = new List<Vector3>();
    }
#endif

    private Vector3[] GetPreviewWorldPoints()
    {
        if (!TryBuildWorldPoints())
            return null;

        return BuildPathPoints();
    }
}
