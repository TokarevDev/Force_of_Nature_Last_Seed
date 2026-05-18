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

public static class RewardAdAssistRules
{
    public const float PaidRerollHeadProgressThreshold = 0.8f;
    public const float TakeAllHeadProgressThreshold = 0.95f;
    public const float PostReviveTakeAllHeadProgressThreshold = 0.8f;

    public static bool CanUsePaidReroll(RewardRollContext rollContext)
    {
        return rollContext.HasRevivedThisRun ||
            rollContext.HeadPathProgressNormalized >= PaidRerollHeadProgressThreshold;
    }

    public static bool CanUseTakeAll(RewardRollContext rollContext)
    {
        if (rollContext.HasRevivedThisRun)
            return rollContext.HeadPathProgressNormalized >= PostReviveTakeAllHeadProgressThreshold;

        return rollContext.HeadPathProgressNormalized >= TakeAllHeadProgressThreshold;
    }
}
