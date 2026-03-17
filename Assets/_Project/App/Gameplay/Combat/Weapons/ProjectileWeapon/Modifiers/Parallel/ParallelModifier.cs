using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Duplicates shots horizontally relative to firing direction.
/// </summary>
public class ParallelModifier : IShotModifier
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
        if (_count <= 1 || _spacing <= 0f) return;

        var newShots = new List<ShotSpawnData>();

        foreach (var shot in shots)
        {
            Vector3 right = shot.Rotation * Vector3.right;
            float half = (_count - 1) * 0.5f;

            for (int i = 0; i < _count; i++)
            {
                float offsetIndex = i - half;
                Vector3 offset = right * offsetIndex * _spacing;

                newShots.Add(new ShotSpawnData(shot.Position + offset, shot.Rotation));
            }
        }

        shots.Clear();
        shots.AddRange(newShots);
    }
}