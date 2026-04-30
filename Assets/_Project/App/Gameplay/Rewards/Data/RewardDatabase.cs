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
            0.62f,
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common)),

        new(
            "Green",
            new Color32(95, 220, 130, 255),
            0.28f,
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Common),
            new RewardRaritySlot(RewardRarity.Rare)),

        new(
            "Blue",
            new Color32(125, 220, 255, 255),
            0.08f,
            new RewardRaritySlot(RewardRarity.Rare),
            new RewardRaritySlot(RewardRarity.Rare),
            new RewardRaritySlot(RewardRarity.Common, RewardRarity.Legendary, 0.05f)),

        new(
            "Orange",
            new Color32(255, 150, 60, 255),
            0.02f,
            new RewardRaritySlot(RewardRarity.Legendary),
            new RewardRaritySlot(RewardRarity.Legendary),
            new RewardRaritySlot(RewardRarity.Rare, RewardRarity.Legendary, 0.3f))
    };

    [SerializeField] private string _displayName = "White";
    [SerializeField] private Color _visualColor = Color.white;
    [SerializeField][Min(0f)] private float _spawnWeight = 1f;
    [SerializeField] private List<RewardRaritySlot> _raritySlots = new();

    public static IReadOnlyList<CocoonRewardProfile> Defaults => DefaultProfileSet;
    public static CocoonRewardProfile Default => DefaultProfileSet[0];

    public string DisplayName => _displayName;
    public Color VisualColor => _visualColor;
    public float SpawnWeight => Mathf.Max(0f, _spawnWeight);
    public IReadOnlyList<RewardRaritySlot> RaritySlots => _raritySlots;

    public CocoonRewardProfile()
    {
    }

    private CocoonRewardProfile(
        string displayName,
        Color visualColor,
        float spawnWeight,
        params RewardRaritySlot[] raritySlots)
    {
        _displayName = displayName;
        _visualColor = visualColor;
        _spawnWeight = Mathf.Max(0f, spawnWeight);
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
            slots);
    }
}
