public sealed class RewardApplyService
{
    private readonly ProjectileWeapon _weapon;

    public RewardApplyService(ProjectileWeapon weapon)
    {
        _weapon = weapon;
    }

    public void Apply(RewardChoiceData choice)
    {
        if (choice == null || choice.Effect == null)
            return;

        choice.Effect.Apply(_weapon.RuntimeState);
        _weapon.ForceRebuild();
    }
}