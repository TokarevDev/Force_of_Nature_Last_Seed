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
        CocoonRewardProfile cocoonProfile = null,
        RewardRarity? guaranteedRarity = null,
        int guaranteedRaritySlotCount = 1,
        RewardRollContext rollContext = default)
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
        int guaranteedSlotCount = guaranteedRarity.HasValue && count > 0
            ? Mathf.Clamp(guaranteedRaritySlotCount, 1, count)
            : 0;
        RewardRarity[] slotRarities = guaranteedRarity.HasValue
            ? BuildGuaranteedSlotRarities(
                slots,
                count,
                guaranteedRarity.Value,
                guaranteedSlotCount,
                pools)
            : BuildSlotRarities(slots, count);
        ApplyProgressRarityBias(
            slotRarities,
            guaranteedSlotCount,
            pools,
            rollContext);

        bool useLegendaryProfileRules = cocoonProfile != null
            && cocoonProfile.GuaranteesLegendaryReward;

        if (useLegendaryProfileRules)
        {
            ApplySecondaryLegendaryRolls(
                slotRarities,
                guaranteedSlotCount,
                cocoonProfile.SecondaryLegendaryChance,
                pools);
        }

        List<RewardWeaponGroup> requiredWeaponGroups = GetRequiredWeaponGroups(pools, context);
        var usedCategories = new HashSet<RewardModifierCategory>();
        var usedCategoryRarities = new HashSet<int>();
        var usedWeaponGroups = new HashSet<RewardWeaponGroup>();
        int weaponUnlockSlotIndex = GetWeaponUnlockSlotIndex(
            pools,
            rollContext,
            useLegendaryProfileRules,
            guaranteedSlotCount,
            count);

        for (int i = 0; i < count; i++)
        {
            RewardRarity rarity = slotRarities[i];
            bool allowLegendaryFallback = useLegendaryProfileRules
                || rarity == RewardRarity.Legendary;
            RewardWeaponGroup forcedWeaponGroup = i < guaranteedSlotCount
                ? RewardWeaponGroup.None
                : GetForcedWeaponGroup(
                    requiredWeaponGroups,
                    usedWeaponGroups,
                    count - i);

            RewardModifierEntry selected = null;
            bool isSelected = false;

            if (i == weaponUnlockSlotIndex && rarity != RewardRarity.Legendary)
            {
                isSelected = TryRollWeaponUnlockReward(
                    pools,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext);
            }

            if (!isSelected)
            {
                isSelected = forcedWeaponGroup != RewardWeaponGroup.None
                    ? TryRollRewardForWeaponGroup(
                        pools,
                        rarity,
                        forcedWeaponGroup,
                        usedCategories,
                        usedCategoryRarities,
                        out selected,
                        allowLegendaryFallback,
                        rollContext)
                    : TryRollRewardForRarity(
                        pools,
                        rarity,
                        usedCategories,
                        usedCategoryRarities,
                        out selected,
                        rollContext);
            }

            if (!isSelected && useLegendaryProfileRules)
            {
                isSelected = TryRollPremiumReward(
                    pools,
                    rarity,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext);
            }

            if (!isSelected
                && !TryRollReward(
                    pools,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    allowLegendaryFallback,
                    rollContext))
            {
                break;
            }

            result.Add(new RewardChoiceData(selected));
            usedCategories.Add(selected.Category);
            usedCategoryRarities.Add(GetCategoryRarityKey(selected));

            RewardWeaponGroup selectedWeaponGroup = GetWeaponGroup(selected);

            if (selectedWeaponGroup != RewardWeaponGroup.None)
                usedWeaponGroups.Add(selectedWeaponGroup);
        }

        return result;
    }

    public RewardRarity RollGuaranteeRarity(
        RewardRuntimeContext context,
        CocoonRewardProfile cocoonProfile = null,
        RewardRollContext rollContext = default)
    {
        if (_database == null || context == null)
            return RewardRarity.Common;

        var source = _database.Rewards;

        if (source == null || source.Count == 0)
            return RewardRarity.Common;

        var pools = BuildPools(source, context);

        if (cocoonProfile != null && cocoonProfile.GuaranteesLegendaryReward)
            return HasRewardsForRarity(pools, RewardRarity.Legendary)
                ? RewardRarity.Legendary
                : GetHighestAvailableRarity(pools);

        IReadOnlyList<RewardRaritySlot> slots = GetSlots(cocoonProfile);

        float commonWeight = 0f;
        float rareWeight = 0f;
        float legendaryWeight = 0f;

        for (int i = 0; i < slots.Count; i++)
        {
            RewardRaritySlot slot = slots[i];

            if (slot == null)
                continue;

            AddGuaranteeWeight(
                slot.Rarity,
                1f - slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);

            AddGuaranteeWeight(
                slot.AlternateRarity,
                slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);
        }

        ApplyProgressGuaranteeRarityBias(
            rollContext,
            ref commonWeight,
            ref rareWeight,
            ref legendaryWeight);

        float premiumWeight = rareWeight + legendaryWeight;

        if (premiumWeight > 0f)
        {
            float premiumRoll = Random.value * premiumWeight;
            return premiumRoll < rareWeight
                ? RewardRarity.Rare
                : RewardRarity.Legendary;
        }

        return commonWeight > 0f
            ? RewardRarity.Common
            : GetHighestAvailableRarity(pools);
    }

    private bool TryRollRewardForWeaponGroup(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        RewardWeaponGroup weaponGroup,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        bool allowLegendaryFallback,
        RewardRollContext rollContext)
    {
        selected = null;

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                weaponGroup,
                out selected,
                rollContext))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                weaponGroup,
                out selected,
                rollContext))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.Any,
                weaponGroup,
                out selected,
                rollContext))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            weaponGroup,
            out selected,
            allowLegendaryFallback,
            rollContext);
    }

    private bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                RewardWeaponGroup.None,
                out selected,
                rollContext))
        {
            return true;
        }

        if (TryRollRewardForRarity(
                pools,
                rarity,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                RewardWeaponGroup.None,
                out selected,
                rollContext))
        {
            return true;
        }

        return TryRollRewardForRarity(
            pools,
            rarity,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            RewardWeaponGroup.None,
            out selected,
            rollContext);
    }

    private bool TryRollPremiumReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity preferredRarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (preferredRarity == RewardRarity.Legendary)
        {
            return TryRollRewardForRarity(
                    pools,
                    RewardRarity.Legendary,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext)
                || TryRollRewardForRarity(
                    pools,
                    RewardRarity.Rare,
                    usedCategories,
                    usedCategoryRarities,
                    out selected,
                    rollContext);
        }

        return TryRollRewardForRarity(
                pools,
                RewardRarity.Rare,
                usedCategories,
                usedCategoryRarities,
                out selected,
                rollContext)
            || TryRollRewardForRarity(
                pools,
                RewardRarity.Legendary,
                usedCategories,
                usedCategoryRarities,
                out selected,
                rollContext);
    }

    private static bool TryRollWeaponUnlockReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (TryRollWeaponUnlockReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                out selected,
                rollContext))
        {
            return true;
        }

        if (TryRollWeaponUnlockReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                out selected,
                rollContext))
        {
            return true;
        }

        return TryRollWeaponUnlockReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            out selected,
            rollContext);
    }

    private static bool TryRollWeaponUnlockReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (pools == null || pools.Count == 0)
            return false;

        float totalWeight = 0f;

        foreach (var rarityPool in pools)
        {
            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (IsWeaponUnlockReward(entry)
                    && IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        RewardWeaponGroup.None))
                {
                    totalWeight += GetEffectiveWeight(entry, rollContext);
                }
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var rarityPool in pools)
        {
            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (!IsWeaponUnlockReward(entry)
                    || !IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        RewardWeaponGroup.None))
                {
                    continue;
                }

                currentWeight += GetEffectiveWeight(entry, rollContext);

                if (roll > currentWeight)
                    continue;

                selected = entry;
                pool.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private static bool TryRollRewardForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (!pools.TryGetValue(rarity, out var pool))
            return false;

        return TryTakeReward(
            pool,
            usedCategories,
            usedCategoryRarities,
            mode,
            requiredWeaponGroup,
            out selected,
            rollContext);
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
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default)
    {
        selected = null;

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                RewardWeaponGroup.None,
                out selected,
                allowLegendary,
                rollContext))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                RewardWeaponGroup.None,
                out selected,
                allowLegendary,
                rollContext))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            RewardWeaponGroup.None,
            out selected,
            allowLegendary,
            rollContext);
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default)
    {
        selected = null;

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategory,
                requiredWeaponGroup,
                out selected,
                allowLegendary,
                rollContext))
        {
            return true;
        }

        if (TryRollReward(
                pools,
                usedCategories,
                usedCategoryRarities,
                RewardPickMode.UniqueCategoryRarity,
                requiredWeaponGroup,
                out selected,
                allowLegendary,
                rollContext))
        {
            return true;
        }

        return TryRollReward(
            pools,
            usedCategories,
            usedCategoryRarities,
            RewardPickMode.Any,
            requiredWeaponGroup,
            out selected,
            allowLegendary,
            rollContext);
    }

    private static bool TryRollReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        HashSet<RewardModifierCategory> usedCategories,
        HashSet<int> usedCategoryRarities,
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        bool allowLegendary = true,
        RewardRollContext rollContext = default)
    {
        selected = null;

        if (pools == null || pools.Count == 0)
            return false;

        float totalWeight = 0f;

        foreach (var rarityPool in pools)
        {
            if (!allowLegendary && rarityPool.Key == RewardRarity.Legendary)
                continue;

            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        requiredWeaponGroup))
                {
                    totalWeight += GetEffectiveWeight(entry, rollContext);
                }
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var rarityPool in pools)
        {
            if (!allowLegendary && rarityPool.Key == RewardRarity.Legendary)
                continue;

            List<RewardModifierEntry> pool = rarityPool.Value;

            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                RewardModifierEntry entry = pool[i];

                if (!IsEligible(
                        entry,
                        usedCategories,
                        usedCategoryRarities,
                        mode,
                        requiredWeaponGroup))
                {
                    continue;
                }

                currentWeight += GetEffectiveWeight(entry, rollContext);

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

    private static RewardRarity[] BuildSlotRarities(
        IReadOnlyList<RewardRaritySlot> slots,
        int count)
    {
        var result = new RewardRarity[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = slots[i] != null
                ? slots[i].RollRarity()
                : RewardRarity.Common;
        }

        return result;
    }

    private static RewardRarity[] BuildGuaranteedSlotRarities(
        IReadOnlyList<RewardRaritySlot> slots,
        int count,
        RewardRarity guaranteedRarity,
        int guaranteedSlotCount,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        var result = new RewardRarity[count];

        if (count == 0)
            return result;

        guaranteedSlotCount = Mathf.Clamp(guaranteedSlotCount, 1, count);

        for (int i = 0; i < guaranteedSlotCount; i++)
        {
            result[i] = guaranteedRarity;
        }

        float commonWeight = 0f;
        float rareWeight = 0f;
        float legendaryWeight = 0f;

        CollectAvailableRarityWeights(
            slots,
            guaranteedSlotCount,
            pools,
            ref commonWeight,
            ref rareWeight,
            ref legendaryWeight);

        for (int i = guaranteedSlotCount; i < count; i++)
        {
            result[i] = RollRarityFromWeights(
                commonWeight,
                rareWeight,
                legendaryWeight);
        }

        return result;
    }

    private static void ApplySecondaryLegendaryRolls(
        RewardRarity[] slotRarities,
        int guaranteedSlotCount,
        float secondaryLegendaryChance,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (slotRarities == null || slotRarities.Length == 0)
            return;

        if (secondaryLegendaryChance <= 0f)
            return;

        if (!HasRewardsForRarity(pools, RewardRarity.Legendary))
            return;

        int startIndex = Mathf.Clamp(
            guaranteedSlotCount,
            0,
            slotRarities.Length);

        for (int i = startIndex; i < slotRarities.Length; i++)
        {
            if (slotRarities[i] == RewardRarity.Legendary)
                continue;

            if (Random.value < secondaryLegendaryChance)
                slotRarities[i] = RewardRarity.Legendary;
        }
    }

    private static void AddGuaranteeWeight(
        RewardRarity rarity,
        float weight,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        ref float commonWeight,
        ref float rareWeight,
        ref float legendaryWeight)
    {
        if (weight <= 0f)
            return;

        if (!HasRewardsForRarity(pools, rarity))
            return;

        switch (rarity)
        {
            case RewardRarity.Rare:
                rareWeight += weight;
                break;

            case RewardRarity.Legendary:
                legendaryWeight += weight;
                break;

            default:
                commonWeight += weight;
                break;
        }
    }

    private static void CollectAvailableRarityWeights(
        IReadOnlyList<RewardRaritySlot> slots,
        int startIndex,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        ref float commonWeight,
        ref float rareWeight,
        ref float legendaryWeight)
    {
        if (slots == null)
            return;

        startIndex = Mathf.Clamp(startIndex, 0, slots.Count);

        for (int i = startIndex; i < slots.Count; i++)
        {
            RewardRaritySlot slot = slots[i];

            if (slot == null)
                continue;

            AddGuaranteeWeight(
                slot.Rarity,
                1f - slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);

            AddGuaranteeWeight(
                slot.AlternateRarity,
                slot.AlternateChance,
                pools,
                ref commonWeight,
                ref rareWeight,
                ref legendaryWeight);
        }
    }

    private static RewardRarity RollRarityFromWeights(
        float commonWeight,
        float rareWeight,
        float legendaryWeight)
    {
        float totalWeight = commonWeight + rareWeight + legendaryWeight;

        if (totalWeight <= 0f)
            return RewardRarity.Common;

        float roll = Random.value * totalWeight;

        if (roll < commonWeight)
            return RewardRarity.Common;

        roll -= commonWeight;

        if (roll < rareWeight)
            return RewardRarity.Rare;

        return RewardRarity.Legendary;
    }

    private static void ApplyProgressRarityBias(
        RewardRarity[] slotRarities,
        int protectedSlotCount,
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRollContext rollContext)
    {
        if (slotRarities == null || slotRarities.Length == 0)
            return;

        bool hasRare = HasRewardsForRarity(pools, RewardRarity.Rare);
        bool hasLegendary = HasRewardsForRarity(pools, RewardRarity.Legendary);

        if (!hasRare && !hasLegendary)
            return;

        float pressureProgress = GetRewardPressureProgress(rollContext);
        float commonToRareChance = 0f;
        float rareToLegendaryChance = 0f;

        if (pressureProgress >= 0.65f)
        {
            commonToRareChance = 0.85f;
            rareToLegendaryChance = 0.45f;
        }
        else if (pressureProgress >= 0.35f)
        {
            commonToRareChance = Mathf.Lerp(0.45f, 0.7f, Mathf.InverseLerp(0.35f, 0.65f, pressureProgress));
            rareToLegendaryChance = Mathf.Lerp(0.15f, 0.32f, Mathf.InverseLerp(0.35f, 0.65f, pressureProgress));
        }
        else if (pressureProgress >= 0.18f)
        {
            commonToRareChance = 0.3f;
            rareToLegendaryChance = 0.05f;
        }

        if (rollContext.HasRevivedThisRun)
        {
            commonToRareChance = Mathf.Max(commonToRareChance, 0.65f);
            rareToLegendaryChance = Mathf.Max(rareToLegendaryChance, 0.28f);
        }

        protectedSlotCount = Mathf.Clamp(protectedSlotCount, 0, slotRarities.Length);

        for (int i = protectedSlotCount; i < slotRarities.Length; i++)
        {
            if (slotRarities[i] == RewardRarity.Common && hasRare && Random.value < commonToRareChance)
            {
                slotRarities[i] = RewardRarity.Rare;
                continue;
            }

            if (slotRarities[i] == RewardRarity.Rare && hasLegendary && Random.value < rareToLegendaryChance)
                slotRarities[i] = RewardRarity.Legendary;
        }
    }

    private static void ApplyProgressGuaranteeRarityBias(
        RewardRollContext rollContext,
        ref float commonWeight,
        ref float rareWeight,
        ref float legendaryWeight)
    {
        float pressureProgress = GetRewardPressureProgress(rollContext);

        if (pressureProgress >= 0.65f)
        {
            commonWeight *= 0.15f;
            rareWeight *= 3.5f;
            legendaryWeight *= 5f;
        }
        else if (pressureProgress >= 0.35f)
        {
            float t = Mathf.InverseLerp(0.35f, 0.65f, pressureProgress);
            commonWeight *= Mathf.Lerp(0.65f, 0.35f, t);
            rareWeight *= Mathf.Lerp(1.7f, 2.8f, t);
            legendaryWeight *= Mathf.Lerp(1.5f, 3.2f, t);
        }
        else if (pressureProgress >= 0.18f)
        {
            commonWeight *= 0.75f;
            rareWeight *= 1.55f;
            legendaryWeight *= 1.35f;
        }

        if (!rollContext.HasRevivedThisRun)
            return;

        commonWeight *= 0.35f;
        rareWeight *= 2.2f;
        legendaryWeight *= 3.2f;
    }

    private static float GetEffectiveWeight(
        RewardModifierEntry entry,
        RewardRollContext rollContext)
    {
        if (entry == null)
            return 0f;

        float baseWeight = entry.Weight;

        if (baseWeight <= 0f)
            return 0f;

        float pressureProgress = GetRewardPressureProgress(rollContext);
        float multiplier = GetCategoryPressureMultiplier(entry.Category, pressureProgress);

        if (rollContext.HasRevivedThisRun)
            multiplier *= GetReviveMultiplier(entry.Category, pressureProgress);

        return Mathf.Max(0.01f, baseWeight * multiplier);
    }

    private static float GetCategoryPressureMultiplier(
        RewardModifierCategory category,
        float pressureProgress)
    {
        if (category == RewardModifierCategory.AcaciaThornUnlock)
        {
            if (pressureProgress < 0.22f)
                return 4f;

            if (pressureProgress < 0.5f)
                return 1.25f;

            if (pressureProgress < 0.65f)
                return 0.45f;

            return 0.15f;
        }

        if (IsDirectDpsCategory(category))
        {
            float multiplier = pressureProgress switch
            {
                >= 0.65f => 12f,
                >= 0.35f => Mathf.Lerp(3.5f, 8f, Mathf.InverseLerp(0.35f, 0.65f, pressureProgress)),
                >= 0.18f => 2.2f,
                _ => 0.9f
            };

            if (IsDamageMultiplierCategory(category) && pressureProgress >= 0.35f)
                multiplier *= pressureProgress >= 0.65f ? 1.5f : 1.2f;

            return multiplier;
        }

        if (IsUtilityCategory(category))
        {
            if (pressureProgress >= 0.65f)
                return 0.08f;

            if (pressureProgress >= 0.35f)
                return 0.45f;

            return 0.9f;
        }

        return 1f;
    }

    private static float GetReviveMultiplier(
        RewardModifierCategory category,
        float pressureProgress)
    {
        if (IsDirectDpsCategory(category))
            return pressureProgress >= 0.8f ? 2.5f : 1.8f;

        if (category == RewardModifierCategory.AcaciaThornUnlock)
            return 0.7f;

        return 0.35f;
    }

    private static float GetRewardPressureProgress(RewardRollContext rollContext)
    {
        return Mathf.Clamp01(Mathf.Max(
            rollContext.HeadPathProgressNormalized,
            rollContext.WormDestructionProgressNormalized));
    }

    private static bool IsDirectDpsCategory(RewardModifierCategory category)
    {
        return category is RewardModifierCategory.Damage
            or RewardModifierCategory.FireRate
            or RewardModifierCategory.CriticalChance
            or RewardModifierCategory.CriticalPower
            or RewardModifierCategory.Penetration
            or RewardModifierCategory.ParallelProjectiles
            or RewardModifierCategory.Salvo
            or RewardModifierCategory.AcaciaThornDamage
            or RewardModifierCategory.AcaciaThornFireRate
            or RewardModifierCategory.AcaciaThornSalvo
            or RewardModifierCategory.AcaciaThornCriticalChance
            or RewardModifierCategory.AcaciaThornCriticalPower;
    }

    private static bool IsDamageMultiplierCategory(RewardModifierCategory category)
    {
        return category is RewardModifierCategory.Damage
            or RewardModifierCategory.AcaciaThornDamage;
    }

    private static bool IsUtilityCategory(RewardModifierCategory category)
    {
        return category is RewardModifierCategory.ProjectileSpeed
            or RewardModifierCategory.AcaciaThornProjectileSpeed;
    }

    private static bool HasRewardsForRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRarity rarity)
    {
        return pools != null
            && pools.TryGetValue(rarity, out var pool)
            && pool != null
            && pool.Count > 0;
    }

    private static bool HasAvailableWeaponUnlockReward(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (pools == null)
            return false;

        foreach (var pool in pools.Values)
        {
            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                if (IsWeaponUnlockReward(pool[i]))
                    return true;
            }
        }

        return false;
    }

    private static int GetWeaponUnlockSlotIndex(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRollContext rollContext,
        bool useLegendaryProfileRules,
        int guaranteedSlotCount,
        int choiceCount)
    {
        if (choiceCount <= 0)
            return -1;

        if (!HasAvailableWeaponUnlockReward(pools))
            return -1;

        if (useLegendaryProfileRules && guaranteedSlotCount < choiceCount)
            return guaranteedSlotCount;

        float pressureProgress = GetRewardPressureProgress(rollContext);
        float chance;

        if (pressureProgress < 0.18f)
            chance = 0.75f;
        else if (pressureProgress < 0.35f)
            chance = 0.45f;
        else if (pressureProgress < 0.55f)
            chance = 0.2f;
        else
            chance = 0.05f;

        if (rollContext.HasRevivedThisRun)
            chance *= 0.5f;

        if (Random.value >= chance)
            return -1;

        return Mathf.Clamp(guaranteedSlotCount, 0, choiceCount - 1);
    }

    private static RewardRarity GetHighestAvailableRarity(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools)
    {
        if (HasRewardsForRarity(pools, RewardRarity.Legendary))
            return RewardRarity.Legendary;

        if (HasRewardsForRarity(pools, RewardRarity.Rare))
            return RewardRarity.Rare;

        return RewardRarity.Common;
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
        RewardWeaponGroup requiredWeaponGroup,
        out RewardModifierEntry selected,
        RewardRollContext rollContext)
    {
        selected = null;

        if (pool == null || pool.Count == 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (IsEligible(
                    entry,
                    usedCategories,
                    usedCategoryRarities,
                    mode,
                    requiredWeaponGroup))
            {
                totalWeight += GetEffectiveWeight(entry, rollContext);
            }
        }

        if (totalWeight <= 0f)
            return false;

        float roll = Random.value * totalWeight;
        float currentWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            RewardModifierEntry entry = pool[i];

            if (!IsEligible(
                    entry,
                    usedCategories,
                    usedCategoryRarities,
                    mode,
                    requiredWeaponGroup))
            {
                continue;
            }

            currentWeight += GetEffectiveWeight(entry, rollContext);

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
        RewardPickMode mode,
        RewardWeaponGroup requiredWeaponGroup)
    {
        if (entry == null)
            return false;

        if (entry.Weight <= 0f)
            return false;

        if (requiredWeaponGroup != RewardWeaponGroup.None
            && GetWeaponGroup(entry) != requiredWeaponGroup)
        {
            return false;
        }

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

    private static bool IsWeaponUnlockReward(RewardModifierEntry entry)
    {
        return entry != null
            && entry.Category == RewardModifierCategory.AcaciaThornUnlock;
    }

    private static List<RewardWeaponGroup> GetRequiredWeaponGroups(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardRuntimeContext context)
    {
        var result = new List<RewardWeaponGroup>(2);

        if (!HasAdditionalWeaponUnlocked(context))
            return result;

        if (HasAnyRewardForWeaponGroup(pools, RewardWeaponGroup.MainWeapon))
            result.Add(RewardWeaponGroup.MainWeapon);

        if (HasAnyRewardForWeaponGroup(pools, RewardWeaponGroup.AcaciaThorn))
            result.Add(RewardWeaponGroup.AcaciaThorn);

        if (result.Count >= 2)
            return result;

        result.Clear();
        return result;
    }

    private static bool HasAdditionalWeaponUnlocked(RewardRuntimeContext context)
    {
        AcaciaThornRuntimeState acaciaState = context?.AcaciaThornState;
        return acaciaState != null && acaciaState.IsUnlocked;
    }

    private static bool HasAnyRewardForWeaponGroup(
        Dictionary<RewardRarity, List<RewardModifierEntry>> pools,
        RewardWeaponGroup weaponGroup)
    {
        if (pools == null)
            return false;

        foreach (var pool in pools.Values)
        {
            if (pool == null)
                continue;

            for (int i = 0; i < pool.Count; i++)
            {
                if (GetWeaponGroup(pool[i]) == weaponGroup)
                    return true;
            }
        }

        return false;
    }

    private static RewardWeaponGroup GetForcedWeaponGroup(
        List<RewardWeaponGroup> requiredWeaponGroups,
        HashSet<RewardWeaponGroup> usedWeaponGroups,
        int remainingSlots)
    {
        if (requiredWeaponGroups == null || requiredWeaponGroups.Count == 0)
            return RewardWeaponGroup.None;

        int missingCount = 0;
        RewardWeaponGroup firstMissing = RewardWeaponGroup.None;

        for (int i = 0; i < requiredWeaponGroups.Count; i++)
        {
            RewardWeaponGroup group = requiredWeaponGroups[i];

            if (usedWeaponGroups.Contains(group))
                continue;

            missingCount++;

            if (firstMissing == RewardWeaponGroup.None)
                firstMissing = group;
        }

        return missingCount >= remainingSlots
            ? firstMissing
            : RewardWeaponGroup.None;
    }

    private static RewardWeaponGroup GetWeaponGroup(RewardModifierEntry entry)
    {
        if (entry == null)
            return RewardWeaponGroup.None;

        return entry.Category switch
        {
            RewardModifierCategory.Damage
                or RewardModifierCategory.FireRate
                or RewardModifierCategory.CriticalChance
                or RewardModifierCategory.CriticalPower
                or RewardModifierCategory.Penetration
                or RewardModifierCategory.ParallelProjectiles
                or RewardModifierCategory.Salvo
                or RewardModifierCategory.ProjectileSpeed => RewardWeaponGroup.MainWeapon,

            RewardModifierCategory.AcaciaThornDamage
                or RewardModifierCategory.AcaciaThornFireRate
                or RewardModifierCategory.AcaciaThornSalvo
                or RewardModifierCategory.AcaciaThornProjectileSpeed
                or RewardModifierCategory.AcaciaThornCriticalChance
                or RewardModifierCategory.AcaciaThornCriticalPower => RewardWeaponGroup.AcaciaThorn,

            _ => RewardWeaponGroup.None
        };
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

    private enum RewardWeaponGroup
    {
        None,
        MainWeapon,
        AcaciaThorn
    }
}
