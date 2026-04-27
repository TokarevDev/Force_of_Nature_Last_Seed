using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private const int MAX_CHOICES = 3;

    private readonly RewardDatabase _database;
    private readonly List<RewardRarityWeight> _defaultWeights = new()
    {
        new RewardRarityWeight(RewardRarity.Common, 1f),
        new RewardRarityWeight(RewardRarity.Rare, 0.3f),
        new RewardRarityWeight(RewardRarity.Legendary, 0.1f)
    };

    public RewardRollService(RewardDatabase database)
    {
        _database = database;
    }

    public List<RewardChoiceData> Roll3()
    {
        var result = new List<RewardChoiceData>(MAX_CHOICES);

        if (_database == null)
        {
            Debug.LogWarning("Reward database is not set.");
            return result;
        }

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("Reward database is empty.");
            return result;
        }

        var pools = BuildPools(source);
        int count = Mathf.Min(MAX_CHOICES, CountRewards(pools));

        for (int i = 0; i < count; i++)
        {
            if (!TryRollReward(pools, out RewardModifierEntry selected))
                break;

            result.Add(new RewardChoiceData(selected));
        }

        return result;
    }

    private Dictionary<RewardRarity, List<RewardModifierEntry>> BuildPools(
        IReadOnlyList<RewardModifierEntry> source)
    {
        var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

        foreach (RewardModifierEntry entry in source)
        {
            if (entry == null || entry.Effect == null)
                continue;

            if (!pools.TryGetValue(entry.Rarity, out var pool))
            {
                pool = new List<RewardModifierEntry>();
                pools.Add(entry.Rarity, pool);
            }

            pool.Add(entry);
        }

        return pools;
    }

    private bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (!TryRollRarity(pools, out RewardRarity rarity))
            return false;

        List<RewardModifierEntry> pool = pools[rarity];
        int randomIndex = Random.Range(0, pool.Count);
        selected = pool[randomIndex];
        pool.RemoveAt(randomIndex);

        return true;
    }

    private bool TryRollRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        out RewardRarity selectedRarity)
    {
        selectedRarity = RewardRarity.Common;
        float totalWeight = 0f;

        foreach (RewardRarityWeight weight in GetWeights())
        {
            if (!CanRollRarity(pools, weight))
                continue;

            totalWeight += weight.Weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float current = 0f;

        foreach (RewardRarityWeight weight in GetWeights())
        {
            if (!CanRollRarity(pools, weight))
                continue;

            current += weight.Weight;
            if (roll > current)
                continue;

            selectedRarity = weight.Rarity;
            return true;
        }

        return false;
    }

    private IReadOnlyList<RewardRarityWeight> GetWeights()
    {
        var weights = _database.RarityWeights;

        if (weights == null || weights.Count == 0)
            return _defaultWeights;

        return weights;
    }

    private static bool CanRollRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarityWeight weight)
    {
        return weight != null
            && weight.Weight > 0f
            && pools.TryGetValue(weight.Rarity, out var pool)
            && pool.Count > 0;
    }

    private static int CountRewards(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        int count = 0;

        foreach (var pool in pools.Values)
        {
            count += pool.Count;
        }

        return count;
    }
}
