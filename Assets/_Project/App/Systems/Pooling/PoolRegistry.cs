using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry responsible for managing projectile pools.
///
/// Each projectile prefab receives its own dedicated pool.
/// Pools are created lazily on first request and reused afterwards.
///
/// This allows multiple weapon types to use different projectile
/// prefabs without creating duplicate pools.
/// </summary>
public sealed class PoolRegistry : MonoBehaviour
{
    [SerializeField] private ProjectilePool _poolPrefab;

    private readonly Dictionary<int, ProjectilePool> _pools = new();
    private IScreenBounds _screenBounds;

    public void Init(IScreenBounds screenBounds)
    {
        _screenBounds = screenBounds;
    }

    /// <summary>
    /// Returns an existing pool for the given projectile prefab
    /// or creates a new one if it does not exist yet.
    /// </summary>
    public ProjectilePool GetPool(Projectile projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("PoolRegistry: projectilePrefab is NULL");
            return null;
        }

        int key = projectilePrefab.GetInstanceID();

        if (_pools.TryGetValue(key, out var pool))
            return pool;

        return CreatePool(projectilePrefab, key);
    }

    /// <summary>
    /// Instantiates a new pool instance for the specified projectile prefab.
    /// The pool will handle spawning and recycling projectile instances.
    /// </summary>
    private ProjectilePool CreatePool(Projectile prefab, int key)
    {
        if (_screenBounds == null)
        {
            Debug.LogError("PoolRegistry: screen bounds are not initialized.", this);
            return null;
        }

        var pool = Instantiate(_poolPrefab, transform);
        pool.name = $"Pool_{prefab.name}";

        pool.SetPrefab(prefab, _screenBounds);

        _pools.Add(key, pool);
        return pool;
    }
}
