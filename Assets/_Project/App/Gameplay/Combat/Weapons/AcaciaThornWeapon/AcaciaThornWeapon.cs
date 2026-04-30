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
    private float _salvoTimer;
    private int _salvoShotsRemaining;
    private bool _isSalvoActive;
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
            _config.MaxSalvoExtraShots,
            _config.MaxProjectileSpeedBonus,
            _config.MaxCriticalChance,
            _config.CriticalDamageMultiplier,
            _config.MaxCriticalDamageMultiplier);
        _runtimeState.SetBaseDamage(_config.Damage);

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

        if (_isSalvoActive)
        {
            TickSalvo();
            return;
        }

        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer > 0f)
            return;

        StartSalvo();
    }

    public void Unlock(int baseDamage)
    {
        if (!_runtimeState.CanUnlock)
            return;

        int fallbackBaseDamage = _config != null ? _config.Damage : 1;
        _runtimeState.Unlock(Mathf.Max(fallbackBaseDamage, baseDamage));
        _cooldownTimer = 0f;
        _salvoTimer = 0f;
        _salvoShotsRemaining = 0;
        _isSalvoActive = false;
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

    public void AddSalvoShots(int extraShots)
    {
        if (!_runtimeState.CanApplySalvoShots(extraShots))
            return;

        _runtimeState.AddSalvoShots(extraShots);
        RuntimeStatsChanged?.Invoke();
    }

    public void AddProjectileSpeedBonus(float bonus)
    {
        if (!_runtimeState.CanApplyProjectileSpeedBonus(bonus))
            return;

        _runtimeState.AddProjectileSpeedBonus(bonus);
        RuntimeStatsChanged?.Invoke();
    }

    public void AddCriticalChance(float chanceBonus)
    {
        if (!_runtimeState.CanApplyCriticalChance(chanceBonus))
            return;

        _runtimeState.AddCriticalChance(chanceBonus);
        RuntimeStatsChanged?.Invoke();
    }

    public void AddCriticalDamageBonus(float damageBonus)
    {
        if (!_runtimeState.CanApplyCriticalDamageBonus(damageBonus))
            return;

        _runtimeState.AddCriticalDamageBonus(damageBonus);
        RuntimeStatsChanged?.Invoke();
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
            _cooldownTimer = _currentCooldown;
            return;
        }

        _isSalvoActive = true;
        _salvoTimer = GetSalvoInterval();
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
        int damage = BuildDamage(out DamageKind damageKind, out bool isCritical);
        projectile.Activate(
            position,
            direction,
            damage,
            damageKind,
            isCritical,
            GetProjectileSpeed(),
            _config.LifeTime,
            _config.BounceCount,
            GetSplitCount(),
            true,
            _config.Sprite,
            _config.RotateSprite,
            _config.SpriteScale);
    }

    private int BuildDamage(out DamageKind damageKind, out bool isCritical)
    {
        double rawDamage = Mathf.Max(1, _runtimeState.BaseDamage) *
            (double)_runtimeState.DamageMultiplier;

        isCritical = _runtimeState.CriticalChance > 0f &&
            UnityEngine.Random.value < _runtimeState.CriticalChance;
        damageKind = isCritical ? DamageKind.Critical : DamageKind.Normal;

        if (isCritical)
            rawDamage *= _runtimeState.CriticalDamageMultiplier;

        return AcaciaThornRuntimeState.ClampDamage(rawDamage);
    }

    private int GetSplitCount()
    {
        return Mathf.Max(0, _config.BaseSplitCount);
    }

    private float GetProjectileSpeed()
    {
        return Mathf.Max(
            0.1f,
            _config.Speed * GetProjectileSpeedMultiplier());
    }

    private float GetSalvoInterval()
    {
        return Mathf.Max(
            0.01f,
            _config.SalvoInterval / GetProjectileSpeedMultiplier());
    }

    private float GetProjectileSpeedMultiplier()
    {
        return Mathf.Max(0.1f, 1f + _runtimeState.ProjectileSpeedBonus);
    }

    private void RebuildCooldown(bool resetTimer)
    {
        float cappedFireRateBonus = Mathf.Min(
            _runtimeState.FireRateBonus,
            _config.MaxFireRateBonus);

        _currentCooldown = Mathf.Max(
            _config.MinCooldown,
            _config.Cooldown / (1f + cappedFireRateBonus));

        if (resetTimer)
            _cooldownTimer = 0f;
        else
            _cooldownTimer = Mathf.Min(_cooldownTimer, _currentCooldown);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Unlock Acacia Thorn")]
    private void DebugUnlockAcaciaThorn()
    {
        Unlock(_config != null ? _config.Damage : 1);
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
            Unlock(_config != null ? _config.Damage : 1);

        Fire();
        _cooldownTimer = _currentCooldown;
    }
#endif
}
