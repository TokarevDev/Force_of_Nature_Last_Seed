using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single reward button view.
/// Displays modifier info and notifies when selected.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _targetIcon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private TMP_Text _value;

    [Header("Rarity Popup Visuals")]
    [SerializeField] private Image _commonVisual;
    [SerializeField] private Image _rareVisual;
    [SerializeField] private Image _legendaryVisual;
    [SerializeField] private Image _weaponUnlockVisual;

    [Header("Text Highlighting")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);

    [Header("Text Colors")]
    [SerializeField] private Color32 _titleColor = new(255, 221, 75, 255);
    [SerializeField] private Color32 _descriptionColor = new(255, 255, 255, 255);
    [SerializeField] private Color32 _valueColor = new(255, 255, 255, 255);
    [SerializeField] private Color32 _weaponUnlockTitleColor = new(255, 232, 120, 255);
    [SerializeField] private Color32 _weaponUnlockDescriptionColor = new(238, 226, 255, 255);
    [SerializeField] private Color32 _weaponUnlockValueColor = new(105, 255, 120, 255);
    [SerializeField] private string _weaponUnlockValueFallback = "NEW";

    private RewardChoiceData _data;
    private RectTransform _rectTransform;
    private RectTransform _iconRectTransform;
    private Vector2 _baseAnchoredPosition;
    private Vector3 _baseScale;
    private Vector3 _baseIconScale;
    private bool _hasCachedTransformState;

    private event Action<RewardChoiceData> _onClick;

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            return _rectTransform;
        }
    }

    private void Awake()
    {
        CacheTransformState();
    }

    public void Bind(
        RewardChoiceData data,
        RewardPresentationData presentation,
        Action<RewardChoiceData> onClick,
        bool interactable = true)
    {
        CacheTransformState();

        _data = data;
        _onClick = onClick;

        ApplyTextColors(presentation.Kind);
        SetText(_title, data.Title);
        SetOptionalText(_description, data.Description);
        SetValueText(data.ValueText, presentation.Kind);
        ApplyTargetIcon(presentation.IconProfile);

        ApplyCardVisual(data.Rarity, presentation.Kind);

        if (_button == null)
            return;

        _button.interactable = interactable;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    public bool IsBoundTo(RewardChoiceData data)
    {
        return data != null && ReferenceEquals(_data, data);
    }

    public void KillAnimations()
    {
        if (RectTransform != null)
            RectTransform.DOKill();

        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        if (_iconRectTransform != null)
            _iconRectTransform.DOKill();
    }

    public void ResetAnimatedState()
    {
        CacheTransformState();
        KillAnimations();

        if (RectTransform != null)
        {
            RectTransform.anchoredPosition = _baseAnchoredPosition;
            RectTransform.localScale = _baseScale;
        }

        if (_iconRectTransform != null)
            _iconRectTransform.localScale = _baseIconScale;

        SetCanvasAlpha(1f);
    }

    public void PrepareEnter(float yOffset, float startScaleMultiplier)
    {
        CacheTransformState();
        KillAnimations();

        if (RectTransform != null)
        {
            RectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, yOffset);
            RectTransform.localScale = _baseScale * startScaleMultiplier;
        }

        if (_iconRectTransform != null)
            _iconRectTransform.localScale = _baseIconScale * 0.86f;

        SetCanvasAlpha(0f);
    }

    public Tween CreateEnterTween(float duration, Ease moveEase, Ease scaleEase)
    {
        CacheTransformState();

        Sequence sequence = DOTween.Sequence();

        if (RectTransform != null)
        {
            sequence.Join(RectTransform.DOAnchorPos(_baseAnchoredPosition, duration).SetEase(moveEase));
            sequence.Join(RectTransform.DOScale(_baseScale, duration).SetEase(scaleEase));
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(1f, duration * 0.72f).SetEase(Ease.OutSine));

        if (_iconRectTransform != null)
            sequence.Join(_iconRectTransform.DOScale(_baseIconScale, duration * 0.78f).SetEase(Ease.OutBack));

        return sequence;
    }

    public Tween CreateRefreshTween(
        RewardChoiceData data,
        RewardPresentationData presentation,
        Action<RewardChoiceData> onClick,
        float delay,
        float outDuration,
        float inDuration,
        Ease outEase,
        Ease inEase)
    {
        CacheTransformState();
        KillAnimations();
        SetInteractable(false);

        Sequence sequence = DOTween.Sequence();

        if (delay > 0f)
            sequence.AppendInterval(delay);

        if (RectTransform != null)
        {
            sequence.Append(
                RectTransform.DOShakeAnchorPos(
                    outDuration,
                    new Vector2(0f, 10f),
                    8,
                    45f,
                    false,
                    true));
            sequence.Join(RectTransform.DOScale(_baseScale * 0.96f, outDuration).SetEase(outEase));
        }
        else
        {
            sequence.AppendInterval(outDuration);
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, outDuration).SetEase(Ease.InSine));

        sequence.AppendCallback(() =>
        {
            Bind(data, presentation, onClick, false);
            SetCanvasAlpha(0f);

            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = _baseAnchoredPosition;
                RectTransform.localScale = _baseScale * 0.96f;
            }

            if (_iconRectTransform != null)
                _iconRectTransform.localScale = _baseIconScale * 0.62f;
        });

        if (RectTransform != null)
            sequence.Append(RectTransform.DOScale(_baseScale, inDuration).SetEase(inEase));
        else
            sequence.AppendInterval(inDuration);

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(1f, inDuration * 0.84f).SetEase(Ease.OutSine));

        if (_iconRectTransform != null)
            sequence.Join(_iconRectTransform.DOScale(_baseIconScale, inDuration).SetEase(Ease.OutBack));

        return sequence;
    }

    public Tween CreateSelectedDismissTween(
        float focusDuration,
        float growDuration,
        float exitDuration,
        float exitYOffset,
        float focusScaleMultiplier,
        float exitScaleMultiplier,
        Ease focusEase,
        Ease exitEase)
    {
        CacheTransformState();
        KillAnimations();
        SetInteractable(false);

        Sequence sequence = DOTween.Sequence();

        float clampedGrowDuration = Mathf.Max(0f, growDuration);
        float clampedFocusDuration = Mathf.Max(0f, focusDuration);

        if (RectTransform != null && clampedGrowDuration > 0f)
        {
            sequence.Append(RectTransform.DOScale(_baseScale * focusScaleMultiplier, clampedGrowDuration).SetEase(focusEase));

            if (_iconRectTransform != null)
                sequence.Join(_iconRectTransform.DOScale(_baseIconScale * focusScaleMultiplier, clampedGrowDuration).SetEase(focusEase));
        }
        else
        {
            sequence.AppendInterval(clampedGrowDuration);
        }

        float holdDuration = Mathf.Max(0f, clampedFocusDuration - clampedGrowDuration);

        if (holdDuration > 0f)
            sequence.AppendInterval(holdDuration);

        if (RectTransform != null)
        {
            sequence.Append(RectTransform.DOAnchorPos(_baseAnchoredPosition + new Vector2(0f, exitYOffset), exitDuration).SetEase(exitEase));
            sequence.Join(RectTransform.DOScale(_baseScale * exitScaleMultiplier, exitDuration).SetEase(exitEase));
        }
        else
        {
            sequence.AppendInterval(exitDuration);
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InSine));

        if (_iconRectTransform != null)
            sequence.Join(_iconRectTransform.DOScale(_baseIconScale * exitScaleMultiplier, exitDuration).SetEase(exitEase));

        return sequence;
    }

    public Tween CreateUnselectedDismissTween(
        float duration,
        float exitYOffset,
        float exitScaleMultiplier,
        Ease exitEase)
    {
        CacheTransformState();
        KillAnimations();
        SetInteractable(false);

        Sequence sequence = DOTween.Sequence();

        if (RectTransform != null)
        {
            sequence.Join(RectTransform.DOAnchorPos(_baseAnchoredPosition + new Vector2(0f, exitYOffset), duration).SetEase(exitEase));
            sequence.Join(RectTransform.DOScale(_baseScale * exitScaleMultiplier, duration).SetEase(exitEase));
        }
        else
        {
            sequence.AppendInterval(duration);
        }

        if (_canvasGroup != null)
            sequence.Join(_canvasGroup.DOFade(0f, duration).SetEase(Ease.InSine));

        if (_iconRectTransform != null)
            sequence.Join(_iconRectTransform.DOScale(_baseIconScale * exitScaleMultiplier, duration).SetEase(exitEase));

        return sequence;
    }

    private void ApplyCardVisual(
        RewardRarity rarity,
        RewardPresentationKind presentationKind)
    {
        bool isWeaponUnlock = presentationKind == RewardPresentationKind.WeaponUnlock;

        SetVisualActive(_commonVisual, !isWeaponUnlock && rarity == RewardRarity.Common);
        SetVisualActive(_rareVisual, !isWeaponUnlock && rarity == RewardRarity.Rare);
        SetVisualActive(_legendaryVisual, !isWeaponUnlock && rarity == RewardRarity.Legendary);
        SetVisualActive(_weaponUnlockVisual, isWeaponUnlock);
    }

    private void ApplyTextColors(RewardPresentationKind presentationKind)
    {
        bool isWeaponUnlock = presentationKind == RewardPresentationKind.WeaponUnlock;

        SetColor(_title, isWeaponUnlock ? _weaponUnlockTitleColor : _titleColor);
        SetColor(_description, isWeaponUnlock ? _weaponUnlockDescriptionColor : _descriptionColor);
        SetColor(_value, isWeaponUnlock ? _weaponUnlockValueColor : _valueColor);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void SetOptionalText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        text.gameObject.SetActive(hasValue);
        text.text = hasValue ? value : string.Empty;
    }

    private void SetValueText(string value, RewardPresentationKind presentationKind)
    {
        if (_value == null)
            return;

        if (presentationKind == RewardPresentationKind.WeaponUnlock)
        {
            _value.text = string.IsNullOrWhiteSpace(value)
                ? _weaponUnlockValueFallback
                : value;
            return;
        }

        _value.text = RewardTextFormatter.HighlightNumbers(value, _numberColor);
    }

    private void ApplyTargetIcon(RewardIconProfile iconProfile)
    {
        if (_targetIcon == null)
            return;

        if (iconProfile == null || iconProfile.Sprite == null)
        {
            _targetIcon.enabled = false;
            return;
        }

        iconProfile.ApplyTo(_targetIcon);

        if (_iconRectTransform == null)
            _iconRectTransform = _targetIcon.rectTransform;

        _baseIconScale = _iconRectTransform.localScale;
    }

    private static void SetColor(TMP_Text text, Color32 color)
    {
        if (text != null)
            text.color = color;
    }

    private static void SetVisualActive(Image image, bool active)
    {
        if (image != null)
            image.gameObject.SetActive(active);
    }

    private void CacheTransformState()
    {
        if (_hasCachedTransformState)
            return;

        if (RectTransform != null)
        {
            _baseAnchoredPosition = RectTransform.anchoredPosition;
            _baseScale = RectTransform.localScale;
        }

        if (_targetIcon != null)
        {
            _iconRectTransform = _targetIcon.rectTransform;
            _baseIconScale = _iconRectTransform.localScale;
        }
        else
        {
            _baseIconScale = Vector3.one;
        }

        _hasCachedTransformState = true;
    }

    private void SetCanvasAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}
