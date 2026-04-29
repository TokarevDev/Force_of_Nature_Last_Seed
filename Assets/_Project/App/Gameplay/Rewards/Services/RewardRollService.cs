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

    public List<RewardChoiceData> Roll3(
        WeaponRuntimeState state,
        IReadOnlyList<RewardRarityWeight> rarityWeights = null)
    {
        var result = new List<RewardChoiceData>(MAX_CHOICES);

        if (_database == null)
        {
            Debug.LogWarning("Reward database is not set.");
            return result;
        }

        if (state == null)
        {
            Debug.LogWarning("Cannot roll rewards: weapon runtime state is not initialized.");
            return result;
        }

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("Reward database is empty.");
            return result;
        }

        var pools = BuildPools(source, state);
        IReadOnlyList<RewardRarityWeight> weights = GetWeights(rarityWeights);
        int count = Mathf.Min(MAX_CHOICES, CountRewards(pools));
        var usedCategories = new HashSet<RewardModifierCategory>();
        var usedCategoryRarities = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            if (!TryRollReward(
                    pools,
                    weights,
                    usedCategories,
                    usedCategoryRarities,
                    out RewardModifierEntry selected))
            {
                break;
            }

            result.Add(new RewardChoiceData(selected));
            usedCategories.Add(selected.Category);
            usedCategoryRarities.Add(GetCategoryRarityKey(selected));
        }

        return result;
    }

    private Dictionary<RewardRarity, List<RewardModifierEntry>> BuildPools(
        IReadOnlyList<RewardModifierEntry> source,
        WeaponRuntimeState state)
    {
        var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

        foreach (RewardModifierEntry entry in source)
        {
            if (entry == null || entry.Effect == null)
                continue;

            if (!entry.Effect.CanApply(state))
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
        IReadOnlyList<RewardRarityWeight> weights,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (TryRollReward(
                pools,
                weights,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                out selected))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                weights,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                out selected))
        {
            return true;
        }

        return TryRollReward(
            pools,
            weights,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            out selected);
    }

    private bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        IReadOnlyList<RewardRarityWeight> weights,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (!TryRollRarity(
                pools,
                weights,
                usedCategories,
                usedCategoryRarities,
                mode,
                out RewardRarity rarity))
        {
            return false;
        }

        List<RewardModifierEntry> pool = pools[rarity];
        return TryTakeReward(pool, usedCategories, usedCategoryRarities, mode, out selected);
    }

    private bool TryRollRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        IReadOnlyList<RewardRarityWeight> weights,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardRarity selectedRarity)
    {
        selectedRarity = RewardRarity.Common;
        float totalWeight = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            RewardRarityWeight weight = weights[i];

            if (!CanRollRarity(
                    pools,
                    weight,
                    usedCategories,
                    usedCategoryRarities,
                    mode))
            {
                continue;
            }

            totalWeight += weight.Weight;
        }

        if (totalWeight <= 0f)
        {
            return TryGetAnyAvailableRarity(
                pools,
                usedCategories,
                usedCategoryRarities,
                mode,
                out selectedRarity);
        }

        float roll = Random.value * totalWeight;
        float current = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            RewardRarityWeight weight = weights[i];

            if (!CanRollRarity(
                    pools,
                    weight,
                    usedCategories,
                    usedCategoryRarities,
                    mode))
            {
                continue;
            }

            current += weight.Weight;
            if (roll > current)
                continue;

            selectedRarity = weight.Rarity;
            return true;
        }

        return TryGetAnyAvailableRarity(
            pools,
            usedCategories,
            usedCategoryRarities,
            mode,
            out selectedRarity);
    }

    private static bool TryGetAnyAvailableRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardRarity selectedRarity)
    {
        selectedRarity = RewardRarity.Common;

        foreach (var pool in pools)
        {
            if (pool.Value == null
                || !HasEligibleReward(
                    pool.Value,
                    usedCategories,
                    usedCategoryRarities,
                    mode))
            {
                continue;
            }

            selectedRarity = pool.Key;
            return true;
        }

        return false;
    }

    private IReadOnlyList<RewardRarityWeight> GetWeights(
        IReadOnlyList<RewardRarityWeight> overrideWeights)
    {
        if (HasPositiveWeight(overrideWeights))
            return overrideWeights;

        var weights = _database.RarityWeights;

        if (HasPositiveWeight(weights))
            return weights;

        return _defaultWeights;
    }

    private static bool HasPositiveWeight(IReadOnlyList<RewardRarityWeight> weights)
    {
        if (weights == null)
            return false;

        for (int i = 0; i < weights.Count; i++)
        {
            RewardRarityWeight weight = weights[i];

            if (weight != null && weight.Weight > 0f)
                return true;
        }

        return false;
    }

    private static bool CanRollRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarityWeight weight,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode)
    {
        return weight != null
            && weight.Weight > 0f
            && pools.TryGetValue(weight.Rarity, out var pool)
            && HasEligibleReward(pool, usedCategories, usedCategoryRarities, mode);
    }

    private static bool HasEligibleReward(
        List<RewardModifierEntry> pool,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode)
    {
        if (pool == null)
            return false;

        for (int i = 0; i < pool.Count; i++)
        {
            if (IsEligible(pool[i], usedCategories, usedCategoryRarities, mode))
                return true;
        }

        return false;
    }

    private static bool TryTakeReward(
        List<RewardModifierEntry> pool,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (pool == null || pool.Count == 0)
            return false;

        int eligibleCount = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            if (IsEligible(pool[i], usedCategories, usedCategoryRarities, mode))
                eligibleCount++;
        }

        if (eligibleCount == 0)
            return false;

        int targetIndex = Random.Range(0, eligibleCount);
        int currentIndex = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (!IsEligible(entry, usedCategories, usedCategoryRarities, mode))
                continue;

            if (currentIndex != targetIndex)
            {
                currentIndex++;
                continue;
            }

            selected = entry;
            pool.RemoveAt(i);
            return true;
        }

        return false;
    }

    private static bool IsEligible(
        RewardModifierEntry entry,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode)
    {
        if (entry == null)
            return false;

        return mode switch
        {
            RewardPickMode.UniqueCategory => !usedCategories.Contains(entry.Category),
            RewardPickMode.UniqueCategoryRarity => !usedCategoryRarities.Contains(GetCategoryRarityKey(entry)),
            _ => true
        };
    }

    private static int GetCategoryRarityKey(RewardModifierEntry entry)
    {
        return ((int)entry.Category * 10) + (int)entry.Rarity;
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

    private enum RewardPickMode
    {
        UniqueCategory,
        UniqueCategoryRarity,
        Any
    }
}
