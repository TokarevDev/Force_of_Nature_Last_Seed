using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Reward Database")]
public sealed class RewardDatabase : ScriptableObject
{
    [SerializeField] private List<RewardRarityWeight> _rarityWeights = new();
    [SerializeField] private List<RewardModifierEntry> _rewards;

    public IReadOnlyList<RewardRarityWeight> RarityWeights => _rarityWeights;
    public IReadOnlyList<RewardModifierEntry> Rewards => _rewards;

    private void Reset()
    {
        EnsureDefaultRarityWeights();
    }

    private void OnValidate()
    {
        EnsureDefaultRarityWeights();
    }

    private void EnsureDefaultRarityWeights()
    {
        if (_rarityWeights == null)
            _rarityWeights = new List<RewardRarityWeight>();

        if (_rarityWeights.Count > 0)
            return;

        _rarityWeights.Add(new RewardRarityWeight(RewardRarity.Common, 1f));
        _rarityWeights.Add(new RewardRarityWeight(RewardRarity.Rare, 0.3f));
        _rarityWeights.Add(new RewardRarityWeight(RewardRarity.Legendary, 0.1f));
    }
}
