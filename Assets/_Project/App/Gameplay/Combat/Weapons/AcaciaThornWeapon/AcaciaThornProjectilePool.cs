using System.Collections.Generic;
using UnityEngine;

public sealed class AcaciaThornProjectilePool
{
    private readonly Queue<AcaciaThornProjectile> _pool = new();

    private AcaciaThornProjectile _prefab;
    private Transform _parent;
    private IScreenBounds _screenBounds;
    private bool _initialized;

    public bool IsInitialized => _initialized;

    public void Init(
        AcaciaThornProjectile prefab,
        Transform parent,
        IScreenBounds screenBounds,
        int prewarmCount)
    {
        if (_initialized)
            return;

        _prefab = prefab;
        _parent = parent;
        _screenBounds = screenBounds;

        for (int i = 0; i < Mathf.Max(0, prewarmCount); i++)
        {
            AcaciaThornProjectile projectile = CreateNew();
            Release(projectile);
        }

        _initialized = true;
    }

    public AcaciaThornProjectile Get()
    {
        if (_pool.Count == 0)
            return CreateNew();

        return _pool.Dequeue();
    }

    public void Release(AcaciaThornProjectile projectile)
    {
        if (projectile == null)
            return;

        projectile.gameObject.SetActive(false);
        _pool.Enqueue(projectile);
    }

    private AcaciaThornProjectile CreateNew()
    {
        AcaciaThornProjectile projectile = Object.Instantiate(_prefab, _parent);
        projectile.Init(this, _screenBounds);
        projectile.gameObject.SetActive(false);
        return projectile;
    }
}
