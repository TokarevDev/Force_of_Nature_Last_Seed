public sealed class RewardRuntimeContext
{
    public RewardRuntimeContext(
        ProjectileWeapon mainWeapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        MainWeapon = mainWeapon;
        AcaciaThornWeapon = acaciaThornWeapon;
    }

    public ProjectileWeapon MainWeapon { get; }
    public AcaciaThornWeapon AcaciaThornWeapon { get; }
    public WeaponRuntimeState MainWeaponState =>
        MainWeapon != null ? MainWeapon.RuntimeState : null;
}
