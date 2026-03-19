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
    [field: SerializeField] public WormSegmentType Type { get; private set; }
    [field: SerializeField] public Transform VisualRoot { get; private set; }

    [Header("Cocoon Overlay")]
    [SerializeField] private GameObject _cocoonVisual;

    [SerializeField] private Collider2D _cocoonCollider;

    [SerializeField] private bool _hasReward = false;

    private Collider2D _cachedCollider;
    private SpriteRenderer _renderer;
    private SpriteRenderer _cocoonRenderer;

    private Transform _cocoonTransform;

    public Transform CachedTransform { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

    public bool HasReward => _hasReward;
    public bool HasCocoon { get; private set; }
    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        CachedTransform = transform;
        _cachedCollider = GetComponent<Collider2D>();

        if (VisualRoot != null)
            _renderer = VisualRoot.GetComponentInChildren<SpriteRenderer>();

        if (_cocoonVisual != null)
        {
            _cocoonRenderer = _cocoonVisual.GetComponentInChildren<SpriteRenderer>();
            _cocoonTransform = _cocoonVisual.transform;
        }
    }

    /// <summary>
    /// Keeps the cocoon visually upright regardless of segment rotation.
    /// The worm body rotates along the movement path, but cocoon overlays
    /// should remain vertically aligned for readability.
    /// </summary>
    private void LateUpdate()
    {
        if (HasCocoon && _cocoonTransform != null)
        {
            _cocoonTransform.localEulerAngles = new Vector3(0f, 0f, -transform.eulerAngles.z);
        }
    }

    public void SetSortingOrder(int order)
    {
        if (_renderer != null)
            _renderer.sortingOrder = order;

        if (_cocoonRenderer != null)
            _cocoonRenderer.sortingOrder = order + 100;
    }

    public void SetHasReward(bool value)
    {
        _hasReward = value;
    }

    /// <summary>
    /// Activates cocoon overlay visuals and collider.
    /// Only valid for body segments.
    /// </summary>
    public void EnableCocoon()
    {
        if (Type != WormSegmentType.Body)
            return;

        HasCocoon = true;

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(true);

        if (_cocoonCollider != null)
            _cocoonCollider.enabled = true;
    }

    public void DisableCocoon()
    {
        HasCocoon = false;

        if (_cocoonVisual != null)
            _cocoonVisual.SetActive(false);

        if (_cocoonCollider != null)
            _cocoonCollider.enabled = false;
    }

    // Resets pooled segment state before reuse.
    public void Activate()
    {
        gameObject.SetActive(true);
        IsAlive = true;

        if (VisualRoot != null && !VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(true);

        if (_cachedCollider != null && !_cachedCollider.enabled)
            _cachedCollider.enabled = true;

        DisableCocoon();
        _hasReward = false;
        Section = null;
    }

    /// <summary>
    /// Disables rendering and collision without destroying the object.
    /// Used when segments are removed from the worm chain.
    /// </summary>
    public void KillVisualAndCollision()
    {
        IsAlive = false;

        if (VisualRoot != null && VisualRoot.gameObject.activeSelf)
            VisualRoot.gameObject.SetActive(false);

        if (_cachedCollider != null && _cachedCollider.enabled)
            _cachedCollider.enabled = false;

        DisableCocoon();
    }
}