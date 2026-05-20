using System;
using UnityEngine;

public readonly struct RewardRollContext
{
    public readonly float HeadPathProgressNormalized;
    public readonly float WormDestructionProgressNormalized;
    public readonly bool HasRevivedThisRun;

    public RewardRollContext(
        float headPathProgressNormalized,
        float wormDestructionProgressNormalized,
        bool hasRevivedThisRun)
    {
        HeadPathProgressNormalized = Mathf.Clamp01(headPathProgressNormalized);
        WormDestructionProgressNormalized = Mathf.Clamp01(wormDestructionProgressNormalized);
        HasRevivedThisRun = hasRevivedThisRun;
    }
}

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
        MainWeaponConfig = mainWeapon != null ? mainWeapon.Config : null;
        AcaciaThornConfig = acaciaThornWeapon != null ? acaciaThornWeapon.Config : null;
    }

    public RewardRuntimeContext(
        WeaponRuntimeState mainWeaponState,
        AcaciaThornRuntimeState acaciaThornState,
        Func<int> mainWeaponDamageProvider = null,
        WeaponConfig mainWeaponConfig = null,
        AcaciaThornWeaponConfig acaciaThornConfig = null)
    {
        _mainWeaponState = mainWeaponState;
        _acaciaThornState = acaciaThornState;
        _mainWeaponDamageProvider = mainWeaponDamageProvider;
        MainWeaponConfig = mainWeaponConfig;
        AcaciaThornConfig = acaciaThornConfig;
    }

    public ProjectileWeapon MainWeapon { get; }
    public AcaciaThornWeapon AcaciaThornWeapon { get; }
    public WeaponConfig MainWeaponConfig { get; }
    public AcaciaThornWeaponConfig AcaciaThornConfig { get; }
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
