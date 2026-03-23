using System.Collections.Generic;
using UnityEngine;

public sealed class WormSectionHpPresenter : MonoBehaviour
{
    [SerializeField] private WormSectionHpView _viewPrefab;
    [SerializeField] private Transform _root;

    private readonly Dictionary<WormSection, WormSectionHpView> _views = new();

    public void BindSections(List<WormSection> sections)
    {
        if (_views.Count > 0)
            return;

        for (int i = 0; i < sections.Count; i++)
        {
            BindSection(sections[i]);
        }
    }

    private void BindSection(WormSection section)
    {
        if (section == null) return;

        WormSectionHpView view = Instantiate(_viewPrefab, _root);

        view.Bind(section.GetHpAnchor());
        view.SetValue(section.CurrentHP);

        section.HPChanged += OnHpChanged;
        section.Destroyed += OnSectionDestroyed;

        _views.Add(section, view);
    }

    private void OnHpChanged(WormSection section)
    {
        if (!_views.TryGetValue(section, out var view)) return;

        view.SetValue(section.CurrentHP);
    }

    private void OnSectionDestroyed(WormSection section)
    {
        if (!_views.TryGetValue(section, out var view)) return;

        Destroy(view.gameObject);

        section.HPChanged -= OnHpChanged;
        section.Destroyed -= OnSectionDestroyed;

        _views.Remove(section);
    }
}