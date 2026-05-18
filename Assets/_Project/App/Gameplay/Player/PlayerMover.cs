using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _edgePadding = 0.5f;

    private IScreenBounds _screenBounds;
    private Vector3 _startPosition;
    private float _currentInput;

    public float MovementInput => _currentInput;

    private void Awake()
    {
        _startPosition = transform.position;
    }

    public void Init(IScreenBounds screenBounds)
    {
        _screenBounds = screenBounds;
    }

    public void ResetForNewRun()
    {
        _currentInput = 0f;
        transform.position = _startPosition;
    }

    public void Move(float inputX)
    {
        if (!CanProcessMovement())
            return;

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            if (Mathf.Approximately(inputX, 0f))
                _currentInput = 0f;

            return;
        }

        _currentInput = Mathf.Lerp(_currentInput, inputX, _smooth * deltaTime);

        Vector3 pos = transform.position;

        pos.x += _currentInput * _speed * deltaTime;

        float minX = _screenBounds.Left + _edgePadding;
        float maxX = _screenBounds.Right - _edgePadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        transform.position = pos;
    }

    public void MoveByNormalizedScreenDeltaX(float normalizedDeltaX)
    {
        if (!CanProcessMovement())
            return;

        float minX = _screenBounds.Left + _edgePadding;
        float maxX = _screenBounds.Right - _edgePadding;
        float movementAreaWidth = maxX - minX;
        float movementDeltaX = normalizedDeltaX * movementAreaWidth;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + movementDeltaX, minX, maxX);
        transform.position = pos;

        _currentInput = Mathf.Approximately(movementDeltaX, 0f)
            ? 0f
            : Mathf.Sign(movementDeltaX);
    }

    private bool CanProcessMovement()
    {
        if (_screenBounds == null)
        {
            Debug.LogError("PlayerMover: screen bounds are not initialized.", this);
            return false;
        }

        if (!GameplayInputBlocker.IsBlocked)
            return true;

        Stop();
        return false;
    }

    private void Stop()
    {
        _currentInput = 0f;
    }
}
