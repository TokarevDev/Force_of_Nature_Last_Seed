using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds parallel shots symmetrically around the base shot.
/// Works only from base shot to prevent exponential growth.
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

        var baseShot = shots[0];

        shots.Clear();
        shots.Add(baseShot);

        Vector3 right = baseShot.Rotation * Vector3.right;

        int pairs = _count - 1;

        for (int i = 1; i <= pairs; i++)
        {
            float offset = i * _spacing;

            shots.Add(new ShotSpawnData(
                baseShot.Position + right * offset,
                baseShot.Rotation
            ));

            shots.Add(new ShotSpawnData(
                baseShot.Position - right * offset,
                baseShot.Rotation
            ));
        }
    }
}