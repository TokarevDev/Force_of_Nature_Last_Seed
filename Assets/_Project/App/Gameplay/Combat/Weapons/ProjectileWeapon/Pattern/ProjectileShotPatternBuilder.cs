using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the final projectile shot pattern from accumulated runtime modifiers.
/// Pure calculation only: no spawning, no MonoBehaviour lifecycle.
/// </summary>
public sealed class ProjectileShotPatternBuilder
{
    private const float DefaultParallelSpacing = 0.5f;

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

        var positions = new List<Vector3>(settings.ParallelCount);
        Vector3 right = baseShot.Rotation * Vector3.right;

        positions.Add(baseShot.Position);

        int sideCount = settings.ParallelCount - 1;

        for (int i = 1; i <= sideCount; i++)
        {
            float offset = ((i + 1) / 2) * settings.Spacing;

            if (i % 2 == 1)
                positions.Add(baseShot.Position + right * offset);
            else
                positions.Add(baseShot.Position - right * offset);
        }

        foreach (var position in positions)
        {
            AddSpreadShots(shots, position, baseShot.Rotation, settings);
        }
    }

    private static ShotPatternSettings CollectSettings(WeaponRuntimeState runtimeState)
    {
        var settings = new ShotPatternSettings
        {
            ParallelCount = 1,
            SpreadCount = 1,
            SpreadAngle = 0f,
            Spacing = DefaultParallelSpacing
        };

        foreach (var modifier in runtimeState.ShotModifiers)
        {
            if (modifier is ParallelModifierData parallel)
            {
                settings.ParallelCount += parallel.Count - 1;
                settings.Spacing = parallel.Spacing;
            }
            else if (modifier is SpreadModifierData spread)
            {
                settings.SpreadCount += spread.Count - 1;
                settings.SpreadAngle += spread.Angle * 0.5f;
            }
        }

        return settings;
    }

    private static void AddSpreadShots(
        List<ShotSpawnData> shots,
        Vector3 position,
        Quaternion baseRotation,
        ShotPatternSettings settings)
    {
        if (settings.SpreadCount <= 1)
        {
            shots.Add(new ShotSpawnData(position, baseRotation));
            return;
        }

        float step = settings.SpreadAngle / (settings.SpreadCount - 1);
        float start = -settings.SpreadAngle * 0.5f;

        for (int i = 0; i < settings.SpreadCount; i++)
        {
            float angle = start + step * i;
            Quaternion rotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            shots.Add(new ShotSpawnData(position, rotation));
        }
    }

    private struct ShotPatternSettings
    {
        public int ParallelCount;
        public int SpreadCount;
        public float SpreadAngle;
        public float Spacing;
    }
}
