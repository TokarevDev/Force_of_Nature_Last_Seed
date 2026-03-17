using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces a single shot with multiple shots spread across an angle.
/// </summary>
public class SpreadModifier : IShotModifier
{
    private readonly int _count;
    private readonly float _angle;

    public SpreadModifier(int count, float angle)
    {
        _count = count;
        _angle = angle;
    }

    public void Apply(List<ShotSpawnData> shots, ShotContext context)
    {
        if (_count <= 1 || _angle <= 0f) return;

        var newShots = new List<ShotSpawnData>();

        foreach (var shot in shots)
        {
            float half = _angle * 0.5f;
            float step = _angle / (_count - 1);

            for (int i = 0; i < _count; i++)
            {
                float offset = -half + step * i;

                Quaternion rot = shot.Rotation * Quaternion.Euler(0f, 0f, offset);

                newShots.Add(new ShotSpawnData(shot.Position, rot));
            }
        }

        shots.Clear();
        shots.AddRange(newShots);
    }
}