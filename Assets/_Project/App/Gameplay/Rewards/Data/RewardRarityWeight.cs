using System;
using UnityEngine;

[Serializable]
public sealed class RewardRarityWeight
{
    [SerializeField] private RewardRarity _rarity;
    [SerializeField][Min(0f)] private float _weight = 1f;

    public RewardRarityWeight()
    {
    }

    public RewardRarityWeight(RewardRarity rarity, float weight)
    {
        _rarity = rarity;
        _weight = Mathf.Max(0f, weight);
    }

    public RewardRarity Rarity => _rarity;
    public float Weight => _weight;
}
