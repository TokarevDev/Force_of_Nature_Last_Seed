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
/// • penetration count reaches zero
/// • collision occurs with a valid damageable target
/// </summary>
[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private LayerMask _hitMask;

    private float _lifeTime;
    private float _timer;

    private int _damage;
    private int _penetrationLeft;

    private ProjectilePool _pool;
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

        UpdateVisualRotation();
    }

    public void Init(ProjectilePool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Applies projectile configuration coming from a ScriptableObject.
    /// This defines damage, speed, penetration and visual behaviour.
    /// </summary>
    public void ApplyConfig(ProjectileConfig config)
    {
        _renderer.sprite = config.Sprite;
        _baseVisualRotation = config.RotateSprite;

        _lifeTime = Mathf.Max(0.05f, config.LifeTime);
        _damage = config.Damage;
        _penetrationLeft = Mathf.Max(0, config.Penetration);

        _movement.SetSpeed(config.Speed);

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
        transform.position = position;
        transform.rotation = Quaternion.identity;

        _timer = _lifeTime;
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

        if (((1 << collision.gameObject.layer) & _hitMask) == 0)
            return;

        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage);

            if (_penetrationLeft > 0)
            {
                _penetrationLeft--;
                return;
            }
        }

        ReleaseSelf();
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