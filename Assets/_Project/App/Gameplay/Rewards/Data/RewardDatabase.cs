using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Reward Database")]
public sealed class RewardDatabase : ScriptableObject
{
    [SerializeField] private List<RewardModifierEntry> _rewards;

    public IReadOnlyList<RewardModifierEntry> Rewards => _rewards;
}