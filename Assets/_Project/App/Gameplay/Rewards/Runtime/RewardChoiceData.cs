public sealed class RewardChoiceData
{
    public ShotModifierData Modifier { get; }

    public RewardChoiceData(ShotModifierData modifier)
    {
        Modifier = modifier;
    }
}