using UnityEngine;

[DisallowMultipleComponent]
public sealed class CocoonLegendaryLightningEffect : MonoBehaviour
{
    private const int DefaultLineCount = 3;
    private const int DefaultPointCount = 4;

    private static Material _sharedLineMaterial;

    [SerializeField, Min(1)] private int _lineCount = DefaultLineCount;
    [SerializeField, Min(2)] private int _pointCount = DefaultPointCount;
    [SerializeField, Min(0.01f)] private float _radius = 0.38f;
    [SerializeField, Min(0f)] private float _radiusJitter = 0.08f;
    [SerializeField, Min(0.01f)] private float _arcAngle = 0.9f;
    [SerializeField, Min(0.01f)] private float _refreshInterval = 0.13f;
    [SerializeField, Min(0.001f)] private float _lineWidth = 0.018f;
    [SerializeField] private Color _color = new(1f, 0.58f, 0.08f, 0.85f);

    private LineState[] _lines;
    private float _refreshTimer;
    private int _sortingLayerId;
    private int _sortingOrder;
    private bool _hasSorting;

    private static Material SharedLineMaterial
    {
        get
        {
            if (_sharedLineMaterial != null)
                return _sharedLineMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            _sharedLineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.DontSave,
                name = "Cocoon Legendary Lightning Runtime"
            };

            return _sharedLineMaterial;
        }
    }

    private void OnEnable()
    {
        EnsureLines();
        RefreshLines();
        _refreshTimer = 0f;
    }

    private void OnDisable()
    {
        SetLinesVisible(false);
    }

    private void Update()
    {
        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer > 0f)
            return;

        RefreshLines();
        _refreshTimer = _refreshInterval;
    }

    public void Configure(Color color)
    {
        _color = color;

        if (_lines != null)
            ApplyColor();
    }

    public void SetActive(bool active)
    {
        if (gameObject.activeSelf != active)
            gameObject.SetActive(active);

        enabled = active;
    }

    public void SetSorting(int sortingLayerId, int sortingOrder)
    {
        _hasSorting = true;
        _sortingLayerId = sortingLayerId;
        _sortingOrder = sortingOrder;

        if (_lines == null)
            return;

        for (int i = 0; i < _lines.Length; i++)
        {
            LineRenderer lineRenderer = _lines[i].Renderer;

            if (lineRenderer == null)
                continue;

            lineRenderer.sortingLayerID = _sortingLayerId;
            lineRenderer.sortingOrder = _sortingOrder + i;
        }
    }

    private void EnsureLines()
    {
        if (_lines != null && _lines.Length == _lineCount)
            return;

        _lineCount = Mathf.Max(1, _lineCount);
        _pointCount = Mathf.Max(2, _pointCount);
        _lines = new LineState[_lineCount];

        for (int i = 0; i < _lineCount; i++)
        {
            GameObject lineObject = new($"LightningArc_{i + 1}");
            lineObject.transform.SetParent(transform, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.positionCount = _pointCount;
            lineRenderer.widthMultiplier = _lineWidth;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.material = SharedLineMaterial;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.alignment = LineAlignment.View;

            _lines[i] = new LineState(lineRenderer, new Vector3[_pointCount]);
        }

        ApplyColor();

        if (_hasSorting)
            SetSorting(_sortingLayerId, _sortingOrder);
    }

    private void RefreshLines()
    {
        EnsureLines();
        SetLinesVisible(true);

        for (int i = 0; i < _lines.Length; i++)
        {
            LineState state = _lines[i];
            float centerAngle = Random.Range(0f, Mathf.PI * 2f);
            float startAngle = centerAngle - _arcAngle * 0.5f;
            float alpha = Random.Range(0.45f, 0.95f);

            for (int pointIndex = 0; pointIndex < state.Points.Length; pointIndex++)
            {
                float t = pointIndex / (float)(state.Points.Length - 1);
                float angle = startAngle + _arcAngle * t + Random.Range(-0.14f, 0.14f);
                float radius = _radius + Random.Range(-_radiusJitter, _radiusJitter);
                state.Points[pointIndex] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 1.15f,
                    0f);
            }

            Color lineColor = _color;
            lineColor.a *= alpha;
            state.Renderer.startColor = lineColor;
            state.Renderer.endColor = lineColor;
            state.Renderer.SetPositions(state.Points);
        }
    }

    private void ApplyColor()
    {
        if (_lines == null)
            return;

        for (int i = 0; i < _lines.Length; i++)
        {
            LineRenderer lineRenderer = _lines[i].Renderer;

            if (lineRenderer == null)
                continue;

            lineRenderer.startColor = _color;
            lineRenderer.endColor = _color;
        }
    }

    private void SetLinesVisible(bool visible)
    {
        if (_lines == null)
            return;

        for (int i = 0; i < _lines.Length; i++)
        {
            LineRenderer lineRenderer = _lines[i].Renderer;

            if (lineRenderer != null && lineRenderer.enabled != visible)
                lineRenderer.enabled = visible;
        }
    }

    private sealed class LineState
    {
        public LineState(LineRenderer renderer, Vector3[] points)
        {
            Renderer = renderer;
            Points = points;
        }

        public LineRenderer Renderer { get; }
        public Vector3[] Points { get; }
    }
}
