public sealed class RewardChoiceData
{
    public RewardEffect Effect { get; }
    public string Title { get; }
    public string Description { get; }

    public RewardChoiceData(RewardModifierEntry entry)
    {
        Effect = entry.Effect;
        Title = entry.Title;
        Description = entry.Description;
    }
}