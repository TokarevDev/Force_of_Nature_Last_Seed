using System;
using UnityEngine;

public static class GameplayRunRestartEvents
{
    public static event Action RestartRequested;

    public static void RequestRestart()
    {
        if (RestartRequested == null)
        {
            Debug.LogWarning("GameplayRunRestartEvents: restart requested but no listener is registered.");
            return;
        }

        RestartRequested.Invoke();
    }
}
