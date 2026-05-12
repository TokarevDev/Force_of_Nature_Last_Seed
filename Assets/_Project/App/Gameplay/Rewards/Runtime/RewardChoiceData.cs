public sealed class RewardChoiceData
{
    public RewardEffect Effect { get; }
    public RewardModifierCategory Category { get; }
    public RewardRarity Rarity { get; }
    public string Title { get; }
    public string Description { get; }
    public string ValueText { get; }

    public RewardChoiceData(RewardModifierEntry entry)
    {
        Effect = entry.Effect;
        Category = entry.Category;
        Rarity = entry.Rarity;
        Title = entry.Title;
        Description = entry.Description;
        ValueText = entry.ValueText;
    }
}
