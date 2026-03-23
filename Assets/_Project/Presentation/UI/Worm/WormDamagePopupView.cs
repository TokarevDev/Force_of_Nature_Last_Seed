using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormDamagePopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _duration = 0.6f;

    private Sequence _sequence;

    private Action<WormDamagePopupView> _onComplete;

    private void Awake()
    {
        var renderer = _text.GetComponent<MeshRenderer>();

        renderer.sortingLayerName = "UI";
        renderer.sortingOrder = 3000;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
    }

    public void Show(DamageViewRequest request, Action<WormDamagePopupView> onComplete)
    {
        _onComplete = onComplete;

        if (_text == null)
        {
            Debug.LogError("DamagePopupView: TMP_text not assigned", this);
            return;
        }

        transform.position = request.WorldPosition;

        _text.text = request.Amount.ToString();
        _text.color = GetColor(request);

        transform.localScale = request.IsCritical ? Vector3.one * 1.3f : Vector3.one;

        PlayAnimation();
    }

    private void PlayAnimation()
    {
        _sequence?.Kill();

        _text.alpha = 1f;

        transform.localScale = Vector3.zero;

        _sequence = DOTween.Sequence();

        _sequence.Append(transform.DOScale(1.2f, 0.08f).SetEase(Ease.OutBack));

        _sequence.Append(transform.DOPunchScale(
            Vector3.one * 0.4f,
            0.25f,
            vibrato: 6,
            elasticity: 0.6f
        ));

        _sequence.Append(transform.DOScale(1f, 0.1f));

        _sequence.AppendInterval(0.1f);

        _sequence.Append(_text.DOFade(0f, 0.2f));

        _sequence.OnComplete(OnAnimationComplete);
    }

    private void OnAnimationComplete()
    {
        _onComplete?.Invoke(this);
    }

    private Color GetColor(DamageViewRequest request)
    {
        switch (request.Kind)
        {
            case DamageKind.Critical:
                return Color.yellow;

            case DamageKind.DamageOverTime:
                return new Color(0.6f, 0.2f, 1f);

            default:
                return Color.white;
        }
    }
}