using UnityEngine;

public enum AcaciaThornUpgradeType
{
    Unlock = 0,
    DamageMultiplier = 1,
    FireRateBonus = 2,
    ExtraSplitProjectiles = 3
}

[CreateAssetMenu(menuName = "Game/Rewards/Effects/Acacia Thorn")]
public sealed class AcaciaThornRewardEffect : RewardEffect
{
    [SerializeField] private AcaciaThornUpgradeType _upgradeType;
    [SerializeField] private float _damageMultiplier = 1.5f;
    [SerializeField] private float _fireRateBonus = 0.5f;
    [SerializeField] private int _extraSplitProjectiles = 1;

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

            case AcaciaThornUpgradeType.ExtraSplitProjectiles:
                return state.CanApplyExtraSplitProjectiles(_extraSplitProjectiles);

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
                weapon.Unlock();
                break;

            case AcaciaThornUpgradeType.DamageMultiplier:
                weapon.AddDamageMultiplier(_damageMultiplier);
                break;

            case AcaciaThornUpgradeType.FireRateBonus:
                weapon.AddFireRateBonus(_fireRateBonus);
                break;

            case AcaciaThornUpgradeType.ExtraSplitProjectiles:
                weapon.AddExtraSplitProjectiles(_extraSplitProjectiles);
                break;
        }
    }

    public override void Apply(WeaponRuntimeState state)
    {
    }
}
