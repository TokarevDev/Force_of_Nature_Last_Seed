public static class GameplayInputBlocker
{
    private static int _lockCount;

    public static bool IsBlocked => _lockCount > 0;

    public static void PushLock()
    {
        _lockCount++;
    }

    public static void PopLock()
    {
        if (_lockCount <= 0)
            return;

        _lockCount--;
    }
}
