using UnityEngine;

[DisallowMultipleComponent]
public sealed class EntryPoint : MonoBehaviour
{
    private static EntryPoint _instance;
    private Bootstrap _bootstrap;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _bootstrap = new Bootstrap(new SceneLoader());
    }

    private void Start()
    {
        _bootstrap.StartGame();
    }
}