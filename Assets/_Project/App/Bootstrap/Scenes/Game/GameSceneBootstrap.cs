using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSceneBootstrap : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private PlayerMover _playerMover;
    [SerializeField] private PlayerShooter _playerShooter;
    [SerializeField] private PoolRegistry _poolRegistry;

    private ScreenBoundsService _screenBounds;

    private void Awake()
    {
        ResolveReferences();

        _screenBounds = new ScreenBoundsService();
        _screenBounds.Recalculate(_worldCamera);

        if (_playerMover != null)
            _playerMover.Init(_screenBounds);

        if (_poolRegistry != null)
            _poolRegistry.Init(_screenBounds);

        if (_playerShooter != null)
            _playerShooter.Init();
    }

    private void ResolveReferences()
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_playerShooter == null && _playerMover != null)
            _playerShooter = _playerMover.GetComponent<PlayerShooter>();

        if (_worldCamera == null)
            Debug.LogError("GameSceneBootstrap: world camera is missing.", this);

        if (_playerMover == null)
            Debug.LogError("GameSceneBootstrap: player mover is missing.", this);

        if (_poolRegistry == null)
            Debug.LogError("GameSceneBootstrap: pool registry is missing.", this);

        if (_playerShooter == null)
            Debug.LogError("GameSceneBootstrap: player shooter is missing.", this);
    }
}
