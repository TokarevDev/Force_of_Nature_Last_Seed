using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Reward Database")]
public sealed class RewardDatabase : ScriptableObject
{
    [SerializeField] private List<ShotModifierData> _modifiers;

    public IReadOnlyList<ShotModifierData> Modifiers => _modifiers;
}