public sealed class RewardApplyService
{
    private readonly ProjectileWeapon _weapon;

    public RewardApplyService(ProjectileWeapon weapon)
    {
        _weapon = weapon;
    }

    public WeaponRuntimeState RuntimeState => _weapon != null ? _weapon.RuntimeState : null;

    public void Apply(RewardChoiceData choice)
    {
        if (choice == null || choice.Effect == null)
            return;

        if (_weapon == null || _weapon.RuntimeState == null)
            return;

        if (!choice.Effect.CanApply(_weapon.RuntimeState))
            return;

        choice.Effect.Apply(_weapon.RuntimeState);
        _weapon.ForceRebuild();
    }
}
