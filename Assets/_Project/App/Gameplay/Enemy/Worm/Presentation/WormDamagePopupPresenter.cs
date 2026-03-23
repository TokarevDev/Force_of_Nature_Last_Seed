using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormDamagePopupPresenter : MonoBehaviour
{
    [SerializeField] private WormCombatController _combat;
    [SerializeField] private WormDamagePopupView _popupPrefab;
    [SerializeField] private int _initialPoolSize = 20;

    private readonly Queue<WormDamagePopupView> _pool = new();

    private void Awake()
    {
        if (_combat == null)
            Debug.LogError("DamagePopupPresenter: Combat not assigned", this);

        if (_popupPrefab == null)
            Debug.LogError("DamagePopupPresenter: Popup prefab not assigned", this);

        CreatePool();
    }

    private void OnEnable()
    {
        if (_combat != null)
            _combat.DamageDealt += OnDamageDealt;
    }

    private void OnDisable()
    {
        if (_combat != null)
            _combat.DamageDealt -= OnDamageDealt;
    }

    private void OnDamageDealt(DamageViewRequest request)
    {
        var popup = GetFromPool();
        popup.gameObject.SetActive(true);
        popup.Show(request, ReturnToPool);
    }

    private WormDamagePopupView GetFromPool()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        return CreatePopup();
    }

    private void ReturnToPool(WormDamagePopupView view)
    {
        view.gameObject.SetActive(false);
        _pool.Enqueue(view);
    }

    private void CreatePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            var popup = GetFromPool();
            popup.gameObject.SetActive(true);
            _pool.Enqueue(popup);
        }
    }

    private WormDamagePopupView CreatePopup()
    {
        var popup = Instantiate(_popupPrefab, transform);
        return popup;
    }
}