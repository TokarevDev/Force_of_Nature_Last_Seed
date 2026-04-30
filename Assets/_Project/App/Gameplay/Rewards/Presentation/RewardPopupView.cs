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

    private bool _inputLocked;

    public bool Show(List<RewardChoiceData> choices)
    {
        if (choices == null || choices.Count == 0)
        {
            Hide();
            return false;
        }

        Time.timeScale = 0f;
        LockGameplayInput();

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

        return true;
    }

    private void OnClicked(RewardChoiceData data)
    {
        Time.timeScale = 1f;

        OnSelected?.Invoke(data);
        Hide();
    }

    private void OnDisable()
    {
        UnlockGameplayInput();
    }

    private void Hide()
    {
        _root.SetActive(false);
        UnlockGameplayInput();
    }

    private void LockGameplayInput()
    {
        if (_inputLocked)
            return;

        GameplayInputBlocker.PushLock();
        _inputLocked = true;
    }

    private void UnlockGameplayInput()
    {
        if (!_inputLocked)
            return;

        GameplayInputBlocker.PopLock();
        _inputLocked = false;
    }
}
