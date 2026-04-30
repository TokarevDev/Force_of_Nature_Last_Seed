using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMover _mover;
    [SerializeField] private InputReader _input;

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_input.HasTouchTargetX)
        {
            _mover.MoveTowardNormalizedX(_input.TouchTargetXNormalized);
            return;
        }

        _mover.Move(_input.MoveX);
    }
}
