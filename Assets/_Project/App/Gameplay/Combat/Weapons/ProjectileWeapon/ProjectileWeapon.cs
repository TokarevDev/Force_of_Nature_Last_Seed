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

    private float _shotCooldown;
    private float _currentShotCooldown;
    private float _minShotCooldown = 0.5f;

    private readonly List<ShotSpawnData> _shots = new();

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

        _shotCooldown -= Time.deltaTime;

        if (_shotCooldown <= 0f)
        {
            Fire();
            _shotCooldown = Mathf.Max(_minShotCooldown, _currentShotCooldown);
        }
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

    private void Fire()
    {
        _shots.Clear();

        var context = new ShotContext(_firePoint, _pool, _config.Projectile);

        BuildShots(_shots, context);

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

    private void BuildShots(List<ShotSpawnData> shots, ShotContext context)
    {
        var baseShot = new ShotSpawnData(_firePoint.position, _firePoint.rotation);

        int parallelCount = 1;
        int spreadCount = 1;
        float spreadAngle = 0f;
        float spacing = 0.5f;

        foreach (var mod in _runtimeState.ShotModifiers)
        {
            if (mod is ParallelModifierData p)
            {
                parallelCount += (p.Count - 1);
                spacing = p.Spacing;
            }
            else if (mod is SpreadModifierData s)
            {
                spreadCount += (s.Count - 1);
                spreadAngle += s.Angle * 0.5f;
            }
        }

        List<Vector3> positions = new();
        Vector3 right = baseShot.Rotation * Vector3.right;

        positions.Add(baseShot.Position);

        int sideCount = parallelCount - 1;

        for (int i = 1; i <= sideCount; i++)
        {
            float offset = ((i + 1) / 2) * spacing;

            if (i % 2 == 1)
                positions.Add(baseShot.Position + right * offset);
            else
                positions.Add(baseShot.Position - right * offset);
        }

        foreach (var pos in positions)
        {
            if (spreadCount <= 1)
            {
                shots.Add(new ShotSpawnData(pos, baseShot.Rotation));
                continue;
            }

            float step = spreadAngle / (spreadCount - 1);
            float start = -spreadAngle * 0.5f;

            for (int i = 0; i < spreadCount; i++)
            {
                float angle = start + step * i;

                Quaternion rot = baseShot.Rotation * Quaternion.Euler(0f, 0f, angle);

                shots.Add(new ShotSpawnData(pos, rot));
            }
        }
    }

    private void Spawn(ShotSpawnData shot)
    {
        var projectile = _pool.Get();

        int finalDamage = Mathf.RoundToInt(
            _config.Projectile.Damage * _runtimeState.DamageMultiplier);

        projectile.ApplyConfig(_config.Projectile, finalDamage);
        projectile.Activate(shot.Position, shot.Rotation);
    }
}