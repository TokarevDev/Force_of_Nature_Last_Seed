using UnityEngine;

[CreateAssetMenu(menuName = "Game/Reward/Reward Definition")]
public sealed class RewardDefinition : ScriptableObject
{

    [Header("UI")]
    [SerializeField] private string _title;

    [SerializeField] private string _description;

    [Header("Gameplay")]
    [SerializeField] private RewardEffect _effect;

    public string Title => _title;
    public string Description => _description;
    public RewardEffect Effect => _effect;
}