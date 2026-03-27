using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds parallel shots symmetrically around the base shot.
/// Each new modifier places the next outer pair instead of overlapping existing shots.
/// </summary>
public sealed class ParallelModifier : IShotModifier
{
    private readonly int _count;
    private readonly float _spacing;

    public ParallelModifier(int count, float spacing)
    {
        _count = count;
        _spacing = spacing;
    }

    public void Apply(List<ShotSpawnData> shots, ShotContext context)
    {
        if (_count <= 1 || _spacing <= 0f || shots.Count == 0)
            return;

        ShotSpawnData baseShot = shots[0];
        Vector3 right = baseShot.Rotation * Vector3.right;

        int existingShots = shots.Count;

        int extraShots = _count - 1;

        for (int i = 0; i < extraShots; i++)
        {
            int shotIndex = existingShots + i;

            float sideIndex;

            if (shotIndex % 2 == 1)
            {
                sideIndex = (shotIndex + 1) / 2f;
            }
            else
            {
                sideIndex = -(shotIndex / 2f);
            }

            Vector3 offset = right * sideIndex * _spacing;

            shots.Add(new ShotSpawnData(
                baseShot.Position + offset,
                baseShot.Rotation
            ));
        }
    }
}