public sealed class RewardChoiceData
{
    public RewardEffect Effect { get; }
    public RewardRarity Rarity { get; }
    public string Title { get; }
    public string Description { get; }

    public RewardChoiceData(RewardModifierEntry entry)
    {
        Effect = entry.Effect;
        Rarity = entry.Rarity;
        Title = entry.Title;
        Description = entry.Description;
    }
}
