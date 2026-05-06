using UnityEngine;

[DisallowMultipleComponent]
public abstract class PopupView : MonoBehaviour
{
    [SerializeField] private string _popupId;
    [SerializeField] private GameObject _root;

    public string PopupId => string.IsNullOrEmpty(_popupId) ? GetType().Name : _popupId;
    public bool IsVisible => ResolveRoot().activeSelf;

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    protected virtual void Reset()
    {
        _root = gameObject;
        _popupId = GetType().Name;
    }

    protected virtual void OnShown()
    {
    }

    protected virtual void OnHidden()
    {
    }

    private void SetVisible(bool visible)
    {
        GameObject root = ResolveRoot();

        if (root.activeSelf == visible)
            return;

        root.SetActive(visible);

        if (visible)
            OnShown();
        else
            OnHidden();
    }

    private GameObject ResolveRoot()
    {
        if (_root == null)
            _root = gameObject;

        return _root;
    }
}
