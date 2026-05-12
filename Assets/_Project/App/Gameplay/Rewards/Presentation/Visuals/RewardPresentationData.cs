public readonly struct RewardPresentationData
{
    public RewardPresentationData(
        RewardIconProfile iconProfile,
        RewardPresentationKind kind)
    {
        IconProfile = iconProfile;
        Kind = kind;
    }

    public RewardIconProfile IconProfile { get; }
    public RewardPresentationKind Kind { get; }
}
