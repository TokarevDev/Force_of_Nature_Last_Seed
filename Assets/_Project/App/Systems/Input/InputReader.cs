using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class InputReader : MonoBehaviour
{
    [SerializeField] private bool _enableTouchTarget = true;

    public float MoveX { get; private set; }
    public bool HasTouchTargetX { get; private set; }
    public float TouchTargetXNormalized { get; private set; }

    private InputActions _input;
    private InputAction _touchPressAction;
    private InputAction _touchPositionAction;

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
        if (!_enableTouchTarget)
            return;

        SetTouchTarget(_touchPositionAction.ReadValue<Vector2>());
    }

    private void OnTouchPositionChanged(InputAction.CallbackContext context)
    {
        if (!_enableTouchTarget || !HasTouchTargetX || !_touchPressAction.IsPressed())
            return;

        SetTouchTarget(context.ReadValue<Vector2>());
    }

    private void SetTouchTarget(Vector2 screenPosition)
    {
        float screenWidth = Mathf.Max(1f, Screen.width);

        TouchTargetXNormalized = Mathf.Clamp01(screenPosition.x / screenWidth);
        HasTouchTargetX = true;
    }

    private void EnableTouchActions()
    {
        if (!_enableTouchTarget)
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
        ResetTouchTarget();
    }

    private void ResetTouchTarget()
    {
        HasTouchTargetX = false;
        TouchTargetXNormalized = 0f;
    }

    private void ResetMovement()
    {
        MoveX = 0f;
        ResetTouchTarget();
    }
}
