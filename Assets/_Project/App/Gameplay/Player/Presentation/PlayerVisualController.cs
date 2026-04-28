using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class PlayerVisualController : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    [Header("References")]
    [SerializeField] private PlayerMover _mover;

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float _movingThreshold = 0.05f;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private bool _hasAnimatorState;
    private bool _lastIsMoving;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_mover == null)
            _mover = GetComponentInParent<PlayerMover>();
    }

    private void OnEnable()
    {
        _hasAnimatorState = false;
    }

    private void LateUpdate()
    {
        if (_mover == null)
            return;

        float input = _mover.MovementInput;
        bool isMoving = Mathf.Abs(input) > _movingThreshold;

        if (!_hasAnimatorState || _lastIsMoving != isMoving)
        {
            _animator.SetBool(IsMovingHash, isMoving);
            _lastIsMoving = isMoving;
            _hasAnimatorState = true;
        }

        if (isMoving)
            _spriteRenderer.flipX = input < 0f;
    }
}
