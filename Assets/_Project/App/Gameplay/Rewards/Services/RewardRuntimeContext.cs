using System;

public sealed class RewardRuntimeContext
{
    private readonly WeaponRuntimeState _mainWeaponState;
    private readonly AcaciaThornRuntimeState _acaciaThornState;
    private readonly Func<int> _mainWeaponDamageProvider;

    public RewardRuntimeContext(
        ProjectileWeapon mainWeapon,
        AcaciaThornWeapon acaciaThornWeapon)
    {
        MainWeapon = mainWeapon;
        AcaciaThornWeapon = acaciaThornWeapon;
    }

    public RewardRuntimeContext(
        WeaponRuntimeState mainWeaponState,
        AcaciaThornRuntimeState acaciaThornState,
        Func<int> mainWeaponDamageProvider = null)
    {
        _mainWeaponState = mainWeaponState;
        _acaciaThornState = acaciaThornState;
        _mainWeaponDamageProvider = mainWeaponDamageProvider;
    }

    public ProjectileWeapon MainWeapon { get; }
    public AcaciaThornWeapon AcaciaThornWeapon { get; }
    public WeaponRuntimeState MainWeaponState =>
        _mainWeaponState ?? (MainWeapon != null ? MainWeapon.RuntimeState : null);

    public AcaciaThornRuntimeState AcaciaThornState =>
        _acaciaThornState ?? (AcaciaThornWeapon != null ? AcaciaThornWeapon.RuntimeState : null);

    public int MainWeaponDamageSnapshot
    {
        get
        {
            if (_mainWeaponDamageProvider != null)
                return _mainWeaponDamageProvider();

            return MainWeapon != null ? MainWeapon.CurrentProjectileDamage : 0;
        }
    }
}
