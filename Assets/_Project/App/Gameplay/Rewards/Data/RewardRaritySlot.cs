using System;
using UnityEngine;

[Serializable]
public sealed class RewardRaritySlot
{
    [SerializeField] private RewardRarity _rarity = RewardRarity.Common;
    [SerializeField] private RewardRarity _alternateRarity = RewardRarity.Legendary;
    [SerializeField][Range(0f, 1f)] private float _alternateChance;

    public RewardRarity Rarity => _rarity;
    public RewardRarity AlternateRarity => _alternateRarity;
    public float AlternateChance => Mathf.Clamp01(_alternateChance);

    public RewardRaritySlot()
    {
    }

    public RewardRaritySlot(RewardRarity rarity)
        : this(rarity, RewardRarity.Legendary, 0f)
    {
    }

    public RewardRaritySlot(
        RewardRarity rarity,
        RewardRarity alternateRarity,
        float alternateChance)
    {
        _rarity = rarity;
        _alternateRarity = alternateRarity;
        _alternateChance = Mathf.Clamp01(alternateChance);
    }

    public RewardRarity RollRarity()
    {
        if (_alternateChance <= 0f)
            return _rarity;

        return UnityEngine.Random.value < _alternateChance
            ? _alternateRarity
            : _rarity;
    }

    public RewardRaritySlot Clone()
    {
        return new RewardRaritySlot(
            _rarity,
            _alternateRarity,
            _alternateChance);
    }
}
