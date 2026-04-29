using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Reward Database")]
public sealed class RewardDatabase : ScriptableObject
{
    [SerializeField] private List<RewardRarityWeight> _rarityWeights = new();
    [SerializeField] private List<CocoonRewardProfile> _cocoonProfiles = new();
    [SerializeField] private List<RewardModifierEntry> _rewards;

    public IReadOnlyList<RewardRarityWeight> RarityWeights => _rarityWeights;
    public IReadOnlyList<CocoonRewardProfile> CocoonProfiles =>
        HasSpawnableCocoonProfile(_cocoonProfiles)
            ? _cocoonProfiles
            : CocoonRewardProfile.Defaults;

    public IReadOnlyList<RewardModifierEntry> Rewards => _rewards;

    private void OnEnable()
    {
        EnsureDefaultRarityWeights();
        EnsureDefaultCocoonProfiles();
    }

    private void Reset()
    {
        EnsureDefaultRarityWeights();
        EnsureDefaultCocoonProfiles();
    }

    private void OnValidate()
    {
        EnsureDefaultRarityWeights();
        EnsureDefaultCocoonProfiles();
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

    private void EnsureDefaultCocoonProfiles()
    {
        if (_cocoonProfiles == null)
            _cocoonProfiles = new List<CocoonRewardProfile>();

        if (_cocoonProfiles.Count > 0)
            return;

        CocoonRewardProfile.AddDefaultsTo(_cocoonProfiles);
    }

    private static bool HasSpawnableCocoonProfile(IReadOnlyList<CocoonRewardProfile> profiles)
    {
        if (profiles == null)
            return false;

        for (int i = 0; i < profiles.Count; i++)
        {
            CocoonRewardProfile profile = profiles[i];

            if (profile != null && profile.SpawnWeight > 0f)
                return true;
        }

        return false;
    }
}

[System.Serializable]
public sealed class CocoonRewardProfile
{
    private static readonly CocoonRewardProfile[] DefaultProfileSet =
    {
        new(
            "White",
            Color.white,
            0.62f,
            new RewardRarityWeight(RewardRarity.Common, 1f),
            new RewardRarityWeight(RewardRarity.Rare, 0.1f),
            new RewardRarityWeight(RewardRarity.Legendary, 0f)),

        new(
            "Blue",
            new Color32(125, 220, 255, 255),
            0.28f,
            new RewardRarityWeight(RewardRarity.Common, 1f),
            new RewardRarityWeight(RewardRarity.Rare, 0.25f),
            new RewardRarityWeight(RewardRarity.Legendary, 0.01f)),

        new(
            "Purple",
            new Color32(210, 120, 255, 255),
            0.08f,
            new RewardRarityWeight(RewardRarity.Common, 0.8f),
            new RewardRarityWeight(RewardRarity.Rare, 0.45f),
            new RewardRarityWeight(RewardRarity.Legendary, 0.05f)),

        new(
            "Gold",
            new Color32(255, 211, 90, 255),
            0.02f,
            new RewardRarityWeight(RewardRarity.Common, 0.25f),
            new RewardRarityWeight(RewardRarity.Rare, 0.75f),
            new RewardRarityWeight(RewardRarity.Legendary, 0.15f))
    };

    [SerializeField] private string _displayName = "White";
    [SerializeField] private Color _visualColor = Color.white;
    [SerializeField][Min(0f)] private float _spawnWeight = 1f;
    [SerializeField] private List<RewardRarityWeight> _rarityWeights = new();

    public static IReadOnlyList<CocoonRewardProfile> Defaults => DefaultProfileSet;
    public static CocoonRewardProfile Default => DefaultProfileSet[0];

    public string DisplayName => _displayName;
    public Color VisualColor => _visualColor;
    public float SpawnWeight => Mathf.Max(0f, _spawnWeight);
    public IReadOnlyList<RewardRarityWeight> RarityWeights => _rarityWeights;

    public CocoonRewardProfile()
    {
    }

    private CocoonRewardProfile(
        string displayName,
        Color visualColor,
        float spawnWeight,
        params RewardRarityWeight[] rarityWeights)
    {
        _displayName = displayName;
        _visualColor = visualColor;
        _spawnWeight = Mathf.Max(0f, spawnWeight);
        _rarityWeights = new List<RewardRarityWeight>();

        if (rarityWeights == null)
            return;

        for (int i = 0; i < rarityWeights.Length; i++)
        {
            RewardRarityWeight weight = rarityWeights[i];

            if (weight == null)
                continue;

            _rarityWeights.Add(new RewardRarityWeight(weight.Rarity, weight.Weight));
        }
    }

    public static void AddDefaultsTo(List<CocoonRewardProfile> target)
    {
        if (target == null)
            return;

        for (int i = 0; i < DefaultProfileSet.Length; i++)
        {
            target.Add(DefaultProfileSet[i].Clone());
        }
    }

    private CocoonRewardProfile Clone()
    {
        int weightCount = _rarityWeights != null ? _rarityWeights.Count : 0;
        RewardRarityWeight[] weights = new RewardRarityWeight[weightCount];

        for (int i = 0; i < weightCount; i++)
        {
            RewardRarityWeight weight = _rarityWeights[i];
            weights[i] = weight != null
                ? new RewardRarityWeight(weight.Rarity, weight.Weight)
                : null;
        }

        return new CocoonRewardProfile(
            _displayName,
            _visualColor,
            _spawnWeight,
            weights);
    }
}
