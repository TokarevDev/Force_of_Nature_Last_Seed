using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles projectile spawning and applies modifier pipeline.
/// Modifiers can be added/removed at runtime and affect shot generation immediately.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectileWeapon : MonoBehaviour, IWeapon
{
    private WeaponConfig _config;
    private ProjectilePool _pool;
    private Transform _firePoint;

    private float _cooldown;
    private float _currentFireRate;

    private readonly List<RuntimeModifier> _modifiers = new();
    private readonly List<ShotSpawnData> _shots = new();

    private WeaponRuntimeState _runtimeState;

    public WeaponRuntimeState RuntimeState => _runtimeState;

    private void OnDestroy()
    {
        if (_config != null)
            _config.OnModifiersChanged -= RebuildModifiers;
    }

    public void Init(ProjectilePool pool, Transform firePoint)
    {
        _pool = pool;
        _firePoint = firePoint;
    }

    public void ApplyConfig(WeaponConfig config)
    {
        if (_config != null)
            _config.OnModifiersChanged -= RebuildModifiers;

        _config = config;

        _runtimeState = new WeaponRuntimeState();

        if (_config != null)
            _config.OnModifiersChanged += RebuildModifiers;

        RebuildModifiers();
    }

    public void Tick()
    {
        if (_pool == null || _firePoint == null || _config == null) return;

        _cooldown -= Time.deltaTime;

        if (_cooldown <= 0f)
        {
            Fire();
            _cooldown = Mathf.Max(0.02f, _currentFireRate);
        }
    }

    /// <summary>
    /// Rebuilds modifier pipeline and recalculates fire rate.
    /// Called when config or runtime modifiers change.
    /// </summary>
    private void RebuildModifiers()
    {
        if (_config == null) return;

        _modifiers.Clear();

        _currentFireRate = _config.FireRate / _runtimeState.FireRateMultiplier;

        Debug.Log($"[Rebuild] FireRate = {_currentFireRate} | Multiplier = {_runtimeState.FireRateMultiplier}");

        foreach (var data in _config.Modifiers)
        {
            if (data == null) continue;

            var runtime = data.CreateRuntime();
            _modifiers.Add(new RuntimeModifier(data, runtime));
        }

        foreach (var data in _runtimeState.ShotModifiers)
        {
            if (data == null) continue;

            var runtime = data.CreateRuntime();
            _modifiers.Add(new RuntimeModifier(data, runtime));
        }

        _cooldown = 0f;
    }

    public void ForceRebuild()
    {
        RebuildModifiers();
    }

    private void Fire()
    {
        _shots.Clear();
        _shots.Add(new ShotSpawnData(_firePoint.position, _firePoint.rotation));

        var context = new ShotContext(_firePoint, _pool, _config.Projectile);

        // Applies modifiers sequentially.
        // Order affects final shot pattern.
        foreach (var modifier in _modifiers)
            modifier.Runtime.Apply(_shots, context);

        foreach (var shot in _shots)
            Spawn(shot);
    }

    private void Spawn(ShotSpawnData shot)
    {
        var projectile = _pool.Get();
        projectile.ApplyConfig(_config.Projectile);
        projectile.Activate(shot.Position, shot.Rotation);
    }
}