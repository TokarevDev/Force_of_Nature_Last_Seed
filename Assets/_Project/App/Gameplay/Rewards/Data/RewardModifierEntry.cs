using System;
using UnityEngine;

public enum RewardModifierCategory
{
    None = 0,
    Damage = 1,
    FireRate = 2,
    CriticalChance = 3,
    CriticalPower = 4,
    Penetration = 5,
    ParallelProjectiles = 6,
    Salvo = 7,
    Spread = 8
}

[Serializable]
public sealed class RewardModifierEntry
{
    [SerializeField] private RewardEffect _effect;
    [SerializeField] private RewardModifierCategory _category = RewardModifierCategory.None;
    [SerializeField] private RewardRarity _rarity = RewardRarity.Common;
    [SerializeField] private string _title;
    [SerializeField][TextArea(2, 4)] private string _description;

    public RewardEffect Effect => _effect;
    public RewardModifierCategory Category => _category;
    public RewardRarity Rarity => _rarity;
    public string Title => _title;
    public string Description => _description;
}
