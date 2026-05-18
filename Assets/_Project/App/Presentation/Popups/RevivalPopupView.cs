using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RevivalPopupView : PopupView
{
    private const string AnimatedContentRootName = "AnimatedContentRoot";

    [SerializeField] private Button _reviveButton;
    [SerializeField] private Button _giveUpButton;
    [SerializeField] private RectTransform _animatedContentRoot;
    [SerializeField] private Slider _remainingSlider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _percentText;
    [SerializeField] private TMP_Text _attemptsText;
    [SerializeField] private string _progressFormat = "Only {0}% of the level remains.";
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";

    public event Action ReviveRequested;
    public event Action GiveUpRequested;

    private bool _canRevive;
    private readonly List<RectTransform> _contentChildren = new();
    private CanvasGroup _contentCanvasGroup;
    private Image _backgroundImage;
    private float _backgroundBaseAlpha = 1f;
    private Vector3 _contentBaseScale = Vector3.one;
    private Sequence _closeSequence;

    private void OnEnable()
    {
        EnsureAnimationRefs();

        if (_reviveButton != null)
            _reviveButton.onClick.AddListener(HandleReviveClicked);

        if (_giveUpButton != null)
            _giveUpButton.onClick.AddListener(HandleGiveUpClicked);
    }

    private void OnDisable()
    {
        _closeSequence?.Kill();
        _closeSequence = null;

        if (_reviveButton != null)
            _reviveButton.onClick.RemoveListener(HandleReviveClicked);

        if (_giveUpButton != null)
            _giveUpButton.onClick.RemoveListener(HandleGiveUpClicked);
    }

    protected override void OnShown()
    {
        ResetAnimationState();
    }

    public void Bind(
        int attemptsLeft,
        float currentProgressNormalized,
        float remainingLevelNormalized,
        bool canRevive)
    {
        int currentPercent = Mathf.RoundToInt(Mathf.Clamp01(currentProgressNormalized) * 100f);
        int remainingPercent = Mathf.RoundToInt(Mathf.Clamp01(remainingLevelNormalized) * 100f);
        _canRevive = canRevive && attemptsLeft > 0;

        if (_remainingSlider != null)
            _remainingSlider.SetValueWithoutNotify(currentPercent / 100f);

        if (_progressText != null)
            _progressText.SetText(_progressFormat, remainingPercent);

        if (_percentText != null)
            _percentText.SetText("{0}%", currentPercent);

        if (_attemptsText != null)
            _attemptsText.SetText(_attemptsFormat, Mathf.Max(0, attemptsLeft));

        SetWaitingForAd(false);
        ResetAnimationState();
    }

    public void SetWaitingForAd(bool isWaiting)
    {
        if (_reviveButton != null)
            _reviveButton.interactable = !isWaiting && _canRevive;

        if (_giveUpButton != null)
            _giveUpButton.interactable = !isWaiting;
    }

    private void HandleReviveClicked()
    {
        if (!_canRevive)
            return;

        ReviveRequested?.Invoke();
    }

    private void HandleGiveUpClicked()
    {
        GiveUpRequested?.Invoke();
    }

    public void PlayCloseAnimation(
        float duration,
        float targetScale,
        Action onComplete)
    {
        EnsureAnimationRefs();
        SetWaitingForAd(true);
        _closeSequence?.Kill();

        float safeDuration = Mathf.Max(0f, duration);
        float safeScale = Mathf.Clamp(targetScale, 0.5f, 1f);

        if (safeDuration <= 0f)
        {
            onComplete?.Invoke();
            return;
        }

        _closeSequence = DOTween.Sequence().SetUpdate(true);

        if (_animatedContentRoot != null)
        {
            _closeSequence.Join(
                _animatedContentRoot
                    .DOScale(_contentBaseScale * safeScale, safeDuration)
                    .SetEase(Ease.InOutSine));
        }

        if (_contentCanvasGroup != null)
        {
            _closeSequence.Join(
                _contentCanvasGroup
                    .DOFade(0f, safeDuration)
                    .SetEase(Ease.InSine));
        }

        if (_backgroundImage != null && _backgroundBaseAlpha > 0f)
        {
            _closeSequence.Join(
                _backgroundImage
                    .DOFade(0f, safeDuration)
                    .SetEase(Ease.InSine));
        }

        _closeSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void EnsureAnimationRefs()
    {
        if (_animatedContentRoot == null)
            _animatedContentRoot = CreateAnimatedContentRoot();

        if (_animatedContentRoot != null)
        {
            _contentBaseScale = _animatedContentRoot.localScale;

            if (!_animatedContentRoot.TryGetComponent(out _contentCanvasGroup))
                _contentCanvasGroup = _animatedContentRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (_backgroundImage == null && TryGetComponent(out _backgroundImage))
            _backgroundBaseAlpha = _backgroundImage.color.a;
    }

    private void ResetAnimationState()
    {
        EnsureAnimationRefs();
        _closeSequence?.Kill();
        _closeSequence = null;

        if (_animatedContentRoot != null)
            _animatedContentRoot.localScale = _contentBaseScale;

        if (_contentCanvasGroup != null)
            _contentCanvasGroup.alpha = 1f;

        if (_backgroundImage != null)
        {
            Color color = _backgroundImage.color;
            color.a = _backgroundBaseAlpha;
            _backgroundImage.color = color;
        }
    }

    private RectTransform CreateAnimatedContentRoot()
    {
        Transform root = transform;
        int childCount = root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child != null && child.name == AnimatedContentRootName)
                return child as RectTransform;
        }

        if (childCount <= 0)
            return transform as RectTransform;

        _contentChildren.Clear();

        for (int i = 0; i < childCount; i++)
        {
            if (root.GetChild(i) is RectTransform childRect)
                _contentChildren.Add(childRect);
        }

        GameObject contentObject = new(AnimatedContentRootName)
        {
            layer = gameObject.layer
        };
        RectTransform contentRoot = contentObject.AddComponent<RectTransform>();
        contentRoot.SetParent(root, false);
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.localScale = Vector3.one;
        contentRoot.SetAsLastSibling();

        for (int i = 0; i < _contentChildren.Count; i++)
            _contentChildren[i].SetParent(contentRoot, false);

        return contentRoot;
    }
}
