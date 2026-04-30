using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    private const float TargetEpsilon = 0.01f;

    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _edgePadding = 0.5f;

    private IScreenBounds _screenBounds;
    private float _currentInput;

    public float MovementInput => _currentInput;

    public void Init(IScreenBounds screenBounds)
    {
        _screenBounds = screenBounds;
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

    public void MoveTowardNormalizedX(float normalizedX)
    {
        if (!CanProcessMovement())
            return;

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        float minX = _screenBounds.Left + _edgePadding;
        float maxX = _screenBounds.Right - _edgePadding;
        float targetX = Mathf.Lerp(minX, maxX, Mathf.Clamp01(normalizedX));

        Vector3 pos = transform.position;
        float deltaX = targetX - pos.x;

        if (Mathf.Abs(deltaX) <= TargetEpsilon)
        {
            pos.x = targetX;
            transform.position = pos;
            _currentInput = 0f;
            return;
        }

        float targetInput = Mathf.Sign(deltaX);

        if (!Mathf.Approximately(_currentInput, 0f)
            && Mathf.Sign(_currentInput) != targetInput)
        {
            _currentInput = 0f;
        }

        _currentInput = Mathf.Lerp(_currentInput, targetInput, _smooth * deltaTime);

        float step = _currentInput * _speed * deltaTime;

        if (Mathf.Abs(step) >= Mathf.Abs(deltaX))
        {
            pos.x = targetX;
            _currentInput = 0f;
        }
        else
        {
            pos.x += step;
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
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
