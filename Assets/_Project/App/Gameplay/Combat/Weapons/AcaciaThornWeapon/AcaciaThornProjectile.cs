using UnityEngine;

[DisallowMultipleComponent]
public sealed class AcaciaThornProjectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField, Min(0f)] private float _spawnHitDelay = 0.04f;
    [SerializeField, Min(0f)] private float _minHitTravelDistance = 0.25f;
    [SerializeField, Min(0f)] private float _hitCooldown = 0.06f;
    [SerializeField, Min(0f)] private float _releaseBoundsPadding = 1.5f;

    private AcaciaThornProjectilePool _pool;
    private IScreenBounds _screenBounds;
    private Vector2 _direction;
    private Vector3 _spawnPosition;
    private float _speed;
    private float _lifeTime;
    private float _timer;
    private float _hitDelayTimer;
    private float _hitCooldownTimer;
    private float _baseVisualRotation;
    private int _damage;
    private DamageKind _damageKind;
    private int _bouncesLeft;
    private int _splitCount;
    private bool _canSplit;
    private bool _isCritical;
    private bool _hasHitWorm;
    private bool _active;

    public void Init(AcaciaThornProjectilePool pool, IScreenBounds screenBounds)
    {
        _pool = pool;
        _screenBounds = screenBounds;
    }

    public void Activate(
        Vector3 position,
        Vector2 direction,
        int damage,
        DamageKind damageKind,
        bool isCritical,
        float speed,
        float lifeTime,
        int bounces,
        int splitCount,
        bool canSplit,
        Sprite sprite,
        float baseVisualRotation,
        float spriteScale)
    {
        _spawnPosition = position;
        _direction = NormalizeDirection(direction);
        _damage = Mathf.Max(1, damage);
        _damageKind = damageKind;
        _isCritical = isCritical;
        _speed = Mathf.Max(0.1f, speed);
        _lifeTime = Mathf.Max(0.05f, lifeTime);
        _timer = _lifeTime;
        _bouncesLeft = Mathf.Max(0, bounces);
        _splitCount = Mathf.Max(0, splitCount);
        _canSplit = canSplit;
        _hasHitWorm = false;
        _hitDelayTimer = _spawnHitDelay;
        _hitCooldownTimer = 0f;
        _baseVisualRotation = baseVisualRotation;

        transform.position = position;
        transform.rotation = Quaternion.identity;

        if (_renderer != null && sprite != null)
            _renderer.sprite = sprite;

        if (_renderer != null)
            _renderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, spriteScale);

        _active = true;
        gameObject.SetActive(true);
        UpdateVisualRotation();
    }

    private void Update()
    {
        if (!_active)
            return;

        float deltaTime = Time.deltaTime;
        _timer -= deltaTime;

        if (_timer <= 0f)
        {
            ReleaseSelf();
            return;
        }

        if (_hitDelayTimer > 0f)
            _hitDelayTimer -= deltaTime;

        if (_hitCooldownTimer > 0f)
            _hitCooldownTimer -= deltaTime;

        transform.position += (Vector3)(_direction * (_speed * deltaTime));

        TryBounceFromScreen();

        if (!_active)
            return;

        if (IsOutsideReleaseBounds())
        {
            ReleaseSelf();
            return;
        }

        UpdateVisualRotation();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_active || !CanHitNow())
            return;

        if (((1 << collision.gameObject.layer) & _hitMask) == 0)
            return;

        if (!collision.TryGetComponent(out WormSegmentDamageReceiver receiver))
            return;

        WormSegment segment = receiver.GetSegment();
        if (segment == null || !segment.IsAlive)
            return;

        WormSection section = receiver.GetDamageSection();
        if (section == null || section.IsDestroyed)
            return;

        Vector3 hitPosition = collision.ClosestPoint(transform.position);
        _hasHitWorm = true;

        receiver.TakeDamage(new DamageInfo(
            _damage,
            hitPosition,
            _damageKind,
            this,
            _isCritical));

        _hitCooldownTimer = _hitCooldown;

        if (_canSplit && _splitCount > 0)
        {
            SpawnSplitProjectiles(hitPosition);
            ReleaseSelf();
            return;
        }

        BounceFromWorm(hitPosition);
    }

    private bool CanHitNow()
    {
        if (_hitDelayTimer > 0f || _hitCooldownTimer > 0f)
            return false;

        if (_minHitTravelDistance <= 0f)
            return true;

        float sqrDistance = Vector3.SqrMagnitude(transform.position - _spawnPosition);
        return sqrDistance >= _minHitTravelDistance * _minHitTravelDistance;
    }

    private void SpawnSplitProjectiles(Vector3 position)
    {
        if (_pool == null)
            return;

        for (int i = 0; i < _splitCount; i++)
        {
            AcaciaThornProjectile projectile = _pool.Get();
            projectile.Activate(
                position,
                GetRandomDirection(),
                _damage,
                _damageKind,
                _isCritical,
                _speed,
                _lifeTime,
                _bouncesLeft,
                0,
                false,
                _renderer != null ? _renderer.sprite : null,
                _baseVisualRotation,
                _renderer != null ? _renderer.transform.localScale.x : 1f);
            projectile._hasHitWorm = true;
        }
    }

    private void BounceFromWorm(Vector3 hitPosition)
    {
        Vector2 normal = (Vector2)(transform.position - hitPosition);

        if (normal.sqrMagnitude < 0.0001f)
            normal = -_direction;

        TryBounce(normal.normalized);
    }

    private void TryBounceFromScreen()
    {
        if (_screenBounds == null || !_hasHitWorm)
            return;

        Vector3 position = transform.position;
        Vector2 normal = Vector2.zero;

        if (position.x < _screenBounds.Left)
        {
            position.x = _screenBounds.Left;
            normal.x += 1f;
        }
        else if (position.x > _screenBounds.Right)
        {
            position.x = _screenBounds.Right;
            normal.x -= 1f;
        }

        if (position.y < _screenBounds.Bottom)
        {
            position.y = _screenBounds.Bottom;
            normal.y += 1f;
        }
        else if (position.y > _screenBounds.Top)
        {
            position.y = _screenBounds.Top;
            normal.y -= 1f;
        }

        if (normal.sqrMagnitude < 0.0001f)
            return;

        transform.position = position;
        TryBounce(normal.normalized);
    }

    private void TryBounce(Vector2 normal)
    {
        if (_bouncesLeft <= 0)
        {
            ReleaseSelf();
            return;
        }

        _direction = Vector2.Reflect(_direction, normal).normalized;
        _bouncesLeft--;
        _hitCooldownTimer = Mathf.Max(_hitCooldownTimer, _hitCooldown);
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

    private void UpdateVisualRotation()
    {
        if (_renderer == null || _direction.sqrMagnitude < 0.0001f)
            return;

        float angle = -Mathf.Atan2(_direction.x, _direction.y) * Mathf.Rad2Deg;
        _renderer.transform.localRotation =
            Quaternion.Euler(0f, 0f, angle + _baseVisualRotation);
    }

    private void ReleaseSelf()
    {
        if (!_active)
            return;

        _active = false;

        if (_pool != null)
        {
            _pool.Release(this);
            return;
        }

        gameObject.SetActive(false);
    }

    private static Vector2 NormalizeDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Vector2.up;

        return direction.normalized;
    }

    private static Vector2 GetRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector2.up;

        return direction.normalized;
    }
}
