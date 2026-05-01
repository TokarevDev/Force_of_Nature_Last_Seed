using System;
using System.Collections.Generic;

public sealed class RewardFlowController : IDisposable
{
    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;

    private List<RewardChoiceData> _currentChoices;
    private CocoonRewardProfile _currentCocoonProfile;

    private bool _isDisposed;

    public RewardFlowController(
        RewardRollService rollService,
        RewardApplyService applyService,
        RewardPopupView popup)
    {
        _rollService = rollService;
        _applyService = applyService;
        _popup = popup;

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

        if (!RollCurrentChoices())
            return false;

        return _popup.Show(_currentChoices);
    }

    public void OnSelected(RewardChoiceData choice)
    {
        _applyService.Apply(choice);
    }

    private void OnRerollRequested()
    {
        if (!RollCurrentChoices())
            return;

        _popup.Show(_currentChoices);
    }

    private void OnTakeAllRequested()
    {
        if (_currentChoices == null || _currentChoices.Count == 0)
            return;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            _applyService.Apply(_currentChoices[i]);
        }

        _popup.Close();
    }

    private bool RollCurrentChoices()
    {
        _currentChoices = _rollService.Roll3(
            _applyService.RuntimeContext,
            _currentCocoonProfile);

        return _currentChoices != null && _currentChoices.Count > 0;
    }
}
