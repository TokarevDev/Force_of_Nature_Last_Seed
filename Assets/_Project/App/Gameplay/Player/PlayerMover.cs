using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _smooth = 10f;
    [SerializeField] private float _edgePadding = 0.5f;

    private IScreenBounds _screenBounds;
    private float _currentInput;

    public void Init(IScreenBounds screenBounds)
    {
        _screenBounds = screenBounds;
    }

    public void Move(float inputX)
    {
        if (_screenBounds == null)
        {
            Debug.LogError("PlayerMover: screen bounds are not initialized.", this);
            return;
        }

        _currentInput = Mathf.Lerp(_currentInput, inputX, _smooth * Time.deltaTime);

        Vector3 pos = transform.position;

        pos.x += _currentInput * _speed * Time.deltaTime;

        float minX = _screenBounds.Left + _edgePadding;
        float maxX = _screenBounds.Right - _edgePadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        transform.position = pos;
    }
}
