using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class RewardInstaller : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RewardDatabase _database;

    [SerializeField] private RewardPopupView _popup;
    [SerializeField] private PopupRoot _popupRoot;
    [SerializeField] private ProjectileWeapon _weapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [FormerlySerializedAs("_takeAllRewardedAdService")]
    [SerializeField] private RewardedAdService _rewardedAdService;

    [Header("Session Attempts")]
    [FormerlySerializedAs("_freeRerollAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _freeRerollAttemptsPerSession = 2;
    [FormerlySerializedAs("_adRerollAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _adRerollAttemptsPerSession = 2;
    [FormerlySerializedAs("_takeAllAttemptsPerPopup")]
    [SerializeField][Min(0)] private int _takeAllAttemptsPerSession = 2;

    private RewardFlowController _rewardFlow;

    public IReadOnlyList<CocoonRewardProfile> CocoonProfiles =>
        _database != null
            ? _database.CocoonProfiles
            : CocoonRewardProfile.Defaults;

    private void Awake()
    {
        var roll = new RewardRollService(_database);
        var apply = new RewardApplyService(_weapon, _acaciaThornWeapon);

        _rewardFlow = new RewardFlowController(
            roll,
            apply,
            _popup,
            _popupRoot,
            _rewardedAdService,
            _freeRerollAttemptsPerSession,
            _adRerollAttemptsPerSession,
            _takeAllAttemptsPerSession);
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
