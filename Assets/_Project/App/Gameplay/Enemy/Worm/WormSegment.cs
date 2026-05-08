using System;
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
    private SpriteRenderer[] _tailRenderers = Array.Empty<SpriteRenderer>();
    private Transform[] _tailVisualParts = Array.Empty<Transform>();
    private int[] _tailSortingOrderOffsets = Array.Empty<int>();
    private Vector3[] _tailRotationOffsets = Array.Empty<Vector3>();

    private Transform _cocoonTransform;
    private bool _usesSyncedCocoonShake;

    public Transform CachedTransform { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

    public bool HasCocoon { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public bool HasTailVisualChain => _tailVisualParts.Length > 1;
    public int TailVisualPartCount => _tailVisualParts.Length;

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

        if (Type == WormSegmentType.Tail)
            CacheTailVisualChain();
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
        if (HasTailVisualChain)
        {
            for (int i = 0; i < _tailRenderers.Length; i++)
            {
                if (_tailRenderers[i] != null)
                    _tailRenderers[i].sortingOrder = order + _tailSortingOrderOffsets[i];
            }
        }
        else if (_renderer != null)
        {
            _renderer.sortingOrder = order;
        }

        if (_cocoonRenderer != null)
            _cocoonRenderer.sortingOrder = order + 100;
    }

    public void ResetTailVisualRootRotation()
    {
        if (VisualRoot == null)
            return;

        if (VisualRoot.localRotation != Quaternion.identity)
            VisualRoot.localRotation = Quaternion.identity;
    }

    public void SetTailVisualPartPose(int index, Vector3 position, float angle)
    {
        if (index < 0 || index >= _tailVisualParts.Length)
            return;

        Transform part = _tailVisualParts[index];

        if (part == null)
            return;

        part.position = position;

        Vector3 rotationOffset = _tailRotationOffsets[index];
        part.rotation = Quaternion.Euler(
            rotationOffset.x,
            rotationOffset.y,
            angle + rotationOffset.z);
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

    private void CacheTailVisualChain()
    {
        if (VisualRoot == null)
        {
            ClearTailVisualChain();
            return;
        }

        SpriteRenderer[] allRenderers = VisualRoot.GetComponentsInChildren<SpriteRenderer>(true);

        if (allRenderers.Length == 0)
        {
            ClearTailVisualChain();
            return;
        }

        int rendererCount = CountVisualRenderers(allRenderers);

        if (rendererCount == 0)
        {
            ClearTailVisualChain();
            return;
        }

        _tailRenderers = new SpriteRenderer[rendererCount];
        _tailVisualParts = new Transform[rendererCount];
        _tailSortingOrderOffsets = new int[rendererCount];
        _tailRotationOffsets = new Vector3[rendererCount];

        int writeIndex = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer renderer = allRenderers[i];

            if (!IsVisualRenderer(renderer))
                continue;

            _tailRenderers[writeIndex++] = renderer;
        }

        Array.Sort(_tailRenderers, CompareTailRenderers);

        int anchorSortingOrder = _tailRenderers[0].sortingOrder;

        for (int i = 0; i < _tailRenderers.Length; i++)
        {
            SpriteRenderer renderer = _tailRenderers[i];
            Transform part = renderer.transform;

            _tailVisualParts[i] = part;
            _tailSortingOrderOffsets[i] = renderer.sortingOrder - anchorSortingOrder;
            _tailRotationOffsets[i] = part.localEulerAngles;
        }
    }

    private void ClearTailVisualChain()
    {
        _tailRenderers = Array.Empty<SpriteRenderer>();
        _tailVisualParts = Array.Empty<Transform>();
        _tailSortingOrderOffsets = Array.Empty<int>();
        _tailRotationOffsets = Array.Empty<Vector3>();
    }

    private int CountVisualRenderers(SpriteRenderer[] renderers)
    {
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsVisualRenderer(renderers[i]))
                count++;
        }

        return count;
    }

    private bool IsVisualRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
            return false;

        if (_cocoonTransform == null)
            return true;

        return !renderer.transform.IsChildOf(_cocoonTransform);
    }

    private static int CompareTailRenderers(SpriteRenderer left, SpriteRenderer right)
    {
        int sortingComparison = right.sortingOrder.CompareTo(left.sortingOrder);

        if (sortingComparison != 0)
            return sortingComparison;

        return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
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
