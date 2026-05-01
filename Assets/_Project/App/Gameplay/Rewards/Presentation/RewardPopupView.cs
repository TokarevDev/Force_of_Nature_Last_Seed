using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays reward choices and handles selection.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardPopupView : MonoBehaviour
{
    private const string RerollButtonName = "ButtonUpdate";
    private const string TakeAllButtonName = "ButtonAds";

    [SerializeField] private GameObject _root;
    [SerializeField] private List<RewardButtonView> _buttons;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _takeAllButton;

    public Action<RewardChoiceData> OnSelected;
    public Action OnRerollRequested;
    public Action OnTakeAllRequested;

    private bool _inputLocked;

    private void Awake()
    {
        ResolveActionButtons();
    }

    private void OnEnable()
    {
        SubscribeActionButtons();
    }

    private void OnDisable()
    {
        UnsubscribeActionButtons();
        UnlockGameplayInput();
    }

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
        OnSelected?.Invoke(data);
        Close();
    }

    public void Close()
    {
        Time.timeScale = 1f;
        Hide();
    }

    private void OnRerollClicked()
    {
        OnRerollRequested?.Invoke();
    }

    private void OnTakeAllClicked()
    {
        OnTakeAllRequested?.Invoke();
    }

    private void Hide()
    {
        if (_root != null)
            _root.SetActive(false);

        UnlockGameplayInput();
    }

    private void SubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(OnRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.AddListener(OnTakeAllClicked);
    }

    private void UnsubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(OnRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.RemoveListener(OnTakeAllClicked);
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

    private void ResolveActionButtons()
    {
        if (_rerollButton == null)
            _rerollButton = FindChildButton(RerollButtonName);

        if (_takeAllButton == null)
            _takeAllButton = FindChildButton(TakeAllButtonName);
    }

    private Button FindChildButton(string childName)
    {
        Transform child = FindChildByName(transform, childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform match = FindChildByName(child, childName);

            if (match != null)
                return match;
        }

        return null;
    }
}
