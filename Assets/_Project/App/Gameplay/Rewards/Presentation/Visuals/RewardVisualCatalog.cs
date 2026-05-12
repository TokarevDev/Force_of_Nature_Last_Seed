using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/UI/Visual Catalog")]
public sealed class RewardVisualCatalog : ScriptableObject
{
    [SerializeField] private RewardIconProfile _fallbackIconProfile;
    [SerializeField] private List<RewardCategoryVisualRule> _rules = new();

    public RewardPresentationData GetPresentation(RewardModifierCategory category)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            RewardCategoryVisualRule rule = _rules[i];

            if (rule == null || rule.Category != category)
                continue;

            RewardIconProfile iconProfile = rule.IconProfile != null
                ? rule.IconProfile
                : _fallbackIconProfile;

            return new RewardPresentationData(iconProfile, rule.Kind);
        }

        return new RewardPresentationData(
            _fallbackIconProfile,
            RewardPresentationKind.StatUpgrade);
    }
}

[Serializable]
public sealed class RewardCategoryVisualRule
{
    [SerializeField] private RewardModifierCategory _category;
    [SerializeField] private RewardIconProfile _iconProfile;
    [SerializeField] private RewardPresentationKind _kind;

    public RewardModifierCategory Category => _category;
    public RewardIconProfile IconProfile => _iconProfile;
    public RewardPresentationKind Kind => _kind;
}
