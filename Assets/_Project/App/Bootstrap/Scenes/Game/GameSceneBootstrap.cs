using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSceneBootstrap : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private PlayerMover _playerMover;
    [SerializeField] private PoolRegistry _poolRegistry;

    private ScreenBoundsService _screenBounds;

    private void Awake()
    {
        _screenBounds = new ScreenBoundsService();
        _screenBounds.Recalculate(_worldCamera);

        _playerMover.Init(_screenBounds);
        _poolRegistry.Init(_screenBounds);
    }
}