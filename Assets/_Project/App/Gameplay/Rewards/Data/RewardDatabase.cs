using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Reward Database")]
public sealed class RewardDatabase : ScriptableObject
{
    [SerializeField] private List<CocoonRewardProfile> _cocoonProfiles = new();
    [SerializeField] private List<RewardModifierEntry> _rewards;

    public IReadOnlyList<CocoonRewardProfile> CocoonProfiles =>
        HasSpawnableCocoonProfile(_cocoonProfiles)
            ? _cocoonProfiles
            : CocoonRewardProfile.Defaults;

    public IReadOnlyList<RewardModifierEntry> Rewards => _rewards;

    private void OnEnable()
    {
        EnsureDefaultCocoonProfiles();
    }

    private void Reset()
    {
        EnsureDefaultCocoonProfiles();
    }

    private void OnValidate()
    {
        EnsureDefaultCocoonProfiles();
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
            1f,
            0f,
            false,
            0f,
            false,
            0f,
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common)),

        new(
            "Green",
            new Color32(95, 220, 130, 255),
            0.8f,
            0f,
            false,
            0f,
            false,
            0f,
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Rare)),

        new(
            "Blue",
            new Color32(125, 220, 255, 255),
            0.6f,
            0f,
            false,
            0f,
            false,
            0f,
            new RewardRaritySlot(RewardRarity.Rare),
            new RewardRaritySlot(RewardRarity.Rare),
            new RewardRaritySlot(RewardRarity.Common)),

        new(
            "Orange",
            new Color32(255, 150, 60, 255),
            0.3f,
            0.5f,
            true,
            0.03f,
            true,
            0.05f,
            new RewardRaritySlot(RewardRarity.Legendary),
            new RewardRaritySlot(RewardRarity.Rare),
            new RewardRaritySlot(RewardRarity.Rare))
    };

    [SerializeField] private string _displayName = "White";
    [SerializeField] private Color _visualColor = Color.white;
    [SerializeField][Min(0f)] private float _spawnWeight = 1f;
    [SerializeField][Range(0f, 1f)] private float _minDestroyedProgressToSpawn;
    [SerializeField] private bool _useFixedSpawnChance;
    [SerializeField][Range(0f, 1f)] private float _fixedSpawnChance;
    [SerializeField] private bool _guaranteesLegendaryReward;
    [SerializeField][Range(0f, 1f)] private float _secondaryLegendaryChance;
    [SerializeField] private List<RewardRaritySlot> _raritySlots = new();

    public static IReadOnlyList<CocoonRewardProfile> Defaults => DefaultProfileSet;
    public static CocoonRewardProfile Default => DefaultProfileSet[0];

    public string DisplayName => _displayName;
    public Color VisualColor => _visualColor;
    public float SpawnWeight => Mathf.Max(0f, _spawnWeight);
    public float MinDestroyedProgressToSpawn => Mathf.Clamp01(_minDestroyedProgressToSpawn);
    public bool UseFixedSpawnChance => _useFixedSpawnChance;
    public float FixedSpawnChance => Mathf.Clamp01(_fixedSpawnChance);
    public bool GuaranteesLegendaryReward => _guaranteesLegendaryReward;
    public float SecondaryLegendaryChance => Mathf.Clamp01(_secondaryLegendaryChance);
    public IReadOnlyList<RewardRaritySlot> RaritySlots => _raritySlots;

    public CocoonRewardProfile()
    {
    }

    private CocoonRewardProfile(
        string displayName,
        Color visualColor,
        float spawnWeight,
        float minDestroyedProgressToSpawn,
        bool useFixedSpawnChance,
        float fixedSpawnChance,
        bool guaranteesLegendaryReward,
        float secondaryLegendaryChance,
        params RewardRaritySlot[] raritySlots)
    {
        _displayName = displayName;
        _visualColor = visualColor;
        _spawnWeight = Mathf.Max(0f, spawnWeight);
        _minDestroyedProgressToSpawn = Mathf.Clamp01(minDestroyedProgressToSpawn);
        _useFixedSpawnChance = useFixedSpawnChance;
        _fixedSpawnChance = Mathf.Clamp01(fixedSpawnChance);
        _guaranteesLegendaryReward = guaranteesLegendaryReward;
        _secondaryLegendaryChance = Mathf.Clamp01(secondaryLegendaryChance);
        _raritySlots = new List<RewardRaritySlot>();

        if (raritySlots == null)
            return;

        for (int i = 0; i < raritySlots.Length; i++)
        {
            RewardRaritySlot slot = raritySlots[i];

            if (slot == null)
                continue;

            _raritySlots.Add(slot.Clone());
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
        int slotCount = _raritySlots != null ? _raritySlots.Count : 0;
        RewardRaritySlot[] slots = new RewardRaritySlot[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            RewardRaritySlot slot = _raritySlots[i];
            slots[i] = slot != null
                ? slot.Clone()
                : null;
        }

        return new CocoonRewardProfile(
            _displayName,
            _visualColor,
            _spawnWeight,
            _minDestroyedProgressToSpawn,
            _useFixedSpawnChance,
            _fixedSpawnChance,
            _guaranteesLegendaryReward,
            _secondaryLegendaryChance,
            slots);
    }
}
