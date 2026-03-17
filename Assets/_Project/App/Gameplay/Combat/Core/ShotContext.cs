using UnityEngine;

public struct ShotContext
{
    public Transform FirePoint;
    public ProjectilePool Pool;
    public ProjectileConfig Config;

    public ShotContext(Transform firePoint, ProjectilePool pool, ProjectileConfig config)
    {
        FirePoint = firePoint;
        Pool = pool;
        Config = config;
    }
}