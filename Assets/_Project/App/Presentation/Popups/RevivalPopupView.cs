using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RevivalPopupView : PopupView
{
    [SerializeField] private Button _reviveButton;
    [SerializeField] private Button _giveUpButton;
    [SerializeField] private Slider _remainingSlider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _percentText;
    [SerializeField] private TMP_Text _attemptsText;
    [SerializeField] private string _progressFormat = "Only {0}% of the level remains.";
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";

    public event Action ReviveRequested;
    public event Action GiveUpRequested;

    private bool _canRevive;

    private void OnEnable()
    {
        if (_reviveButton != null)
            _reviveButton.onClick.AddListener(HandleReviveClicked);

        if (_giveUpButton != null)
            _giveUpButton.onClick.AddListener(HandleGiveUpClicked);
    }

    private void OnDisable()
    {
        if (_reviveButton != null)
            _reviveButton.onClick.RemoveListener(HandleReviveClicked);

        if (_giveUpButton != null)
            _giveUpButton.onClick.RemoveListener(HandleGiveUpClicked);
    }

    public void Bind(
        int attemptsLeft,
        float currentProgressNormalized,
        float remainingLevelNormalized,
        bool canRevive)
    {
        int currentPercent = Mathf.RoundToInt(Mathf.Clamp01(currentProgressNormalized) * 100f);
        int remainingPercent = Mathf.RoundToInt(Mathf.Clamp01(remainingLevelNormalized) * 100f);
        _canRevive = canRevive && attemptsLeft > 0;

        if (_remainingSlider != null)
            _remainingSlider.SetValueWithoutNotify(currentPercent / 100f);

        if (_progressText != null)
            _progressText.SetText(_progressFormat, remainingPercent);

        if (_percentText != null)
            _percentText.SetText("{0}%", currentPercent);

        if (_attemptsText != null)
            _attemptsText.SetText(_attemptsFormat, Mathf.Max(0, attemptsLeft));

        SetWaitingForAd(false);
    }

    public void SetWaitingForAd(bool isWaiting)
    {
        if (_reviveButton != null)
            _reviveButton.interactable = !isWaiting && _canRevive;

        if (_giveUpButton != null)
            _giveUpButton.interactable = !isWaiting;
    }

    private void HandleReviveClicked()
    {
        if (!_canRevive)
            return;

        ReviveRequested?.Invoke();
    }

    private void HandleGiveUpClicked()
    {
        GiveUpRequested?.Invoke();
    }
}
