public sealed class RewardApplyService
{
    private readonly ProjectileWeapon _weapon;

    public RewardApplyService(ProjectileWeapon weapon)
    {
        _weapon = weapon;
    }

    public void Apply(RewardChoiceData choice)
    {
        if (choice == null || choice.Modifier == null)
            return;

        _weapon.RuntimeState.ShotModifiers.Add(choice.Modifier);
        _weapon.ForceRebuild();
    }
}