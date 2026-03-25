using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormDamagePopupPresenter : MonoBehaviour
{
    [SerializeField] private WormCombatController _combat;
    [SerializeField] private WormDamagePopupView _popupPrefab;
    [SerializeField] private int _initialPoolSize = 20;

    [SerializeField] private int _maxActivePopups = 50;

    private readonly Queue<WormDamagePopupView> _pool = new();

    private int _activeCount;

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
        if (_activeCount >= _maxActivePopups)
            return;

        var popup = GetFromPool();

        _activeCount++;

        popup.gameObject.SetActive(true);
        popup.Show(request, OnPopupComplete);
    }

    private void OnPopupComplete(WormDamagePopupView view)
    {
        _activeCount--;
        ReturnToPool(view);
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
            var popup = CreatePopup();
            popup.gameObject.SetActive(false);
            _pool.Enqueue(popup);
        }
    }

    private WormDamagePopupView CreatePopup()
    {
        return Instantiate(_popupPrefab, transform);
    }
}