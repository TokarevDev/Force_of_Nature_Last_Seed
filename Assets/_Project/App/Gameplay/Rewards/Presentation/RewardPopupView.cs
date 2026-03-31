using System;
using System.Collections.Generic;
using UnityEngine;

public class RewardPopupView : MonoBehaviour
{
    public event Action<RewardChoiceData> OnSelected;

    public void Show(List<RewardChoiceData> choices)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            Debug.Log($"Choise {i}: {choices[i].Modifier.name}");
        }
    }

    public void Select(int index, List<RewardChoiceData> choices)
    {
        OnSelected?.Invoke(choices[index]);
    }
}