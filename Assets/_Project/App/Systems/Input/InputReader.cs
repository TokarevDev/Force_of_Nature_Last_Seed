using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class InputReader : MonoBehaviour
{
    [FormerlySerializedAs("_enableTouchTarget")]
    [SerializeField] private bool _enableTouchDrag = true;

    public float MoveX { get; private set; }
    public bool HasActiveTouch { get; private set; }

    private InputActions _input;
    private InputAction _touchPressAction;
    private InputAction _touchPositionAction;
    private float _lastTouchX;
    private float _touchDeltaXNormalized;

    private void Awake()
    {
        _input = new InputActions();
        _touchPressAction = new InputAction(
            "TouchPress",
            InputActionType.Button,
            "<Touchscreen>/primaryTouch/press");
        _touchPositionAction = new InputAction(
            "TouchPosition",
            InputActionType.PassThrough,
            "<Touchscreen>/primaryTouch/position",
            expectedControlType: "Vector2");
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;
        EnableTouchActions();
    }

    private void OnDisable()
    {
        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;
        _input.Player.Disable();
        DisableTouchActions();
        ResetMovement();
    }

    private void OnDestroy()
    {
        _touchPressAction?.Dispose();
        _touchPositionAction?.Dispose();
        _input?.Dispose();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveX = context.ReadValue<float>();
    }

    private void OnTouchPressed(InputAction.CallbackContext context)
    {
        if (!_enableTouchDrag)
            return;

        BeginTouch(_touchPositionAction.ReadValue<Vector2>());
    }

    private void OnTouchPositionChanged(InputAction.CallbackContext context)
    {
        if (!_enableTouchDrag || !HasActiveTouch || !_touchPressAction.IsPressed())
            return;

        AccumulateTouchDelta(context.ReadValue<Vector2>());
    }

    public float ConsumeTouchDeltaXNormalized()
    {
        float delta = _touchDeltaXNormalized;
        _touchDeltaXNormalized = 0f;

        return delta;
    }

    private void BeginTouch(Vector2 screenPosition)
    {
        _lastTouchX = screenPosition.x;
        _touchDeltaXNormalized = 0f;
        HasActiveTouch = true;
    }

    private void AccumulateTouchDelta(Vector2 screenPosition)
    {
        float currentTouchX = screenPosition.x;
        float deltaX = currentTouchX - _lastTouchX;

        _lastTouchX = currentTouchX;
        _touchDeltaXNormalized += deltaX / Mathf.Max(1f, Screen.width);
    }

    private void EnableTouchActions()
    {
        if (!_enableTouchDrag)
            return;

        _touchPressAction.performed += OnTouchPressed;
        _touchPressAction.canceled += OnTouchCanceled;
        _touchPositionAction.performed += OnTouchPositionChanged;
        _touchPositionAction.Enable();
        _touchPressAction.Enable();
    }

    private void DisableTouchActions()
    {
        _touchPressAction.performed -= OnTouchPressed;
        _touchPressAction.canceled -= OnTouchCanceled;
        _touchPositionAction.performed -= OnTouchPositionChanged;
        _touchPressAction.Disable();
        _touchPositionAction.Disable();
    }

    private void OnTouchCanceled(InputAction.CallbackContext context)
    {
        ResetTouchDrag();
    }

    private void ResetTouchDrag()
    {
        HasActiveTouch = false;
        _lastTouchX = 0f;
        _touchDeltaXNormalized = 0f;
    }

    private void ResetMovement()
    {
        MoveX = 0f;
        ResetTouchDrag();
    }
}
