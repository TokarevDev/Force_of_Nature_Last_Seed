using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the final projectile shot pattern from accumulated runtime modifiers.
/// Pure calculation only: no spawning, no MonoBehaviour lifecycle.
/// </summary>
public sealed class ProjectileShotPatternBuilder
{
    public void Build(
        Vector3 origin,
        Quaternion rotation,
        WeaponRuntimeState runtimeState,
        List<ShotSpawnData> shots)
    {
        if (runtimeState == null || shots == null)
            return;

        var settings = CollectSettings(runtimeState);
        var baseShot = new ShotSpawnData(origin, rotation);

        Vector3 right = baseShot.Rotation * Vector3.right;

        for (int i = 0; i < settings.ParallelCount; i++)
        {
            Vector3 position = baseShot.Position;

            if (i > 0)
            {
                float offset = ((i + 1) / 2) * settings.Spacing;
                position += i % 2 == 1
                    ? right * offset
                    : -right * offset;
            }

            shots.Add(new ShotSpawnData(position, baseShot.Rotation));
        }
    }

    private static ShotPatternSettings CollectSettings(WeaponRuntimeState runtimeState)
    {
        var settings = new ShotPatternSettings
        {
            ParallelCount = runtimeState.ParallelProjectileCount,
            Spacing = runtimeState.ParallelSpacing
        };

        return settings;
    }

    private struct ShotPatternSettings
    {
        public int ParallelCount;
        public float Spacing;
    }
}
