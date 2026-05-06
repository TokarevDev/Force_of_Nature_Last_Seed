using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class WormReviveFlowController : MonoBehaviour
{
    [SerializeField] private WormController _wormController;
    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private PoolRegistry _projectilePoolRegistry;
    [SerializeField] private ProjectileWeapon _projectileWeapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [SerializeField] private WormDamagePopupPresenter _damagePopupPresenter;
    [SerializeField] private PopupRoot _popupRoot;
    [SerializeField] private RevivalPopupView _revivalPopup;
    [SerializeField] private RewardedAdService _rewardedAdService;
    [SerializeField, Min(0)] private int _maxReviveAttempts = 3;
    [SerializeField, Range(0f, 1f)] private float _fallbackRemainingLevelNormalized = 0.5f;
    [SerializeField] private bool _reloadCurrentSceneOnGiveUp = true;

    private int _remainingRevives;
    private bool _isFailState;
    private bool _isReviving;

    private void Awake()
    {
        _remainingRevives = _maxReviveAttempts;
    }

    private void OnEnable()
    {
        if (_wormController != null)
            _wormController.PathCompleted += HandlePathCompleted;

        if (_revivalPopup != null)
        {
            _revivalPopup.ReviveRequested += HandleReviveRequested;
            _revivalPopup.GiveUpRequested += HandleGiveUpRequested;
        }
    }

    private void OnDisable()
    {
        if (_wormController != null)
            _wormController.PathCompleted -= HandlePathCompleted;

        if (_revivalPopup != null)
        {
            _revivalPopup.ReviveRequested -= HandleReviveRequested;
            _revivalPopup.GiveUpRequested -= HandleGiveUpRequested;
        }

        _popupRoot?.ReleaseGameplayLock();
    }

    private void HandlePathCompleted()
    {
        if (_isFailState || _isReviving)
            return;

        ClearTransientGameplay();
        _isFailState = true;
        ShowRevivalPopup();
    }

    private void ShowRevivalPopup()
    {
        if (_popupRoot == null || _revivalPopup == null)
        {
            Debug.LogError("WormReviveFlowController: popup references are missing.", this);
            return;
        }

        _revivalPopup.Bind(
            _remainingRevives,
            GetCurrentLevelProgressNormalized(),
            GetCurrentRemainingLevelNormalized(),
            _remainingRevives > 0);

        _popupRoot.Show(_revivalPopup);
    }

    private void HandleReviveRequested()
    {
        if (!_isFailState || _isReviving || _remainingRevives <= 0)
            return;

        _revivalPopup.SetWaitingForAd(true);

        if (_rewardedAdService == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("WormReviveFlowController: rewarded ad service is missing. Granting revive in editor/development build.", this);
            CompleteRewardedAd(true);
#else
            Debug.LogError("WormReviveFlowController: rewarded ad service is missing.", this);
            _revivalPopup.SetWaitingForAd(false);
#endif
            return;
        }

        if (!_rewardedAdService.IsReady)
        {
            Debug.LogWarning("WormReviveFlowController: rewarded ad is not ready.", this);
            _revivalPopup.SetWaitingForAd(false);
            return;
        }

        _rewardedAdService.ShowRewardedAd(CompleteRewardedAd);
    }

    private void CompleteRewardedAd(bool rewardGranted)
    {
        if (!rewardGranted)
        {
            _revivalPopup.SetWaitingForAd(false);
            return;
        }

        StartReviveRollback();
    }

    private void StartReviveRollback()
    {
        if (_wormController == null)
        {
            Debug.LogError("WormReviveFlowController: worm controller is missing.", this);
            _popupRoot?.ReleaseGameplayLock();
            return;
        }

        _remainingRevives = Mathf.Max(0, _remainingRevives - 1);
        _isReviving = true;
        _popupRoot?.HideActive(releaseGameplayLock: false);

        if (!_wormController.RollbackToReviveStart(CompleteReviveRollback))
            CompleteReviveRollback();
    }

    private void CompleteReviveRollback()
    {
        _isFailState = false;
        _isReviving = false;
        _popupRoot?.ReleaseGameplayLock();
    }

    private void HandleGiveUpRequested()
    {
        _popupRoot?.HideActive();

        if (!_reloadCurrentSceneOnGiveUp)
            return;

        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private float GetCurrentRemainingLevelNormalized()
    {
        if (_wormCombat == null || _wormCombat.TotalProgressSegments <= 0)
            return _fallbackRemainingLevelNormalized;

        return _wormCombat.RemainingProgressNormalized;
    }

    private float GetCurrentLevelProgressNormalized()
    {
        if (_wormCombat == null || _wormCombat.TotalProgressSegments <= 0)
            return 1f - _fallbackRemainingLevelNormalized;

        return _wormCombat.DestructionProgressNormalized;
    }

    private void ClearTransientGameplay()
    {
        _projectilePoolRegistry?.ReleaseAllActiveProjectiles();
        _projectileWeapon?.ClearTransientState();
        _acaciaThornWeapon?.ClearTransientState();
        _damagePopupPresenter?.ClearActivePopups();
    }
}
