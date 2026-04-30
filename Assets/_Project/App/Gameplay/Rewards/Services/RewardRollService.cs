using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private const int MAX_CHOICES = 3;

    private readonly RewardDatabase _database;
    private readonly List<RewardRaritySlot> _defaultSlots = new()
    {
        new RewardRaritySlot(RewardRarity.Common),
        new RewardRaritySlot(RewardRarity.Common),
        new RewardRaritySlot(RewardRarity.Common)
    };

    public RewardRollService(RewardDatabase database)
    {
        _database = database;
    }

    public List<RewardChoiceData> Roll3(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile = null)
    {
        var result = new List<RewardChoiceData>(MAX_CHOICES);

        if (_database == null)
        {
            Debug.LogWarning("Reward database is not set.");
            return result;
        }

        if (context == null)
        {
            Debug.LogWarning("Cannot roll rewards: runtime context is not initialized.");
            return result;
        }

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("Reward database is empty.");
            return result;
        }

        var pools = BuildPools(source, context);
        IReadOnlyList<RewardRaritySlot> slots = GetSlots(cocoonProfile);
        int count = Mathf.Min(MAX_CHOICES, Mathf.Min(CountRewards(pools), slots.Count));
        var usedCategories = new HashSet<RewardModifierCategory>();
        var usedCategoryRarities = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            RewardRarity rarity = slots[i] != null
                ? slots[i].RollRarity()
                : RewardRarity.Common;

            if (!TryRollRewardForRarity(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    out RewardModifierEntry selected)
                && !TryRollReward(
                    pools,
                    usedCategories,
                    usedCategoryRarities,
                    out selected))
            {
                break;
            }

            result.Add(new RewardChoiceData(selected));
            usedCategories.Add(selected.Category);
            usedCategoryRarities.Add(GetCategoryRarityKey(selected));
        }

        return result;
    }

    private bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                out selected))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                out selected))
        {
            return true;
        }

        return TryRollRewardForRarity(
            pools,
            rarity,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            out selected);
    }

    private static bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (!pools.TryGetValue(rarity, out var pool))
            return false;

        return TryTakeReward(pool, usedCategories, usedCategoryRarities, mode, out selected);
    }

    private Dictionary<RewardRarity, List<RewardModifierEntry>> BuildPools(
        IReadOnlyList<RewardModifierEntry> source,
        RewardRuntimeContext context)
    {
        var pools = new Dictionary<RewardRarity, List<RewardModifierEntry>>();

        foreach (RewardModifierEntry entry in source)
        {
            if (entry == null || entry.Effect == null)
                continue;

            if (!entry.Effect.CanApply(context))
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

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                out selected))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                out selected))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            out selected);
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected)
    {
        selected = null;

        if (pools == null || pools.Count == 0)
            return false;

        float totalWeight = 0f;

        foreach (var pool in pools.Values)
        {
            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (IsEligible(entry, usedCategories, usedCategoryRarities, mode))
                    totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var pool in pools.Values)
        {
            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (!IsEligible(entry, usedCategories, usedCategoryRarities, mode))
                    continue;

                currentWeight += entry.Weight;

                if (roll > currentWeight)
                    continue;

                selected = entry;
                pool.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<RewardRaritySlot> GetSlots(
        CocoonRewardProfile cocoonProfile)
    {
        if (HasRaritySlots(cocoonProfile?.RaritySlots))
            return cocoonProfile.RaritySlots;

        return _defaultSlots;
    }

    private static bool HasRaritySlots(IReadOnlyList<RewardRaritySlot> slots)
    {
        return slots != null && slots.Count > 0;
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

        float totalWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (IsEligible(entry, usedCategories, usedCategoryRarities, mode))
                totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (!IsEligible(entry, usedCategories, usedCategoryRarities, mode))
                continue;

            currentWeight += entry.Weight;

            if (roll > currentWeight)
                continue;

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

        if (entry.Weight <= 0f)
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
