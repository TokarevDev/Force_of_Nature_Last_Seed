using System;
using System.Collections.Generic;

public sealed class RewardFlowController : IDisposable
{
    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;
    private readonly RewardedAdService _takeAllRewardedAdService;
    private readonly int _maxRerollAttempts;
    private readonly int _maxTakeAllAttempts;

    private List<RewardChoiceData> _currentChoices;
    private CocoonRewardProfile _currentCocoonProfile;
    private RewardRarity _currentGuaranteeRarity;
    private int _rerollAttemptsLeft;
    private int _takeAllAttemptsLeft;

    private bool _isDisposed;
    private bool _isTakeAllPending;

    public RewardFlowController(
        RewardRollService rollService,
        RewardApplyService applyService,
        RewardPopupView popup,
        RewardedAdService takeAllRewardedAdService,
        int maxRerollAttempts,
        int maxTakeAllAttempts)
    {
        _rollService = rollService;
        _applyService = applyService;
        _popup = popup;
        _takeAllRewardedAdService = takeAllRewardedAdService;
        _maxRerollAttempts = UnityEngine.Mathf.Max(0, maxRerollAttempts);
        _maxTakeAllAttempts = UnityEngine.Mathf.Max(0, maxTakeAllAttempts);

        _popup.OnSelected += OnSelected;
        _popup.OnRerollRequested += OnRerollRequested;
        _popup.OnTakeAllRequested += OnTakeAllRequested;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _popup.OnSelected -= OnSelected;
        _popup.OnRerollRequested -= OnRerollRequested;
        _popup.OnTakeAllRequested -= OnTakeAllRequested;
        _isDisposed = true;
    }

    public bool Open(CocoonRewardProfile cocoonProfile = null)
    {
        _currentCocoonProfile = cocoonProfile;
        _rerollAttemptsLeft = _maxRerollAttempts;
        _takeAllAttemptsLeft = _maxTakeAllAttempts;
        _isTakeAllPending = false;

        if (!RollCurrentChoices())
            return false;

        return ShowCurrentChoices();
    }

    public void OnSelected(RewardChoiceData choice)
    {
        if (_isTakeAllPending)
            return;

        _applyService.Apply(choice);
    }

    private void OnRerollRequested()
    {
        if (_rerollAttemptsLeft <= 0 || _isTakeAllPending)
            return;

        if (!RollCurrentChoices())
            return;

        _rerollAttemptsLeft--;
        ShowCurrentChoices();
    }

    private void OnTakeAllRequested()
    {
        if (_currentChoices == null || _currentChoices.Count == 0)
            return;

        if (_takeAllAttemptsLeft <= 0 || _isTakeAllPending)
            return;

        _isTakeAllPending = true;
        _popup.SetAllButtonsInteractable(false);

        if (_takeAllRewardedAdService == null)
        {
            CompleteTakeAllReward(true);
            return;
        }

        _takeAllRewardedAdService.ShowRewardedAd(CompleteTakeAllReward);
    }

    private void CompleteTakeAllReward(bool rewardGranted)
    {
        _isTakeAllPending = false;

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

        _popup.Close();
    }

    private bool RollCurrentChoices()
    {
        _currentGuaranteeRarity = _rollService.RollGuaranteeRarity(
            _applyService.RuntimeContext,
            _currentCocoonProfile);

        _currentChoices = _rollService.Roll3(
            _applyService.RuntimeContext,
            _currentCocoonProfile,
            _currentGuaranteeRarity);

        return _currentChoices != null && _currentChoices.Count > 0;
    }

    private bool ShowCurrentChoices()
    {
        return _popup.Show(
            _currentChoices,
            new RewardPopupState(
                _rerollAttemptsLeft,
                _takeAllAttemptsLeft,
                _currentGuaranteeRarity,
                _rerollAttemptsLeft > 0 && !_isTakeAllPending,
                _takeAllAttemptsLeft > 0 && !_isTakeAllPending));
    }
}
