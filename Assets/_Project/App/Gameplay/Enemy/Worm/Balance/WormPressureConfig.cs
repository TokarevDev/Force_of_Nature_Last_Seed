using UnityEngine;

[CreateAssetMenu(menuName = "Game/Worm/Pressure Config")]
public sealed class WormPressureConfig : ScriptableObject
{
    [SerializeField] private bool _enabled = true;

    [Header("Progress Target")]
    [SerializeField][Range(0f, 1f)] private float _targetHeadProgress = 0.55f;
    [SerializeField][Min(1f)] private float _targetReachTime = 75f;
    [SerializeField][Min(0f)] private float _startDelay = 1.5f;
    [SerializeField][Range(0f, 0.25f)] private float _progressDeadZone = 0.02f;

    [Header("Runtime Pressure")]
    [SerializeField][Min(0.1f)] private float _sampleInterval = 1f;
    [SerializeField][Min(0f)] private float _increasePerSample = 0.11f;
    [SerializeField][Min(0f)] private float _recoveryPerSample = 0.01f;
    [SerializeField][Min(1f)] private float _maxMultiplier = 5.5f;

    public bool Enabled => _enabled;
    public float StartDelay => _startDelay;
    public float ProgressDeadZone => _progressDeadZone;
    public float SampleInterval => _sampleInterval;
    public float IncreasePerSample => _increasePerSample;
    public float RecoveryPerSample => _recoveryPerSample;
    public float MaxMultiplier => _maxMultiplier;

    public float GetExpectedProgress(float elapsedTime)
    {
        float activeTime = Mathf.Max(0f, elapsedTime - _startDelay);
        float normalizedTime = Mathf.Clamp01(activeTime / _targetReachTime);

        return _targetHeadProgress * normalizedTime;
    }

    private void OnValidate()
    {
        _targetHeadProgress = Mathf.Clamp01(_targetHeadProgress);
        _targetReachTime = Mathf.Max(1f, _targetReachTime);
        _startDelay = Mathf.Max(0f, _startDelay);
        _progressDeadZone = Mathf.Clamp(_progressDeadZone, 0f, 0.25f);
        _sampleInterval = Mathf.Max(0.1f, _sampleInterval);
        _increasePerSample = Mathf.Max(0f, _increasePerSample);
        _recoveryPerSample = Mathf.Max(0f, _recoveryPerSample);
        _maxMultiplier = Mathf.Max(1f, _maxMultiplier);
    }
}
