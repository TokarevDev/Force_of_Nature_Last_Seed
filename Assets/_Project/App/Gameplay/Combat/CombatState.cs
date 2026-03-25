using System;

public static class CombatState
{
    public static bool CanShoot { get; private set; }

    public static event Action<bool> OnShootStateChanged;

    public static void SetShootEnabled(bool value)
    {
        if (CanShoot == value)
            return;

        CanShoot = value;
        OnShootStateChanged?.Invoke(value);
    }
}