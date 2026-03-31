using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardInstaller : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RewardDatabase _database;

    [SerializeField] private RewardPopupView _popup;
    [SerializeField] private ProjectileWeapon _weapon;

    private RewardFlowController _rewardFlow;

    private void Awake()
    {
        var roll = new RewardRollService(_database);
        var apply = new RewardApplyService(_weapon);

        _rewardFlow = new RewardFlowController(roll, apply, _popup);
    }

    public void OpenReward()
    {
        _rewardFlow.Open();
    }
}