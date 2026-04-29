using System;
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

    private float _weaponCooldownTimer;
    private float _currentShotCooldown;
    private float _salvoTimer;
    private int _salvoShotsRemaining;
    private bool _isSalvoActive;

    private readonly List<ShotSpawnData> _shots = new();
    private readonly ProjectileShotPatternBuilder _shotPatternBuilder = new();

    private WeaponRuntimeState _runtimeState;

    public event Action RuntimeStatsChanged;

    public WeaponConfig Config => _config;
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

        _runtimeState.SetFireRateBonusLimit(_config.MaxFireRateBonus);
        _runtimeState.SetProgressionLimits(
            _config.MaxDamageMultiplier,
            _config.MaxCriticalChance,
            _config.MaxCriticalDamageMultiplier,
            _config.MaxPenetrationBonus,
            _config.MaxParallelProjectiles,
            _config.MaxSalvoExtraShots);

        RebuildModifiers(resetFiringCycle: true);
        RuntimeStatsChanged?.Invoke();
    }

    public void Tick()
    {
        if (_pool == null || _firePoint == null || _config == null) return;

        if (_isSalvoActive)
        {
            TickSalvo();
            return;
        }

        _weaponCooldownTimer -= Time.deltaTime;

        if (_weaponCooldownTimer <= 0f)
            StartSalvo();
    }

    /// <summary>
    /// Rebuilds fire rate using runtime multipliers.
    /// </summary>
    private void RebuildModifiers(bool resetFiringCycle)
    {
        if (_config == null) return;

        float effectiveFireRateBonus = Mathf.Min(
            _runtimeState.FireRateBonus,
            _config.MaxFireRateBonus);

        _currentShotCooldown = Mathf.Max(
            _config.MinShotCooldown,
            _config.FireRate / (1f + effectiveFireRateBonus));

        if (resetFiringCycle)
        {
            ResetFiringCycle();
            return;
        }

        if (!_isSalvoActive)
            _weaponCooldownTimer = Mathf.Min(_weaponCooldownTimer, _currentShotCooldown);
    }

    public void ForceRebuild()
    {
        RebuildModifiers(resetFiringCycle: false);
        RuntimeStatsChanged?.Invoke();
    }

    private void ResetFiringCycle()
    {
        _isSalvoActive = false;
        _salvoTimer = 0f;
        _salvoShotsRemaining = 0;
        _weaponCooldownTimer = 0f;
    }

    private void StartSalvo()
    {
        _salvoShotsRemaining = 1 + Mathf.Max(0, _runtimeState.SalvoExtraShots);
        FireSalvoShot();
    }

    private void TickSalvo()
    {
        _salvoTimer -= Time.deltaTime;

        if (_salvoTimer > 0f)
            return;

        FireSalvoShot();
    }

    private void FireSalvoShot()
    {
        Fire();
        _salvoShotsRemaining--;

        if (_salvoShotsRemaining <= 0)
        {
            _isSalvoActive = false;
            StartWeaponCooldown();
            return;
        }

        _isSalvoActive = true;
        _salvoTimer = GetSalvoInterval();
    }

    private void StartWeaponCooldown()
    {
        _weaponCooldownTimer = _currentShotCooldown;
    }

    private float GetSalvoInterval()
    {
        return Mathf.Max(0.01f, _runtimeState.SalvoInterval);
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
        int finalDamage = WeaponRuntimeState.ClampDamage(
            _config.Projectile.Damage * (double)_runtimeState.DamageMultiplier);

        return new ProjectileRuntimeStats(
            finalDamage,
            _runtimeState.PenetrationBonus,
            _runtimeState.CriticalChance,
            _runtimeState.CriticalDamageMultiplier
        );
    }
}
