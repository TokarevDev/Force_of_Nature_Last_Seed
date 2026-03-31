using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays reward choices and handles selection.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardPopupView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private List<RewardButtonView> _buttons;

    public Action<RewardChoiceData> OnSelected;

    public void Show(List<RewardChoiceData> choices)
    {
        Time.timeScale = 0f;

        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("No reward choices");
            return;
        }

        _root.SetActive(true);

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (i >= choices.Count)
            {
                _buttons[i].gameObject.SetActive(false);
                continue;
            }

            _buttons[i].gameObject.SetActive(true);
            _buttons[i].Bind(choices[i], OnClicked);
        }
    }

    private void OnClicked(RewardChoiceData data)
    {
        Time.timeScale = 1f;

        OnSelected?.Invoke(data);
        Hide();
    }

    private void Hide()
    {
        _root.SetActive(false);
    }
}