using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerVisualAnimator : MonoBehaviour
{
    private const float MovingThreshold = 0.05f;
    private const float IdleFrequency = 1.35f;
    private const float IdleBob = 0.035f;
    private const float IdleSquash = 0.025f;
    private const float IdleTilt = 1.5f;
    private const float MovementBob = 0.04f;
    private const float MovementSquash = 0.04f;
    private const float MovementTilt = 4f;

    [Header("Sprites")]
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite[] _movementFrames;

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float _movementScale = 1.5f;
    [SerializeField, Min(1f)] private float _animationSpeed = 8f;
    [SerializeField, Range(0f, 2f)] private float _bounceStrength = 1f;

    private SpriteRenderer _spriteRenderer;
    private PlayerMover _mover;
    private Vector3 _baseLocalPosition;
    private Vector3 _baseLocalScale;
    private Quaternion _baseLocalRotation;
    private Vector3 _lastWorldPosition;
    private float _fallbackInput;
    private bool _hasLastWorldPosition;

    private void Awake()
    {
        ResolveReferences();

        if (_idleSprite == null && _spriteRenderer != null)
            _idleSprite = _spriteRenderer.sprite;
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheBasePose();
        _hasLastWorldPosition = false;
    }

    private void LateUpdate()
    {
        if (_spriteRenderer == null)
            return;

        float input = ResolveInput();

        if (Mathf.Abs(input) > MovingThreshold)
        {
            AnimateMovement(Mathf.Sign(input));
            return;
        }

        AnimateIdle();
    }

    private void ResolveReferences()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_mover == null)
            _mover = GetComponentInParent<PlayerMover>();
    }

    private void CacheBasePose()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalScale = transform.localScale;
        _baseLocalRotation = transform.localRotation;
    }

    private float ResolveInput()
    {
        if (_mover != null)
            return _mover.MovementInput;

        Vector3 worldPosition = transform.parent != null ? transform.parent.position : transform.position;

        if (!_hasLastWorldPosition)
        {
            _lastWorldPosition = worldPosition;
            _hasLastWorldPosition = true;
            return 0f;
        }

        float deltaX = worldPosition.x - _lastWorldPosition.x;
        _lastWorldPosition = worldPosition;

        float targetInput = Mathf.Clamp(deltaX / Mathf.Max(Time.deltaTime, 0.0001f), -1f, 1f);
        _fallbackInput = Mathf.Lerp(_fallbackInput, targetInput, 20f * Time.deltaTime);
        return _fallbackInput;
    }

    private void AnimateIdle()
    {
        if (_idleSprite != null)
            _spriteRenderer.sprite = _idleSprite;

        _spriteRenderer.flipX = false;

        float wave = Mathf.Sin(Time.time * IdleFrequency * Mathf.PI * 2f);
        float squash = wave * IdleSquash * _bounceStrength;

        transform.localPosition = _baseLocalPosition + Vector3.up * (wave * IdleBob * _bounceStrength);
        transform.localScale = new Vector3(
            _baseLocalScale.x * (1f + squash * 0.5f),
            _baseLocalScale.y * (1f - squash),
            _baseLocalScale.z);
        transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, wave * IdleTilt * _bounceStrength);
    }

    private void AnimateMovement(float direction)
    {
        if (_movementFrames != null && _movementFrames.Length > 0)
        {
            int frame = Mathf.FloorToInt(Time.time * _animationSpeed) % _movementFrames.Length;
            Sprite movementSprite = _movementFrames[frame];

            if (movementSprite != null)
                _spriteRenderer.sprite = movementSprite;
        }

        _spriteRenderer.flipX = direction < 0f;

        float step = Mathf.Sin(Time.time * _animationSpeed * Mathf.PI * 2f);
        float stepAbs = Mathf.Abs(step);
        float squash = stepAbs * MovementSquash * _bounceStrength;

        transform.localPosition = _baseLocalPosition + Vector3.up * (stepAbs * MovementBob * _bounceStrength);
        transform.localScale = new Vector3(
            _baseLocalScale.x * _movementScale * (1f + squash * 0.5f),
            _baseLocalScale.y * _movementScale * (1f - squash),
            _baseLocalScale.z);
        transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, -direction * MovementTilt * stepAbs * _bounceStrength);
    }
}
