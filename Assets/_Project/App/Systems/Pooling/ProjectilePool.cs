using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool responsible for recycling projectile instances.
///
/// The pool prewarms a number of projectiles during initialization
/// to avoid runtime instantiation during gameplay.
///
/// Projectiles automatically return themselves to the pool when
/// their lifecycle ends.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [SerializeField] private int _prewarmCount = 40;

    private Projectile _prefab;
    private IScreenBounds _screenBounds;
    private readonly Queue<Projectile> _pool = new();
    private readonly List<Projectile> _active = new();
    private bool _initialized;

    /// <summary>
    /// Assigns projectile prefab used by this pool and performs prewarming.
    /// Called once during pool initialization.
    /// </summary>
    public void SetPrefab(Projectile prefab, IScreenBounds screenBounds)
    {
        if (_initialized) return;

        _prefab = prefab;
        _screenBounds = screenBounds;
        Prewarm();
        _initialized = true;
    }

    /// <summary>
    /// Retrieves a projectile instance from the pool.
    /// Creates a new one if the pool is empty.
    /// </summary>
    public Projectile Get()
    {
        Projectile projectile = _pool.Count == 0
            ? CreateNew()
            : _pool.Dequeue();

        _active.Add(projectile);
        return projectile;
    }

    /// <summary>
    /// Returns a projectile instance back to the pool.
    /// </summary>
    public void Release(Projectile projectile)
    {
        if (projectile == null)
            return;

        _active.Remove(projectile);
        projectile.gameObject.SetActive(false);
        _pool.Enqueue(projectile);
    }

    public void ReleaseAllActive()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Projectile projectile = _active[i];

            if (projectile != null)
                projectile.ForceRelease();
        }

        _active.Clear();
    }

    /// <summary>
    /// Creates an initial batch of projectiles to populate the pool.
    /// This prevents runtime allocations during gameplay.
    /// </summary>
    private void Prewarm()
    {
        for (int i = 0; i < _prewarmCount; i++)
        {
            var projectile = CreateNew();
            Release(projectile);
        }
    }

    private Projectile CreateNew()
    {
        var projectile = Instantiate(_prefab, transform);
        projectile.Init(this, _screenBounds);
        projectile.gameObject.SetActive(false);
        return projectile;
    }
}
