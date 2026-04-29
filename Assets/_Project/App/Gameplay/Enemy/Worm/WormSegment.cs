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

    [Header("Cocoon Overlay (Visual Only)")]
    [SerializeField] private GameObject _cocoonVisual;

    private Collider2D _cachedCollider;
    private SpriteRenderer _renderer;
    private SpriteRenderer _cocoonRenderer;

    private Transform _cocoonTransform;

    public Transform CachedTransform { get; private set; }
    public WormSection Section { get; internal set; }
    public int Index { get; set; }

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
            _cocoonRenderer = _cocoonVisual.GetComponentInChildren<SpriteRenderer>(true);
            _cocoonTransform = _cocoonVisual.transform;
        }
    }

    private void LateUpdate()
    {
        if (HasCocoon && _cocoonTransform != null)
        {
            _cocoonTransform.localEulerAngles =
                new Vector3(0f, 0f, -transform.eulerAngles.z);
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
    }

    public void DisableCocoon()
    {
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
}
