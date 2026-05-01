using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class PlayerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMover _mover;

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float _flipThreshold = 0.05f;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_mover == null)
            _mover = GetComponentInParent<PlayerMover>();
    }

    private void LateUpdate()
    {
        if (_mover == null)
            return;

        float input = _mover.MovementInput;

        if (Mathf.Abs(input) > _flipThreshold)
            _spriteRenderer.flipX = input < 0f;
    }
}
