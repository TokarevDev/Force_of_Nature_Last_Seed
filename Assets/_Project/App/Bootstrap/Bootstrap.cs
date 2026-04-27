public sealed class Bootstrap
{
    private readonly SceneLoader _sceneLoader;

    public Bootstrap(SceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    public void StartGame()
    {
        _sceneLoader.LoadGame();
    }
}