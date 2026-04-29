using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardInstaller : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RewardDatabase _database;

    [SerializeField] private RewardPopupView _popup;
    [SerializeField] private ProjectileWeapon _weapon;

    private RewardFlowController _rewardFlow;

    public IReadOnlyList<CocoonRewardProfile> CocoonProfiles =>
        _database != null
            ? _database.CocoonProfiles
            : CocoonRewardProfile.Defaults;

    private void Awake()
    {
        var roll = new RewardRollService(_database);
        var apply = new RewardApplyService(_weapon);

        _rewardFlow = new RewardFlowController(roll, apply, _popup);
    }

    private void OnDestroy()
    {
        _rewardFlow?.Dispose();
    }

    public bool OpenReward()
    {
        return OpenReward(null);
    }

    public bool OpenReward(CocoonRewardProfile cocoonProfile)
    {
        return _rewardFlow != null && _rewardFlow.Open(cocoonProfile);
    }
}
