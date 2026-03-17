using UnityEngine;

public sealed class ProjectileBounce : MonoBehaviour
{
    private int _maxBounces;
    private bool _bounceX;
    private bool _bounceY;

    private int _bouncesLeft;
    private ProjectileMovement _movement;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>();
    }

    public void ResetBounces()
    {
        _bouncesLeft = _maxBounces;
    }

    public void Tick()
    {
        if (_bouncesLeft <= 0) return;

        Vector3 pos = transform.position;
        Vector2 dir = _movement.Direction;

        bool bounced = false;

        if (_bounceX && (pos.x <= ScreenBounds.Left || pos.x >= ScreenBounds.Right))
        {
            dir.x *= -1;
            bounced = true;
        }

        if (_bounceY && (pos.y >= ScreenBounds.Top || pos.y <= ScreenBounds.Bottom))
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