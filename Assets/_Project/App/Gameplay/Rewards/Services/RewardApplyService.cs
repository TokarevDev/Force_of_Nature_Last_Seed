public sealed class RewardApplyService
{
    private readonly WeaponConfig _weaponConfig;

    public RewardApplyService(WeaponConfig weaponConfig)
    {
        _weaponConfig = weaponConfig;
    }

    public void Apply(RewardChoiceData choice)
    {
        _weaponConfig.AddModifier(choice.Modifier);
    }
}