using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormPressureDirector : MonoBehaviour
{
    [SerializeField] private WormController _wormController;
    [SerializeField] private WormSpawner _wormSpawner;
    [SerializeField] private WormPressureConfig _config;

    private float _elapsedTime;
    private float _sampleTimer;
    private float _runtimePressureMultiplier = 1f;
    private bool _isTracking;
    private bool _hasStartedForCurrentWorm;

#if UNITY_EDITOR
    public WormPressureConfig EditorConfig => _config;
#endif

    private void OnEnable()
    {
        CombatState.OnShootStateChanged += HandleShootStateChanged;
        WormCombatController.OnWormDied += HandleWormDied;
    }

    private void OnDisable()
    {
        CombatState.OnShootStateChanged -= HandleShootStateChanged;
        WormCombatController.OnWormDied -= HandleWormDied;
    }

    private void Update()
    {
        if (!_isTracking || _config == null || !_config.Enabled)
            return;

        if (_wormController == null || !_wormController.HasWorm)
            return;

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        _elapsedTime += deltaTime;
        _sampleTimer += deltaTime;

        if (_sampleTimer < _config.SampleInterval)
            return;

        _sampleTimer = 0f;
        UpdatePressure();
    }

    private void HandleShootStateChanged(bool canShoot)
    {
        if (canShoot)
        {
            StartTracking();
            return;
        }

        StopTracking(resetPressure: false);
    }

    private void HandleWormDied()
    {
        StopTracking(resetPressure: true);
    }

    public void ResetForNewRun()
    {
        StopTracking(resetPressure: true);
    }

    private void StartTracking()
    {
        if (_config == null || !_config.Enabled)
            return;

        _isTracking = true;

        if (_hasStartedForCurrentWorm)
            return;

        _hasStartedForCurrentWorm = true;
        _elapsedTime = 0f;
        _sampleTimer = 0f;
        SetPressureMultiplier(1f);
    }

    private void StopTracking(bool resetPressure)
    {
        _isTracking = false;

        if (!resetPressure)
            return;

        _hasStartedForCurrentWorm = false;
        _elapsedTime = 0f;
        _sampleTimer = 0f;
        SetPressureMultiplier(1f);
    }

    private void UpdatePressure()
    {
        float actualProgress = _wormController.HeadPathProgressNormalized;
        float expectedProgress = _config.GetExpectedProgress(_elapsedTime);
        float deadZone = _config.ProgressDeadZone;

        if (actualProgress + deadZone < expectedProgress)
        {
            SetPressureMultiplier(
                Mathf.Min(
                    _config.MaxMultiplier,
                    _runtimePressureMultiplier + _config.IncreasePerSample));

            return;
        }

        if (actualProgress > expectedProgress + deadZone)
        {
            SetPressureMultiplier(
                Mathf.Max(
                    1f,
                    _runtimePressureMultiplier - _config.RecoveryPerSample));
        }
    }

    private void SetPressureMultiplier(float multiplier)
    {
        float clampedMultiplier = Mathf.Clamp(
            multiplier,
            1f,
            _config != null ? _config.MaxMultiplier : multiplier);

        if (Mathf.Approximately(_runtimePressureMultiplier, clampedMultiplier))
            return;

        _runtimePressureMultiplier = clampedMultiplier;

        if (_wormSpawner != null)
            _wormSpawner.SetRuntimePressureMultiplier(_runtimePressureMultiplier);
    }
}
