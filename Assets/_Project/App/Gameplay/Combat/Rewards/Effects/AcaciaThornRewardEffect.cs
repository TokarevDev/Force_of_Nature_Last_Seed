using UnityEngine;

public enum AcaciaThornUpgradeType
{
    Unlock = 0,
    DamageMultiplier = 1,
    FireRateBonus = 2,
    ExtraSalvoShots = 3,
    ProjectileSpeedBonus = 4,
    CriticalChance = 5,
    CriticalPower = 6
}

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Acacia Thorn")]
public sealed class AcaciaThornRewardEffect : RewardEffect
{
    [SerializeField] private AcaciaThornUpgradeType _upgradeType;
    [SerializeField] private float _damageMultiplier = 1.5f;
    [SerializeField] private float _fireRateBonus = 0.5f;
    [SerializeField] private int _extraSalvoShots = 1;
    [SerializeField] private float _projectileSpeedBonus = 0.25f;
    [SerializeField][Range(0f, 1f)] private float _criticalChanceBonus = 0.1f;
    [SerializeField][Min(0f)] private float _criticalDamageBonus = 0.5f;

    public override bool CanApply(RewardRuntimeContext context)
    {
        AcaciaThornWeapon weapon = context?.AcaciaThornWeapon;
        AcaciaThornRuntimeState state = weapon != null ? weapon.RuntimeState : null;

        if (state == null)
            return false;

        switch (_upgradeType)
        {
            case AcaciaThornUpgradeType.Unlock:
                return state.CanUnlock;

            case AcaciaThornUpgradeType.DamageMultiplier:
                return state.CanApplyDamageMultiplier(_damageMultiplier);

            case AcaciaThornUpgradeType.FireRateBonus:
                return state.CanApplyFireRateBonus(_fireRateBonus);

            case AcaciaThornUpgradeType.ExtraSalvoShots:
                return state.CanApplySalvoShots(_extraSalvoShots);

            case AcaciaThornUpgradeType.ProjectileSpeedBonus:
                return state.CanApplyProjectileSpeedBonus(_projectileSpeedBonus);

            case AcaciaThornUpgradeType.CriticalChance:
                return state.CanApplyCriticalChance(_criticalChanceBonus);

            case AcaciaThornUpgradeType.CriticalPower:
                return state.CanApplyCriticalDamageBonus(_criticalDamageBonus);

            default:
                return false;
        }
    }

    public override bool CanApply(WeaponRuntimeState state)
    {
        return false;
    }

    public override void Apply(RewardRuntimeContext context)
    {
        AcaciaThornWeapon weapon = context?.AcaciaThornWeapon;

        if (weapon == null)
            return;

        switch (_upgradeType)
        {
            case AcaciaThornUpgradeType.Unlock:
                weapon.Unlock(GetMainWeaponDamageSnapshot(context));
                break;

            case AcaciaThornUpgradeType.DamageMultiplier:
                weapon.AddDamageMultiplier(_damageMultiplier);
                break;

            case AcaciaThornUpgradeType.FireRateBonus:
                weapon.AddFireRateBonus(_fireRateBonus);
                break;

            case AcaciaThornUpgradeType.ExtraSalvoShots:
                weapon.AddSalvoShots(_extraSalvoShots);
                break;

            case AcaciaThornUpgradeType.ProjectileSpeedBonus:
                weapon.AddProjectileSpeedBonus(_projectileSpeedBonus);
                break;

            case AcaciaThornUpgradeType.CriticalChance:
                weapon.AddCriticalChance(_criticalChanceBonus);
                break;

            case AcaciaThornUpgradeType.CriticalPower:
                weapon.AddCriticalDamageBonus(_criticalDamageBonus);
                break;
        }
    }

    public override void Apply(WeaponRuntimeState state)
    {
    }

    private static int GetMainWeaponDamageSnapshot(RewardRuntimeContext context)
    {
        ProjectileWeapon mainWeapon = context?.MainWeapon;
        return mainWeapon != null ? mainWeapon.CurrentProjectileDamage : 0;
    }
}
