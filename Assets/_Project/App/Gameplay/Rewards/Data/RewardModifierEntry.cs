using System;
using UnityEngine;

[Serializable]
public sealed class RewardModifierEntry
{
    [SerializeField] private ShotModifierData _modifier;
    [SerializeField] private string _title;
    [SerializeField][TextArea(2, 4)] private string _description;

    public ShotModifierData Modifier => _modifier;
    public string Title => _title;
    public string Description => _description;
}