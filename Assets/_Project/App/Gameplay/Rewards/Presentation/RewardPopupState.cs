public readonly struct RewardPopupState
{
    public RewardPopupState(
        int rerollAttemptsLeft,
        int takeAllAttemptsLeft,
        RewardRarity guaranteeRarity,
        bool canReroll,
        bool canTakeAll)
    {
        RerollAttemptsLeft = rerollAttemptsLeft;
        TakeAllAttemptsLeft = takeAllAttemptsLeft;
        GuaranteeRarity = guaranteeRarity;
        CanReroll = canReroll;
        CanTakeAll = canTakeAll;
    }

    public int RerollAttemptsLeft { get; }
    public int TakeAllAttemptsLeft { get; }
    public RewardRarity GuaranteeRarity { get; }
    public bool CanReroll { get; }
    public bool CanTakeAll { get; }
}
