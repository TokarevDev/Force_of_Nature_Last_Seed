public sealed class RewardApplyService
{
    private readonly ProjectileWeapon _weapon;
    private readonly RewardRuntimeContext _context;

    public RewardApplyService(
        ProjectileWeapon weapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        _weapon = weapon;
        _context = new RewardRuntimeContext(weapon, acaciaThornWeapon);
    }

    public WeaponRuntimeState RuntimeState => _weapon != null ? _weapon.RuntimeState : null;
    public RewardRuntimeContext RuntimeContext => _context;

    public void Apply(RewardChoiceData choice)
    {
        if (choice == null || choice.Effect == null)
            return;

        if (_context == null)
            return;

        if (!choice.Effect.CanApply(_context))
            return;

        choice.Effect.Apply(_context);
        _weapon?.ForceRebuild();
    }
}
