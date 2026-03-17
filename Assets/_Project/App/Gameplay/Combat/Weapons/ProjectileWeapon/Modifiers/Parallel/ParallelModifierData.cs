using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modifiers/Parallel")]
public sealed class ParallelModifierData : ShotModifierData
{
    [Min(2)]
    public int Count = 2;

    [Min(0.1f)]
    public float Spacing = 0.5f;

    public override IShotModifier CreateRuntime() => new ParallelModifier(Count, Spacing);

}