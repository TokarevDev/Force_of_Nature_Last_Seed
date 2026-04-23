
public sealed class RewardChoiceData
{
    public ShotModifierData Modifier { get; }
    public string Title { get; }
    public string Description { get; }

    public RewardChoiceData(RewardModifierEntry entry)
    {
        Modifier = entry.Modifier;
        Title = entry.Title;
        Description = entry.Description;
    }
}