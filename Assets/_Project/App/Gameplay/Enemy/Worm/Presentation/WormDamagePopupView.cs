using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormDamagePopupView : MonoBehaviour
{
    public enum AnimationMode
    {
        Normal = 0,
        Fast = 1,
        Critical = 2
    }

    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _yOffset = 0.2f;
    [SerializeField] private Color _criticalColor = new Color(1f, 0.45f, 0.05f);
    [SerializeField, Min(1f)] private float _criticalScaleMultiplier = 1.3f;
    [SerializeField] private string _sortingLayerName = "UI";
    [SerializeField] private int _normalSortingOrder = 3000;
    [SerializeField] private int _criticalSortingOrder = 3200;

    private MeshRenderer _renderer;
    private Sequence _sequence;

    private Action<WormDamagePopupView> _onComplete;

    public bool IsCritical { get; private set; }
    public bool IsCompleting { get; private set; }
    public Vector3 SourceWorldPosition { get; private set; }
    public DamageKind Kind { get; private set; }

    private void Awake()
    {
        if (_text == null)
        {
            Debug.LogError("DamagePopupView: TMP_text not assigned", this);
            return;
        }

        _text.TryGetComponent(out _renderer);
        ApplySorting(false);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
        _onComplete = null;
        IsCompleting = false;
    }

    public void Show(
        DamageViewRequest request,
        AnimationMode animationMode,
        float scaleMultiplier,
        Action<WormDamagePopupView> onComplete)
    {
        _onComplete = onComplete;
        IsCritical = request.IsCritical || request.Kind == DamageKind.Critical;
        IsCompleting = false;
        SourceWorldPosition = request.WorldPosition;
        Kind = request.Kind;

        if (_text == null)
        {
            Debug.LogError("DamagePopupView: TMP_text not assigned", this);
            _onComplete?.Invoke(this);
            return;
        }

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-0.15f, 0.15f),
            _yOffset + UnityEngine.Random.Range(0f, 0.1f),
            0f
        );

        transform.position = request.WorldPosition + randomOffset;

        SetDamageText(request.Amount);
        _text.color = GetColor(request);
        ApplySorting(IsCritical);

        float baseScale = IsCritical ? _criticalScaleMultiplier : 1f;
        float targetScale = baseScale * Mathf.Clamp(scaleMultiplier, 0.1f, 1.5f);
        PlayAnimation(targetScale, animationMode);
    }

    public bool TryFastFade()
    {
        if (IsCritical || IsCompleting || _text == null || !gameObject.activeInHierarchy)
            return false;

        PlayFastFade();
        return true;
    }

    private void PlayAnimation(float targetScale, AnimationMode animationMode)
    {
        _sequence?.Kill();

        IsCompleting = false;
        _text.alpha = 1f;

        transform.localScale = Vector3.zero;

        _sequence = DOTween.Sequence();

        switch (animationMode)
        {
            case AnimationMode.Critical:
                AppendCriticalAnimation(targetScale);
                break;

            case AnimationMode.Fast:
                AppendFastAnimation(targetScale);
                break;

            default:
                AppendNormalAnimation(targetScale);
                break;
        }

        _sequence.OnComplete(OnAnimationComplete);
    }

    private void AppendNormalAnimation(float targetScale)
    {
        _sequence.Append(transform.DOScale(targetScale * 1.08f, 0.06f).SetEase(Ease.OutBack));
        _sequence.Append(transform.DOScale(targetScale, 0.08f).SetEase(Ease.OutSine));
        _sequence.AppendInterval(0.08f);
        _sequence.Append(_text.DOFade(0f, 0.14f));
    }

    private void AppendFastAnimation(float targetScale)
    {
        _sequence.Append(transform.DOScale(targetScale, 0.04f).SetEase(Ease.OutSine));
        _sequence.AppendInterval(0.04f);
        _sequence.Append(_text.DOFade(0f, 0.1f));
    }

    private void AppendCriticalAnimation(float targetScale)
    {
        _sequence.Append(transform.DOScale(targetScale * 1.2f, 0.07f).SetEase(Ease.OutBack));

        _sequence.Append(transform.DOPunchScale(
            Vector3.one * (targetScale * 0.4f),
            0.18f,
            vibrato: 6,
            elasticity: 0.6f
        ));

        _sequence.Append(transform.DOScale(targetScale, 0.08f));

        _sequence.AppendInterval(0.12f);

        _sequence.Append(_text.DOFade(0f, 0.2f));
    }

    private void PlayFastFade()
    {
        _sequence?.Kill();

        IsCompleting = true;
        _sequence = DOTween.Sequence();
        _sequence.Append(_text.DOFade(0f, 0.08f));
        _sequence.Join(transform.DOScale(transform.localScale * 0.85f, 0.08f));
        _sequence.OnComplete(OnAnimationComplete);
    }

    private void OnAnimationComplete()
    {
        _sequence = null;
        IsCompleting = false;

        Action<WormDamagePopupView> onComplete = _onComplete;
        _onComplete = null;
        onComplete?.Invoke(this);
    }

    private Color GetColor(DamageViewRequest request)
    {
        if (request.IsCritical || request.Kind == DamageKind.Critical)
            return _criticalColor;

        switch (request.Kind)
        {
            case DamageKind.DamageOverTime:
                return new Color(0.6f, 0.2f, 1f);

            default:
                return Color.white;
        }
    }

    private void SetDamageText(int amount)
    {
        _text.text = WormHpFormatter.Format(amount);
    }

    private void ApplySorting(bool isCritical)
    {
        if (_renderer == null)
            return;

        _renderer.sortingLayerName = _sortingLayerName;
        _renderer.sortingOrder = isCritical ? _criticalSortingOrder : _normalSortingOrder;
    }
}
