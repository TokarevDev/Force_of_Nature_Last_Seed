using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AcaciaThornWeapon : MonoBehaviour
{
    [SerializeField] private AcaciaThornWeaponConfig _config;

    private readonly AcaciaThornProjectilePool _pool = new();
    private readonly AcaciaThornRuntimeState _runtimeState = new();

    private Transform _firePoint;
    private float _cooldownTimer;
    private float _currentCooldown;
    private bool _initialized;

    public event Action RuntimeStatsChanged;

    public AcaciaThornWeaponConfig Config => _config;
    public AcaciaThornRuntimeState RuntimeState => _runtimeState;

    public void Init(
        Transform firePoint,
        IScreenBounds screenBounds,
        Transform projectileParent)
    {
        if (_initialized)
            return;

        if (_config == null)
        {
            Debug.LogError("AcaciaThornWeapon: config is missing.", this);
            return;
        }

        if (_config.ProjectilePrefab == null)
        {
            Debug.LogError("AcaciaThornWeapon: projectile prefab is missing.", this);
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("AcaciaThornWeapon: fire point is missing.", this);
            return;
        }

        _firePoint = firePoint;
        _runtimeState.SetProgressionLimits(
            _config.MaxDamageMultiplier,
            _config.MaxFireRateBonus,
            _config.MaxExtraSplitProjectiles);

        _pool.Init(
            _config.ProjectilePrefab,
            projectileParent != null ? projectileParent : transform,
            screenBounds,
            _config.PrewarmCount);

        RebuildCooldown(resetTimer: true);
        _initialized = true;
        RuntimeStatsChanged?.Invoke();
    }

    public void Tick()
    {
        if (!_initialized || !_runtimeState.IsUnlocked || !_pool.IsInitialized)
            return;

        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer > 0f)
            return;

        Fire();
        _cooldownTimer = _currentCooldown;
    }

    public void Unlock()
    {
        if (!_runtimeState.CanUnlock)
            return;

        _runtimeState.Unlock();
        _cooldownTimer = 0f;
        RuntimeStatsChanged?.Invoke();
    }

    public void AddDamageMultiplier(float multiplier)
    {
        if (!_runtimeState.CanApplyDamageMultiplier(multiplier))
            return;

        _runtimeState.ApplyDamageMultiplier(multiplier);
        RuntimeStatsChanged?.Invoke();
    }

    public void AddFireRateBonus(float bonus)
    {
        if (!_runtimeState.CanApplyFireRateBonus(bonus))
            return;

        _runtimeState.AddFireRateBonus(bonus);
        RebuildCooldown(resetTimer: false);
        RuntimeStatsChanged?.Invoke();
    }

    public void AddExtraSplitProjectiles(int extraProjectiles)
    {
        if (!_runtimeState.CanApplyExtraSplitProjectiles(extraProjectiles))
            return;

        _runtimeState.AddExtraSplitProjectiles(extraProjectiles);
        RuntimeStatsChanged?.Invoke();
    }

    private void Fire()
    {
        Vector2 direction = _firePoint.rotation * Vector2.up;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        direction.Normalize();

        Vector3 position = _firePoint.position +
            (Vector3)(direction * Mathf.Max(0f, _config.SpawnOffset));

        AcaciaThornProjectile projectile = _pool.Get();
        projectile.Activate(
            position,
            direction,
            BuildDamage(),
            _config.Speed,
            _config.LifeTime,
            _config.BounceCount,
            GetSplitCount(),
            true,
            _config.Sprite,
            _config.RotateSprite,
            _config.SpriteScale);
    }

    private int BuildDamage()
    {
        double rawDamage = Mathf.Max(1, _config.Damage) *
            (double)_runtimeState.DamageMultiplier;

        return AcaciaThornRuntimeState.ClampDamage(rawDamage);
    }

    private int GetSplitCount()
    {
        return Mathf.Max(
            0,
            _config.BaseSplitCount + _runtimeState.ExtraSplitProjectiles);
    }

    private void RebuildCooldown(bool resetTimer)
    {
        float effectiveFireRateBonus = Mathf.Min(
            _runtimeState.FireRateBonus * _config.FireRateBonusEffectiveness,
            _config.MaxFireRateBonus * _config.FireRateBonusEffectiveness);

        _currentCooldown = Mathf.Max(
            _config.MinCooldown,
            _config.Cooldown / (1f + effectiveFireRateBonus));

        if (resetTimer)
            _cooldownTimer = 0f;
        else
            _cooldownTimer = Mathf.Min(_cooldownTimer, _currentCooldown);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Unlock Acacia Thorn")]
    private void DebugUnlockAcaciaThorn()
    {
        Unlock();
    }

    [ContextMenu("Debug/Fire Acacia Thorn Once")]
    private void DebugFireAcaciaThornOnce()
    {
        if (!_initialized)
        {
            Debug.LogWarning("AcaciaThornWeapon debug fire skipped: weapon is not initialized.", this);
            return;
        }

        if (!_runtimeState.IsUnlocked)
            Unlock();

        Fire();
        _cooldownTimer = _currentCooldown;
    }
#endif
}
