using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormVictoryPopupController : MonoBehaviour
{
    [SerializeField] private string _victoryPopupId = "WinPopup";

    private void OnEnable()
    {
        WormCombatController.OnWormDied += HandleWormDied;
    }

    private void OnDisable()
    {
        WormCombatController.OnWormDied -= HandleWormDied;
    }

    private void HandleWormDied()
    {
        if (string.IsNullOrEmpty(_victoryPopupId))
        {
            Debug.LogWarning("WormVictoryPopupController: victory popup id is empty.", this);
            return;
        }

        PopupEvents.Show(_victoryPopupId);
    }
}
