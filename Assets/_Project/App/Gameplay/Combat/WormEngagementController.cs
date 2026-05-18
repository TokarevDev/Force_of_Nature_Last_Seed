using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormEngagementController : MonoBehaviour
{
    private int _wormsInside;

    private void OnEnable()
    {
        WormCombatController.OnWormDied += HandleWormDied;
    }

    private void OnDisable()
    {
        WormCombatController.OnWormDied -= HandleWormDied;
    }

    private void Start()
    {
        _wormsInside = 0;
        CombatState.SetShootEnabled(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<WormSegment>(out var segment))
            return;

        if (segment.Type != WormSegmentType.Head)
            return;

        _wormsInside++;

        if (_wormsInside == 1)
            CombatState.SetShootEnabled(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<WormSegment>(out var segment))
            return;

        if (segment.Type != WormSegmentType.Head)
            return;

        _wormsInside = Mathf.Max(0, _wormsInside - 1);

        if (_wormsInside == 0)
            CombatState.SetShootEnabled(false);
    }

    private void HandleWormDied()
    {
        ResetState();
    }

    public void ResetState()
    {
        _wormsInside = 0;
        CombatState.SetShootEnabled(false);
    }
}
