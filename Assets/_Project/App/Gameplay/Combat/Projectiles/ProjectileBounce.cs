using UnityEngine;

public sealed class ProjectileBounce : MonoBehaviour
{
    private int _maxBounces;
    private bool _bounceX;
    private bool _bounceY;

    private int _bouncesLeft;
    private ProjectileMovement _movement;
    private IScreenBounds _screenBounds;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>();
    }

    public void Init(IScreenBounds screenBounds)
    {
        _screenBounds = screenBounds;
    }

    public void ResetBounces()
    {
        _bouncesLeft = _maxBounces;
    }

    public void Tick()
    {
        if (_bouncesLeft <= 0) return;

        if (_screenBounds == null)
        {
            Debug.LogError("ProjectileBounce: screen bounds are not initialized.", this);
            return;
        }

        Vector3 pos = transform.position;
        Vector2 dir = _movement.Direction;

        bool bounced = false;

        if (_bounceX && (pos.x <= _screenBounds.Left || pos.x >= _screenBounds.Right))
        {
            dir.x *= -1;
            bounced = true;
        }

        if (_bounceY && (pos.y >= _screenBounds.Top || pos.y <= _screenBounds.Bottom))
        {
            dir.y *= -1;
            bounced = true;
        }

        if (bounced)
        {
            _bouncesLeft--;
            _movement.SetDirection(dir);
        }
    }

    public void SetBounces(int bounceCount, bool bounceX, bool bounceY)
    {
        _maxBounces = bounceCount;
        _bounceX = bounceX;
        _bounceY = bounceY;
    }
}
