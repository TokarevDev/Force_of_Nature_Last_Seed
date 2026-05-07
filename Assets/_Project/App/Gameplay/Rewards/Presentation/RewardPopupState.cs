public readonly struct RewardPopupState
{
    public RewardPopupState(
        int freeRerollAttemptsLeft,
        int adRerollAttemptsLeft,
        int takeAllAttemptsLeft,
        RewardRarity guaranteeRarity,
        bool canFreeReroll,
        bool canAdReroll,
        bool canTakeAll)
    {
        FreeRerollAttemptsLeft = freeRerollAttemptsLeft;
        AdRerollAttemptsLeft = adRerollAttemptsLeft;
        TakeAllAttemptsLeft = takeAllAttemptsLeft;
        GuaranteeRarity = guaranteeRarity;
        CanFreeReroll = canFreeReroll;
        CanAdReroll = canAdReroll;
        CanTakeAll = canTakeAll;
    }

    public int FreeRerollAttemptsLeft { get; }
    public int AdRerollAttemptsLeft { get; }
    public int TakeAllAttemptsLeft { get; }
    public RewardRarity GuaranteeRarity { get; }
    public bool CanFreeReroll { get; }
    public bool CanAdReroll { get; }
    public bool CanTakeAll { get; }
    public bool UseFreeRerollButton => FreeRerollAttemptsLeft > 0;
}
