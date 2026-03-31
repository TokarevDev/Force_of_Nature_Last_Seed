using System.Collections.Generic;

public sealed class RewardFlowController
{
    private readonly RewardRollService _rollService;
    private readonly RewardApplyService _applyService;
    private readonly RewardPopupView _popup;

    private List<RewardChoiceData> _currentChoices;

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

    public void Open()
    {
        _currentChoices = _rollService.Roll3();
        _popup.Show(_currentChoices);
    }

    public void OnSelected(RewardChoiceData choice)
    {
        _applyService.Apply(choice);
    }
}