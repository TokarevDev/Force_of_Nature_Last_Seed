using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Projectile Config")]
public sealed class ProjectileConfig : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private Projectile _prefab;

    public Projectile Prefab => _prefab;

    [Header("Visual")]
    [SerializeField] private Sprite _sprite;

    [SerializeField] private float _rotateSprite = 0f;

    [Header("Weapon")]
    [SerializeField] private int _damage = 5;

    [SerializeField] private int _penetration = 0;

    [Header("Movement")]
    [SerializeField] private float _lifeTime = 2f;

    [SerializeField] private float _speed = 6f;

    [Header("Bounce")]
    [SerializeField] private int _bounceCount = 0;

    [SerializeField] private bool _bounceX = true;
    [SerializeField] private bool _bounceY = false;

    [Header("Split")]
    [SerializeField] private int _splitCount = 0;

    [SerializeField] private float _splitAngle = 15f;

    public Sprite Sprite => _sprite;
    public float RotateSprite => _rotateSprite;

    public int Damage => _damage;
    public int Penetration => _penetration;

    public float LifeTime => _lifeTime;
    public float Speed => _speed;

    public int BounceCount => _bounceCount;
    public bool BounceX => _bounceX;
    public bool BounceY => _bounceY;

    public int SplitCount => _splitCount;
    public float SplitAngle => _splitAngle;
}