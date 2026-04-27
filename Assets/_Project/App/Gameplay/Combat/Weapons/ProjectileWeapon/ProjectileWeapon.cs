using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles projectile spawning and applies modifier pipeline.
/// Modifiers are aggregated into a final shot pattern (no exponential growth).
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour, IWeapon
{
    [Header("Debug / Safety")]
    [SerializeField][Min(1)] private int _maxShots = 200;

    private WeaponConfig _config;
    private ProjectilePool _pool;
    private Transform _firePoint;

    private float _shotCooldown;
    private float _currentShotCooldown;
    private float _minShotCooldown = 0.5f;
    private float _burstTimer;
    private int _burstShotsRemaining;
    private bool _isBursting;

    private readonly List<ShotSpawnData> _shots = new();
    private readonly ProjectileShotPatternBuilder _shotPatternBuilder = new();

    private WeaponRuntimeState _runtimeState;

    public WeaponRuntimeState RuntimeState => _runtimeState;

    public void Init(ProjectilePool pool, Transform firePoint)
    {
        _pool = pool;
        _firePoint = firePoint;
    }

    public void ApplyConfig(WeaponConfig config)
    {
        _config = config;

        if (_runtimeState == null)
            _runtimeState = new WeaponRuntimeState();

        RebuildModifiers();
    }

    public void Tick()
    {
        if (_pool == null || _firePoint == null || _config == null) return;

        if (_isBursting)
        {
            TickBurst();
            return;
        }

        _shotCooldown -= Time.deltaTime;

        if (_shotCooldown <= 0f)
            StartBurst();
    }

    /// <summary>
    /// Rebuilds fire rate using runtime multipliers.
    /// </summary>
    private void RebuildModifiers()
    {
        if (_config == null) return;

        _currentShotCooldown = _config.FireRate / (1f + _runtimeState.FireRateBonus);

        Debug.Log(
            $"BaseFireRate={_config.FireRate}, " +
            $"Bonus={_runtimeState.FireRateBonus}" +
            $"CurrentCooldown={_currentShotCooldown}");

        _shotCooldown = 0f;
    }

    public void ForceRebuild()
    {
        RebuildModifiers();
    }

    private void StartBurst()
    {
        Fire();

        _burstShotsRemaining = Mathf.Max(0, _runtimeState.BurstExtraShots);

        if (_burstShotsRemaining <= 0)
        {
            StartWeaponCooldown();
            return;
        }

        _isBursting = true;
        _burstTimer = GetBurstInterval();
    }

    private void TickBurst()
    {
        _burstTimer -= Time.deltaTime;

        while (_burstShotsRemaining > 0 && _burstTimer <= 0f)
        {
            Fire();
            _burstShotsRemaining--;

            if (_burstShotsRemaining > 0)
                _burstTimer += GetBurstInterval();
        }

        if (_burstShotsRemaining > 0)
            return;

        _isBursting = false;
        StartWeaponCooldown();
    }

    private void StartWeaponCooldown()
    {
        _shotCooldown = Mathf.Max(_minShotCooldown, _currentShotCooldown);
    }

    private float GetBurstInterval()
    {
        return Mathf.Max(0.01f, _runtimeState.BurstInterval);
    }

    private void Fire()
    {
        _shots.Clear();
        _shotPatternBuilder.Build(_firePoint.position, _firePoint.rotation, _runtimeState, _shots);

        //  Safety clamp
        if (_shots.Count > _maxShots)
        {
            Debug.LogWarning($"Shot limit exceeded: {_shots.Count} → clamped to {_maxShots}");
            _shots.RemoveRange(_maxShots, _shots.Count - _maxShots);
        }

        foreach (var shot in _shots)
        {
            Spawn(shot);
        }
    }

    private void Spawn(ShotSpawnData shot)
    {
        var projectile = _pool.Get();
        ProjectileRuntimeStats stats = BuildProjectileStats();

        projectile.ApplyConfig(_config.Projectile, stats);
        projectile.Activate(shot.Position, shot.Rotation);
    }

    private ProjectileRuntimeStats BuildProjectileStats()
    {
        int finalDamage = Mathf.RoundToInt(
            _config.Projectile.Damage * _runtimeState.DamageMultiplier);

        return new ProjectileRuntimeStats(
            finalDamage,
            _runtimeState.PenetrationBonus,
            _runtimeState.CriticalChance,
            _runtimeState.CriticalDamageMultiplier
        );
    }
}
