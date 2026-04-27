using UnityEngine;

/// <summary>
/// Immutable shot spawn payload produced by the projectile pattern builder.
/// </summary>
public struct ShotSpawnData
{
    public Vector3 Position;
    public Quaternion Rotation;

    public ShotSpawnData(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}
