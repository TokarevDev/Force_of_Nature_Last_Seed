using UnityEngine;

public sealed class ProjectileMovement : MonoBehaviour
{
    public Vector2 Direction { get; private set; }
    private float _speed;

    public void SetDirection(Vector2 dir)
    {
        Direction = dir.normalized;
    }

    public void Tick()
    {
        transform.position += (Vector3) (Direction * _speed * Time.deltaTime);
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }
}