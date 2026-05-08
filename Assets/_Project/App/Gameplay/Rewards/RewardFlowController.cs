using System;
using System.Collections.Generic;

public sealed class RewardFlowController : IDisposable
{
    private const int AdRerollGuaranteedLegendarySlots = 3;

    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;
    private readonly PopupRoot _popupRoot;
    private readonly RewardedAdService _rewardedAdService;
    private readonly int _maxFreeRerollAttempts;
    private readonly int _maxAdRerollAttempts;
    private readonly int _maxTakeAllAttempts;

    private List<RewardChoiceData> _currentChoices;
    private CocoonRewardProfile _currentCocoonProfile;
    private RewardRarity _currentGuaranteeRarity;
    private int _freeRerollAttemptsLeft;
    private int _adRerollAttemptsLeft;
    private int _takeAllAttemptsLeft;

    private bool _isDisposed;
    private bool _isRewardedAdPending;

    public RewardFlowController(
        RewardRollService rollService,
        RewardApplyService applyService,
        RewardPopupView popup,
        PopupRoot popupRoot,
        RewardedAdService rewardedAdService,
        int maxFreeRerollAttempts,
        int maxAdRerollAttempts,
        int maxTakeAllAttempts)
    {
        _rollService = rollService;
        _applyService = applyService;
        _popup = popup;
        _popupRoot = popupRoot;
        _rewardedAdService = rewardedAdService;
        _maxFreeRerollAttempts = UnityEngine.Mathf.Max(0, maxFreeRerollAttempts);
        _maxAdRerollAttempts = UnityEngine.Mathf.Max(0, maxAdRerollAttempts);
        _maxTakeAllAttempts = UnityEngine.Mathf.Max(0, maxTakeAllAttempts);
        _freeRerollAttemptsLeft = _maxFreeRerollAttempts;
        _adRerollAttemptsLeft = _maxAdRerollAttempts;
        _takeAllAttemptsLeft = _maxTakeAllAttempts;

        if (_popup == null)
        {
            UnityEngine.Debug.LogWarning("RewardFlowController: reward popup is not assigned.");
            return;
        }

        _popup.Selected += HandleSelected;
        _popup.RerollRequested += HandleRerollRequested;
        _popup.AdRerollRequested += HandleAdRerollRequested;
        _popup.TakeAllRequested += HandleTakeAllRequested;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_popup != null)
        {
            _popup.Selected -= HandleSelected;
            _popup.RerollRequested -= HandleRerollRequested;
            _popup.AdRerollRequested -= HandleAdRerollRequested;
            _popup.TakeAllRequested -= HandleTakeAllRequested;
        }

        _isDisposed = true;
    }

    public bool Open(CocoonRewardProfile cocoonProfile = null)
    {
        _currentCocoonProfile = cocoonProfile;
        _isRewardedAdPending = false;

        if (!RollCurrentChoices())
            return false;

        return ShowCurrentChoices();
    }

    private void HandleSelected(RewardChoiceData choice)
    {
        if (_isRewardedAdPending)
            return;

        _applyService.Apply(choice);
    }

    private void HandleRerollRequested()
    {
        if (_freeRerollAttemptsLeft <= 0 || _isRewardedAdPending)
            return;

        if (!RollCurrentChoices())
            return;

        _freeRerollAttemptsLeft--;
        ShowCurrentChoices();
    }

    private void HandleAdRerollRequested()
    {
        if (_freeRerollAttemptsLeft > 0 || _adRerollAttemptsLeft <= 0)
            return;

        if (_isRewardedAdPending)
            return;

        _isRewardedAdPending = true;
        _popup?.SetAllButtonsInteractable(false);

        if (_rewardedAdService == null)
        {
            CompleteAdRerollReward(true);
            return;
        }

        _rewardedAdService.ShowRewardedAd(CompleteAdRerollReward);
    }

    private void HandleTakeAllRequested()
    {
        if (_currentChoices == null || _currentChoices.Count == 0)
            return;

        if (_takeAllAttemptsLeft <= 0 || _isRewardedAdPending)
            return;

        _isRewardedAdPending = true;
        _popup?.SetAllButtonsInteractable(false);

        if (_rewardedAdService == null)
        {
            CompleteTakeAllReward(true);
            return;
        }

        _rewardedAdService.ShowRewardedAd(CompleteTakeAllReward);
    }

    private void CompleteAdRerollReward(bool rewardGranted)
    {
        if (_isDisposed)
            return;

        _isRewardedAdPending = false;

        if (!rewardGranted)
        {
            ShowCurrentChoices();
            return;
        }

        _adRerollAttemptsLeft--;

        if (!RollCurrentChoices(
                RewardRarity.Legendary,
                AdRerollGuaranteedLegendarySlots))
        {
            return;
        }

        ShowCurrentChoices();
    }

    private void CompleteTakeAllReward(bool rewardGranted)
    {
        if (_isDisposed)
            return;

        _isRewardedAdPending = false;

        if (!rewardGranted)
        {
            ShowCurrentChoices();
            return;
        }

        _takeAllAttemptsLeft--;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            _applyService.Apply(_currentChoices[i]);
        }

        _popup?.Close();
    }

    private bool RollCurrentChoices(
        RewardRarity? forcedGuaranteeRarity = null,
        int forcedGuaranteeSlotCount = 1)
    {
        _currentGuaranteeRarity = forcedGuaranteeRarity
            ?? _rollService.RollGuaranteeRarity(
                _applyService.RuntimeContext,
                _currentCocoonProfile);

        _currentChoices = _rollService.Roll3(
            _applyService.RuntimeContext,
            _currentCocoonProfile,
            _currentGuaranteeRarity,
            forcedGuaranteeSlotCount);

        return _currentChoices != null && _currentChoices.Count > 0;
    }

    private bool ShowCurrentChoices()
    {
        if (_popup == null || _popupRoot == null)
        {
            UnityEngine.Debug.LogWarning("RewardFlowController: reward popup or popup root is not assigned.");
            return false;
        }

        bool isBound = _popup.Bind(
            _currentChoices,
            new RewardPopupState(
                _freeRerollAttemptsLeft,
                _adRerollAttemptsLeft,
                _takeAllAttemptsLeft,
                _currentGuaranteeRarity,
                _freeRerollAttemptsLeft > 0 && !_isRewardedAdPending,
                _freeRerollAttemptsLeft <= 0
                    && _adRerollAttemptsLeft > 0
                    && !_isRewardedAdPending,
                _takeAllAttemptsLeft > 0 && !_isRewardedAdPending));

        if (!isBound)
            return false;

        _popupRoot.Show(_popup);
        return true;
    }
}
