using TMPro;
using UnityEngine;

public sealed class WormSectionHpView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Transform _visualRoot;

    [SerializeField] private float _minScale = 0.9f;
    [SerializeField] private float _maxScale = 1f;

    private Transform _target;

    private void Awake()
    {
        var meshRenderer = _text.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found on TMP_Text", this);
            return;
        }

        meshRenderer.sortingLayerName = "UI";
        meshRenderer.sortingOrder = 2600;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        transform.position = _target.position;
    }

    public void Bind(Transform target)
    {
        _target = target;
    }

    public void SetValue(int current)
    {
        _text.text = WormHpFormatter.Format(current);

        float t = Mathf.InverseLerp(0, 10000, current);
        float scale = Mathf.Lerp(_maxScale, _minScale, t);

        if (_visualRoot != null)
            _visualRoot.localScale = Vector3.one * scale;
    }
}