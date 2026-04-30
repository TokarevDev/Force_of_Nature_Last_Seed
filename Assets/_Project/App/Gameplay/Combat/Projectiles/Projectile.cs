using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime projectile entity responsible for movement, collision and lifetime.
///
/// The projectile is configured through ProjectileConfig and reused through
/// ProjectilePool to avoid runtime allocations.
///
/// Behaviour is split into small components:
/// - ProjectileMovement controls trajectory
/// - ProjectileBounce handles bounce logic
///
/// The projectile releases itself back to the pool when:
/// • lifetime expires
/// • allowed hit count reaches zero
/// • collision occurs with a valid damageable target
/// </summary>
[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private float _minHitDistance = 1.5f;
    [SerializeField, Min(0f)] private float _releaseBoundsPadding = 2f;
    [SerializeField, Min(0f)] private float _spawnHitDelay = 0.03f;
    [SerializeField, Min(0f)] private float _minHitTravelDistance = 0.25f;
    [SerializeField, Min(0f)] private float _damageBoundsPadding = 0f;

    private Vector3 _lastHitPosition;
    private Vector3 _spawnPosition;
    private bool _hasLastHit;
    private float _hitDelayTimer;

    private readonly HashSet<WormSection> _hitSections = new();

    private float _lifeTime;
    private float _timer;

    private int _damage;
    private int _hitsLeft;
    private float _criticalChance;
    private float _criticalDamageMultiplier = 1f;

    private ProjectilePool _pool;
    private IScreenBounds _screenBounds;
    private bool _active;
    private float _baseVisualRotation;

    private ProjectileMovement _movement;
    private ProjectileBounce _bounce;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>();
        _bounce = GetComponent<ProjectileBounce>();

        if (_renderer == null)
            Debug.LogError("Projectile: SpriteRenderer reference is not set.", this);
    }

    /// <summary>
    /// Handles projectile lifetime and per-frame behaviour.
    /// Movement and optional bounce logic are updated here.
    /// </summary>
    private void Update()
    {
        if (!_active)
            return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ReleaseSelf();
            return;
        }

        _bounce?.Tick();
        _movement.Tick();

        if (_hitDelayTimer > 0f)
            _hitDelayTimer -= Time.deltaTime;

        if (IsOutsideReleaseBounds())
        {
            ReleaseSelf();
            return;
        }

        UpdateVisualRotation();
    }

    public void Init(ProjectilePool pool, IScreenBounds screenBounds)
    {
        _pool = pool;
        _screenBounds = screenBounds;
        _bounce?.Init(screenBounds);
    }

    /// <summary>
    /// Applies projectile configuration coming from a ScriptableObject.
    /// This defines damage, speed, penetration and visual behaviour.
    /// </summary>
    public void ApplyConfig(ProjectileConfig config, ProjectileRuntimeStats stats)
    {
        _renderer.sprite = config.Sprite;
        _baseVisualRotation = config.RotateSprite;

        _lifeTime = Mathf.Max(0.05f, config.LifeTime);
        _damage = stats.Damage;
        _hitsLeft = Mathf.Max(1, 1 + config.Penetration + stats.ExtraPenetration);
        _criticalChance = stats.CriticalChance;
        _criticalDamageMultiplier = stats.CriticalDamageMultiplier;

        _movement.SetSpeed(config.Speed * stats.ProjectileSpeedMultiplier);

        if (_bounce != null)
        {
            _bounce.SetBounces(
                config.BounceCount,
                config.BounceX,
                config.BounceY
            );
        }
    }

    /// <summary>
    /// Activates projectile from the pool and initializes its direction.
    /// Called by weapon systems when spawning a new shot.
    /// </summary>
    public void Activate(Vector3 position, Quaternion shotRotation)
    {
        _hasLastHit = false;

        _hitSections.Clear();

        _spawnPosition = position;
        transform.position = position;
        transform.rotation = Quaternion.identity;

        _timer = _lifeTime;
        _hitDelayTimer = _spawnHitDelay;
        _active = true;

        Vector2 direction = shotRotation * Vector2.up;
        _movement.SetDirection(direction);

        _bounce?.ResetBounces();

        UpdateVisualRotation();
        gameObject.SetActive(true);
    }

    private void UpdateVisualRotation()
    {
        if (_movement == null) return;

        Vector2 dir = _movement.Direction;
        if (dir.sqrMagnitude < 0.001f) return;

        float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        _renderer.transform.localRotation =
            Quaternion.Euler(0f, 0f, angle + _baseVisualRotation);
    }

    /// <summary>
    /// Handles projectile collision with damageable targets.
    /// Supports penetration allowing the projectile to pass through
    /// multiple enemies before being released back to the pool.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_active)
            return;

        if (!CanHitNow())
            return;

        if (((1 << collision.gameObject.layer) & _hitMask) == 0)
            return;

        if (!collision.TryGetComponent<WormSegmentDamageReceiver>(out var receiver))
            return;

        var segment = receiver.GetSegment();
        if (segment == null || !segment.IsAlive)
            return;

        var section = receiver.GetDamageSection();
        if (section == null || section.IsDestroyed)
            return;

        if (_hitSections.Contains(section))
            return;

        Vector3 hitPosition = collision.ClosestPoint(transform.position);

        if (!IsInsideDamageBounds(hitPosition))
            return;

        if (_hasLastHit)
        {
            float dist = Vector3.SqrMagnitude(hitPosition - _lastHitPosition);
            if (dist < _minHitDistance * _minHitDistance)
                return;
        }

        _hitSections.Add(section);

        int damage = RollDamage(out DamageKind damageKind, out bool isCritical);

        var damageInfo = new DamageInfo(
            damage,
            hitPosition,
            damageKind,
            this,
            isCritical
        );

        receiver.TakeDamage(damageInfo);

        _lastHitPosition = hitPosition;
        _hasLastHit = true;

        _hitsLeft--;

        if (_hitsLeft > 0)
            return;

        ReleaseSelf();
    }

    private bool CanHitNow()
    {
        if (_hitDelayTimer > 0f)
            return false;

        if (_minHitTravelDistance <= 0f)
            return true;

        float sqrDistance = Vector3.SqrMagnitude(transform.position - _spawnPosition);
        return sqrDistance >= _minHitTravelDistance * _minHitTravelDistance;
    }

    private bool IsOutsideReleaseBounds()
    {
        if (_screenBounds == null)
            return false;

        Vector3 position = transform.position;
        float padding = _releaseBoundsPadding;

        return position.x < _screenBounds.Left - padding ||
               position.x > _screenBounds.Right + padding ||
               position.y < _screenBounds.Bottom - padding ||
               position.y > _screenBounds.Top + padding;
    }

    private bool IsInsideDamageBounds(Vector3 position)
    {
        if (_screenBounds == null)
            return true;

        float padding = _damageBoundsPadding;

        return position.x >= _screenBounds.Left - padding &&
               position.x <= _screenBounds.Right + padding &&
               position.y >= _screenBounds.Bottom - padding &&
               position.y <= _screenBounds.Top + padding;
    }

    private int RollDamage(out DamageKind damageKind, out bool isCritical)
    {
        isCritical = _criticalChance > 0f && Random.value < _criticalChance;
        damageKind = isCritical ? DamageKind.Critical : DamageKind.Normal;

        if (!isCritical)
            return _damage;

        return WeaponRuntimeState.ClampDamage(_damage * (double)_criticalDamageMultiplier);
    }

    /// <summary>
    /// Returns projectile back to the pool and disables runtime logic.
    /// </summary>
    private void ReleaseSelf()
    {
        if (!_active)
            return;

        _active = false;
        _pool.Release(this);
    }
}
