using System;

public static class PopupEvents
{
    public static event Action<string> ShowRequested;
    public static event Action HideActiveRequested;

    public static void Show(string popupId)
    {
        if (string.IsNullOrEmpty(popupId))
            return;

        ShowRequested?.Invoke(popupId);
    }

    public static void HideActive()
    {
        HideActiveRequested?.Invoke();
    }
}
