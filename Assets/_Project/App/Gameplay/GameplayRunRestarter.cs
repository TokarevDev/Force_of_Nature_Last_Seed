using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayRunRestarter : MonoBehaviour
{
    [Header("Worm")]
    [SerializeField] private WormSpawner _wormSpawner;
    [SerializeField] private WormPressureDirector _pressureDirector;
    [SerializeField] private WormEngagementController _engagementController;
    [SerializeField] private WormReviveFlowController _reviveFlowController;

    [Header("Player")]
    [SerializeField] private PlayerMover _playerMover;
    [SerializeField] private InputReader _inputReader;

    [Header("Weapons")]
    [SerializeField] private ProjectileWeapon _projectileWeapon;
    [SerializeField] private AcaciaThornWeapon _acaciaThornWeapon;
    [SerializeField] private PoolRegistry _projectilePoolRegistry;

    [Header("Rewards/UI")]
    [SerializeField] private RewardInstaller _rewardInstaller;
    [SerializeField] private PopupRoot _popupRoot;
    [SerializeField] private WormDamagePopupPresenter _damagePopupPresenter;

    private bool _isRestarting;

    private void OnEnable()
    {
        GameplayRunRestartEvents.RestartRequested += RestartRun;
    }

    private void OnDisable()
    {
        GameplayRunRestartEvents.RestartRequested -= RestartRun;
    }

    public void RestartRun()
    {
        if (_isRestarting)
            return;

        _isRestarting = true;

        _popupRoot?.HideActive();
        _popupRoot?.ReleaseGameplayLock();
        Time.timeScale = 1f;

        _inputReader?.ResetMovement();
        _playerMover?.ResetForNewRun();
        _engagementController?.ResetState();
        _pressureDirector?.ResetForNewRun();
        _reviveFlowController?.ResetForNewRun();

        _projectilePoolRegistry?.ReleaseAllActiveProjectiles();
        _projectileWeapon?.ClearTransientState();
        _acaciaThornWeapon?.ClearTransientState();
        _damagePopupPresenter?.ClearActivePopups();

        _wormSpawner?.DespawnWorm();

        _projectileWeapon?.ResetRuntimeState();
        _acaciaThornWeapon?.ResetRuntimeState();
        _rewardInstaller?.ResetSession();

        _wormSpawner?.SpawnWorm();

        _isRestarting = false;
    }
}
