using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modifiers/Spread")]
public sealed class SpreadModifierData : ShotModifierData
{
    [Min(2)]
    public int Count = 3;

    [Min(1f)]
    public float Angle = 30f;
}
