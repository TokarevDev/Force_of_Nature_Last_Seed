using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardPopupView : MonoBehaviour
{
    public Action<RewardChoiceData> OnSelected;

    public void Show(List<RewardChoiceData> choices)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("RewardPopup: no choices");
            return;
        }

        Debug.Log("=== REWARD CHOICES ===");

        for (int i = 0; i < choices.Count; i++)
        {
            var mod = choices[i].Modifier;
            Debug.Log($"[{i}] {mod.name}");
        }

        Debug.Log("Auto-selecting first reward");

        OnSelected?.Invoke(choices[0]);
    }
}