using System;
using UnityEngine;

[Serializable]
public sealed class RewardModifierEntry
{
    [SerializeField] private RewardEffect _effect;
    [SerializeField] private string _title;
    [SerializeField][TextArea(2, 4)] private string _description;

    public RewardEffect Effect => _effect;
    public string Title => _title;
    public string Description => _description;
}