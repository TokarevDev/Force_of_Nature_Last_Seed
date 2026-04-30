using System;
using System.Collections.Generic;

public sealed class RewardFlowController : IDisposable
{
    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;

    private List<RewardChoiceData> _currentChoices;

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
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _popup.OnSelected -= OnSelected;
        _isDisposed = true;
    }

    public bool Open(CocoonRewardProfile cocoonProfile = null)
    {
        _currentChoices = _rollService.Roll3(
            _applyService.RuntimeContext,
            cocoonProfile);

        if (_currentChoices == null || _currentChoices.Count == 0)
            return false;

        return _popup.Show(_currentChoices);
    }

    public void OnSelected(RewardChoiceData choice)
    {
        _applyService.Apply(choice);
    }
}
