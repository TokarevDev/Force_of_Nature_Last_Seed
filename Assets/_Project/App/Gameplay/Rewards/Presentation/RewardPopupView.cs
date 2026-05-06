using System;
using System.Collections.Generic;
using TMPro;
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
    private const string AttemptsTextName = "AttemptsText (TMP)";
    private const string GuaranteeTextName = "GuaranteeText (TMP)";

    [SerializeField] private GameObject _root;
    [SerializeField] private List<RewardButtonView> _buttons;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _takeAllButton;

    [Header("Action State Text")]
    [SerializeField] private TMP_Text _rerollAttemptsText;
    [SerializeField] private TMP_Text _takeAllAttemptsText;
    [SerializeField] private TMP_Text _guaranteeText;
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";
    [SerializeField] private string _guaranteeFormat = "guarantee: {0}";

    [Header("Text Colors")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);
    [SerializeField] private Color32 _commonRarityColor = new(95, 220, 130, 255);
    [SerializeField] private Color32 _rareRarityColor = new(80, 180, 255, 255);
    [SerializeField] private Color32 _legendaryRarityColor = new(255, 155, 70, 255);

    public Action<RewardChoiceData> OnSelected;
    public Action OnRerollRequested;
    public Action OnTakeAllRequested;

    private bool _inputLocked;

    private void Awake()
    {
        ResolveActionButtons();
        ResolveStateTexts();
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

    public bool Show(List<RewardChoiceData> choices, RewardPopupState state)
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

        ApplyState(state);
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

    public void SetAllButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].SetInteractable(interactable);
        }

        if (_rerollButton != null)
            _rerollButton.interactable = interactable;

        if (_takeAllButton != null)
            _takeAllButton.interactable = interactable;
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

    private void ResolveStateTexts()
    {
        if (_rerollAttemptsText == null)
            _rerollAttemptsText = FindChildText(_rerollButton, AttemptsTextName);

        if (_takeAllAttemptsText == null)
            _takeAllAttemptsText = FindChildText(_takeAllButton, AttemptsTextName);

        if (_guaranteeText == null)
            _guaranteeText = FindChildText(_rerollButton, GuaranteeTextName);
    }

    private void ApplyState(RewardPopupState state)
    {
        ApplyAttemptsText(_rerollAttemptsText, state.RerollAttemptsLeft);
        ApplyAttemptsText(_takeAllAttemptsText, state.TakeAllAttemptsLeft);

        if (_guaranteeText != null)
        {
            _guaranteeText.text = RewardTextFormatter.FormatRarityLine(
                _guaranteeFormat,
                state.GuaranteeRarity,
                _commonRarityColor,
                _rareRarityColor,
                _legendaryRarityColor);
        }

        if (_rerollButton != null)
            _rerollButton.interactable = state.CanReroll;

        if (_takeAllButton != null)
            _takeAllButton.interactable = state.CanTakeAll;
    }

    private void ApplyAttemptsText(TMP_Text text, int attemptsLeft)
    {
        if (text == null)
            return;

        string value = string.Format(_attemptsFormat, Mathf.Max(0, attemptsLeft));
        text.text = RewardTextFormatter.HighlightAttempts(value, _numberColor);
    }

    private Button FindChildButton(string childName)
    {
        Transform child = FindChildByName(transform, childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private TMP_Text FindChildText(Button button, string childName)
    {
        if (button == null)
            return null;

        Transform child = FindChildByName(button.transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
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
