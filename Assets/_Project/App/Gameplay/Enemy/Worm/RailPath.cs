using UnityEngine;

public sealed class RailPath : MonoBehaviour
{
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _sampleStep = 0.1f;

    private Vector3[] _samples;
    private float[] _distances;
    private float _totalLength;

    public float TotalLength => _totalLength;

    private void Awake()
    {
        if (_waypoints == null || _waypoints.Length < 2)
        {
            Debug.LogError("RailPath requires at least 2 waypoints", this);
            return;
        }

        if (_sampleStep <= 0f)
            _sampleStep = 0.1f;

        CalculateDistances();
        BuildSamples();
    }

    private void CalculateDistances()
    {
        _distances = new float[_waypoints.Length];
        _distances[0] = 0f;

        for (int i = 1; i < _waypoints.Length; i++)
        {
            float d = Vector3.Distance(
                _waypoints[i - 1].position,
                _waypoints[i].position
            );

            _distances[i] = _distances[i - 1] + d;
        }

        _totalLength = _distances[^1];
    }

    private void BuildSamples()
    {
        int count = Mathf.CeilToInt(_totalLength / _sampleStep) + 2;

        _samples = new Vector3[count];

        float d = 0f;

        for (int i = 0; i < count; i++)
        {
            _samples[i] = GetPointRaw(d);
            d += _sampleStep;
        }
    }

    private Vector3 GetPointRaw(float distance)
    {
        distance = Mathf.Clamp(distance, 0, _totalLength);

        for (int i = 1; i < _distances.Length; i++)
        {
            if (distance <= _distances[i])
            {
                float segLength = _distances[i] - _distances[i - 1];
                float t = (distance - _distances[i - 1]) / segLength;

                return Vector3.Lerp(
                    _waypoints[i - 1].position,
                    _waypoints[i].position,
                    t
                );
            }
        }

        return _waypoints[^1].position;
    }

    public Vector3 GetPoint(float distance)
    {
        EnsureBuilt();

        distance = Mathf.Clamp(distance, 0, _totalLength);

        float fIndex = distance / _sampleStep;

        int index = Mathf.FloorToInt(fIndex);
        float t = fIndex - index;

        if (index >= _samples.Length - 1)
            return _samples[^1];

        return Vector3.Lerp(
            _samples[index],
            _samples[index + 1],
            t
        );
    }

    public float GetClosestDistance(Vector3 worldPosition)
    {
        EnsureBuilt();

        if (_samples == null || _samples.Length == 0)
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

    private void EnsureBuilt()
    {
        if (_samples != null && _samples.Length > 0)
            return;

        if (_waypoints == null || _waypoints.Length < 2)
            return;

        if (_sampleStep <= 0f)
            _sampleStep = 0.1f;

        CalculateDistances();
        BuildSamples();
    }
}
