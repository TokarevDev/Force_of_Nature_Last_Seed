using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces base shot with multiple angled shots.
/// Uses only base shot to avoid exponential multiplication.
/// </summary>
public sealed class SpreadModifier : IShotModifier
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
        if (_count <= 1 || _angle <= 0f || shots.Count == 0)
            return;

        var baseShot = shots[0];

        shots.Clear();

        float half = _angle * 0.5f;
        float step = _angle / (_count - 1);

        for (int i = 0; i < _count; i++)
        {
            float offset = -half + step * i;

            Quaternion rot = baseShot.Rotation * Quaternion.Euler(0f, 0f, offset);

            shots.Add(new ShotSpawnData(baseShot.Position, rot));
        }
    }
}