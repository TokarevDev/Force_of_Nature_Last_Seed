
public sealed class RuntimeModifier
{
    public ShotModifierData Data { get; }
    public IShotModifier Runtime { get; }

    public RuntimeModifier(ShotModifierData data, IShotModifier runtime)
    {
        Data = data;
        Runtime = runtime;
    }
}