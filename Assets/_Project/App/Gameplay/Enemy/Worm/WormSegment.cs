using DG.Tweening;
using UnityEngine;

public enum WormSegmentType
{
    None,
    Head,
    Body,
    Tail
}

/// <summary>
/// Represents a single worm segment instance.
/// Handles rendering setup, cocoon overlay logic,
/// collision state and lifecycle transitions.
/// </summary>
public sealed class WormSegment : MonoBehaviour
{
    private static readonly object SyncedCocoonShakeTarget = new();
    private static Sequence _syncedCocoonShakeSequence;
    private static float _syncedCocoonShakeZOffset;
    private static int _syncedCocoonShakeUsers;

    private const int CocoonShakeStepCount = 8;
    private const float CocoonShakeStepDuration = 0.1f;
    private const float CocoonShakeDuration = CocoonShakeStepDuration * CocoonShakeStepCount;

    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [Header("Cocoon Overlay (Visual Only)")]
    [SerializeField] private GameObject _cocoonVisual;

    [Header("Cocoon Shake")]
    [SerializeField, Min(0f)] private float _cocoonShakeInterval = 3f;
    [SerializeField, Min(0f)] private float _cocoonShakeAngle = 10f;

    private Collider2D _cachedCollider;
    private SpriteRenderer _renderer;
    private SpriteRenderer _cocoonRenderer;

    private Transform _cocoonTransform;
    private bool _usesSyncedCocoonShake;

    public Transform CachedTransform { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

    public bool HasCocoon { get; private set; }
    public bool IsAlive { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSyncedCocoonShakeState()
    {
        StopSyncedCocoonShake();
        _syncedCocoonShakeUsers = 0;
    }

    private void Awake()
    {
        CachedTransform = transform;
        _cachedCollider = GetComponent<Collider2D>();

        if (VisualRoot != null)
            _renderer = VisualRoot.GetComponentInChildren<SpriteRenderer>();

        if (_cocoonVisual != null)
        {
            _cocoonRenderer = _cocoonVisual.GetComponentInChildren<SpriteRenderer>(true);
            _cocoonTransform = _cocoonVisual.transform;
        }
    }

    private void OnEnable()
    {
        if (!HasCocoon)
            return;

        RegisterSyncedCocoonShake();
    }

    private void OnDisable()
    {
        UnregisterSyncedCocoonShake();
    }

    private void OnDestroy()
    {
        UnregisterSyncedCocoonShake();
    }

    private void LateUpdate()
    {
        if (HasCocoon && _cocoonTransform != null)
        {
            float shakeOffset = _usesSyncedCocoonShake
                ? _syncedCocoonShakeZOffset
                : 0f;

            _cocoonTransform.localEulerAngles =
                new Vector3(0f, 0f, -transform.eulerAngles.z + shakeOffset);
        }
    }

    public void SetSortingOrder(int order)
    {
        if (_renderer != null)
            _renderer.sortingOrder = order;

        if (_cocoonRenderer != null)
            _cocoonRenderer.sortingOrder = order + 100;
    }

    public void EnableCocoon()
    {
        EnableCocoon(Color.white);
    }

    public void EnableCocoon(Color visualColor)
    {
        if (Type != WormSegmentType.Body)
            return;

        HasCocoon = true;

        if (_cocoonRenderer != null)
            _cocoonRenderer.color = visualColor;

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(true);

        if (isActiveAndEnabled)
            RegisterSyncedCocoonShake();
    }

    public void DisableCocoon()
    {
        UnregisterSyncedCocoonShake();
        HasCocoon = false;

        if (_cocoonRenderer != null)
            _cocoonRenderer.color = Color.white;

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        IsAlive = true;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;

        DisableCocoon();
        Section = null;
    }

    public void PrepareForWorm()
    {
        IsAlive = true;
        Section = null;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;

        DisableCocoon();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void SetRuntimeVisible(bool visible)
    {
        if (!IsAlive)
            return;

        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (!visible)
            return;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;
    }

    public void KillVisualAndCollision()
    {
        IsAlive = false;

        if (VisualRoot != null && VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(false);

        if (_cachedCollider != null && _cachedCollider.enabled)
            _cachedCollider.enabled = false;

        DisableCocoon();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void RegisterSyncedCocoonShake()
    {
        if (_usesSyncedCocoonShake)
            return;

        if (_cocoonTransform == null || _cocoonShakeAngle <= 0f)
            return;

        _usesSyncedCocoonShake = true;
        _syncedCocoonShakeUsers++;

        EnsureSyncedCocoonShake(_cocoonShakeInterval, _cocoonShakeAngle);
    }

    private void UnregisterSyncedCocoonShake()
    {
        if (!_usesSyncedCocoonShake)
            return;

        _usesSyncedCocoonShake = false;
        _syncedCocoonShakeUsers = Mathf.Max(0, _syncedCocoonShakeUsers - 1);

        if (_syncedCocoonShakeUsers == 0)
            StopSyncedCocoonShake();
    }

    private static void EnsureSyncedCocoonShake(float interval, float angle)
    {
        if (_syncedCocoonShakeSequence != null && _syncedCocoonShakeSequence.IsActive())
            return;

        float strongAngle = angle * 0.8f;
        float mediumAngle = angle * 0.55f;
        float weakAngle = angle * 0.3f;
        float shakeDelay = Mathf.Max(0f, interval - CocoonShakeDuration);

        _syncedCocoonShakeSequence = DOTween.Sequence()
            .SetTarget(SyncedCocoonShakeTarget)
            .AppendInterval(shakeDelay)
            .Append(CreateSyncedCocoonShakeTween(angle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(-angle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(strongAngle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(-strongAngle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(mediumAngle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(-mediumAngle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(weakAngle, CocoonShakeStepDuration))
            .Append(CreateSyncedCocoonShakeTween(0f, CocoonShakeStepDuration))
            .SetLoops(-1, LoopType.Restart);
    }

    private static Tween CreateSyncedCocoonShakeTween(float targetAngle, float duration)
    {
        return DOTween
            .To(
                GetSyncedCocoonShakeOffset,
                SetSyncedCocoonShakeOffset,
                targetAngle,
                duration)
            .SetEase(Ease.InOutSine);
    }

    private static float GetSyncedCocoonShakeOffset()
    {
        return _syncedCocoonShakeZOffset;
    }

    private static void SetSyncedCocoonShakeOffset(float value)
    {
        _syncedCocoonShakeZOffset = value;
    }

    private static void StopSyncedCocoonShake()
    {
        if (_syncedCocoonShakeSequence != null)
        {
            _syncedCocoonShakeSequence.Kill();
            _syncedCocoonShakeSequence = null;
        }

        _syncedCocoonShakeZOffset = 0f;
    }
}
