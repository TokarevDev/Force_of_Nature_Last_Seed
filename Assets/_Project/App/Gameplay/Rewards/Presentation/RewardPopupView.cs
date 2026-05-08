using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays reward choices and handles selection.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardPopupView : PopupView
{
    private const string RerollButtonName = "ButtonUpdate";
    private const string AdRerollButtonName = "ADSRerollButton";
    private const string TakeAllButtonName = "ButtonAds";
    private const string AttemptsTextName = "AttemptsText (TMP)";
    private const string GuaranteeTextName = "GuaranteeText (TMP)";

    [SerializeField] private List<RewardButtonView> _buttons;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _adRerollButton;
    [SerializeField] private Button _takeAllButton;

    [Header("Action State Text")]
    [SerializeField] private TMP_Text _rerollAttemptsText;
    [SerializeField] private TMP_Text _adRerollAttemptsText;
    [SerializeField] private TMP_Text _takeAllAttemptsText;
    [SerializeField] private TMP_Text _guaranteeText;
    [SerializeField] private TMP_Text _adRerollGuaranteeText;
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";
    [SerializeField] private string _guaranteeFormat = "guarantee: {0}";
    [SerializeField] private string _adGuaranteeFormat = "guarantee: {0} x3";

    [Header("Text Colors")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);
    [SerializeField] private Color32 _commonRarityColor = new(95, 220, 130, 255);
    [SerializeField] private Color32 _rareRarityColor = new(80, 180, 255, 255);
    [SerializeField] private Color32 _legendaryRarityColor = new(255, 155, 70, 255);

    public event Action<RewardChoiceData> Selected;
    public event Action RerollRequested;
    public event Action AdRerollRequested;
    public event Action TakeAllRequested;

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
    }

    public bool Bind(List<RewardChoiceData> choices, RewardPopupState state)
    {
        if (choices == null || choices.Count == 0 || _buttons == null || _buttons.Count == 0)
        {
            Debug.LogWarning("RewardPopupView: reward choices or buttons are not assigned.", this);
            RequestClose();
            return false;
        }

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button == null)
                continue;

            if (i >= choices.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);
            button.Bind(choices[i], OnClicked);
        }

        ApplyState(state);
        return true;
    }

    private void OnClicked(RewardChoiceData data)
    {
        Selected?.Invoke(data);
        RequestClose();
    }

    public void Close()
    {
        RequestClose();
    }

    private void OnRerollClicked()
    {
        RerollRequested?.Invoke();
    }

    private void OnTakeAllClicked()
    {
        TakeAllRequested?.Invoke();
    }

    private void OnAdRerollClicked()
    {
        AdRerollRequested?.Invoke();
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

        if (_adRerollButton != null)
            _adRerollButton.interactable = interactable;

        if (_takeAllButton != null)
            _takeAllButton.interactable = interactable;
    }

    private void SubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(OnRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.AddListener(OnAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.AddListener(OnTakeAllClicked);
    }

    private void UnsubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(OnRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.RemoveListener(OnAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.RemoveListener(OnTakeAllClicked);
    }

    private void ResolveActionButtons()
    {
        if (_rerollButton == null)
            _rerollButton = FindChildButton(RerollButtonName);

        if (_adRerollButton == null)
            _adRerollButton = FindChildButton(AdRerollButtonName);

        if (_takeAllButton == null)
            _takeAllButton = FindChildButton(TakeAllButtonName);
    }

    private void ResolveStateTexts()
    {
        if (_rerollAttemptsText == null)
            _rerollAttemptsText = FindChildText(_rerollButton, AttemptsTextName);

        if (_adRerollAttemptsText == null)
            _adRerollAttemptsText = FindChildText(_adRerollButton, AttemptsTextName);

        if (_takeAllAttemptsText == null)
            _takeAllAttemptsText = FindChildText(_takeAllButton, AttemptsTextName);

        if (_guaranteeText == null)
            _guaranteeText = FindChildText(_rerollButton, GuaranteeTextName);

        if (_adRerollGuaranteeText == null)
            _adRerollGuaranteeText = FindChildText(_adRerollButton, GuaranteeTextName);
    }

    private void ApplyState(RewardPopupState state)
    {
        ApplyRerollButtonMode(state);
        ApplyAttemptsText(_rerollAttemptsText, state.FreeRerollAttemptsLeft);
        ApplyAttemptsText(_adRerollAttemptsText, state.AdRerollAttemptsLeft);
        ApplyAttemptsText(_takeAllAttemptsText, state.TakeAllAttemptsLeft);

        ApplyGuaranteeText(_guaranteeText, state.GuaranteeRarity);
        ApplyAdGuaranteeText(_adRerollGuaranteeText, RewardRarity.Legendary);

        if (_rerollButton != null)
            _rerollButton.interactable = state.CanFreeReroll;

        if (_adRerollButton != null)
            _adRerollButton.interactable = state.CanAdReroll;

        if (_takeAllButton != null)
            _takeAllButton.interactable = state.CanTakeAll;
    }

    private void ApplyRerollButtonMode(RewardPopupState state)
    {
        bool showFreeReroll = state.UseFreeRerollButton;

        if (_rerollButton != null)
            _rerollButton.gameObject.SetActive(showFreeReroll);

        if (_adRerollButton != null)
            _adRerollButton.gameObject.SetActive(!showFreeReroll);
    }

    private void ApplyGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
            _guaranteeFormat,
            rarity,
            _commonRarityColor,
            _rareRarityColor,
            _legendaryRarityColor);
    }

    private void ApplyAdGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
            _adGuaranteeFormat,
            rarity,
            _commonRarityColor,
            _rareRarityColor,
            _legendaryRarityColor,
            _numberColor);
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
