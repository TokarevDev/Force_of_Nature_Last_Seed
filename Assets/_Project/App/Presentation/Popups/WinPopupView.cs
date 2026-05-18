using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WinPopupView : PopupView
{
    private const string AnimatedContentRootName = "AnimatedContentRoot";

    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _doubleRewardButton;
    [SerializeField] private RectTransform _animatedContentRoot;
    [SerializeField] private bool _closeOnAccept = true;
    [SerializeField] private bool _closeOnDoubleReward = true;
    [SerializeField, Min(0f)] private float _restartAnimationDuration = 0.55f;
    [SerializeField, Range(0.5f, 1f)] private float _restartAnimationTargetScale = 0.92f;

    public event Action AcceptRequested;

    public event Action DoubleRewardRequested;

    private bool _restartRequested;
    private readonly List<RectTransform> _contentChildren = new();
    private CanvasGroup _contentCanvasGroup;
    private Image _backgroundImage;
    private float _backgroundBaseAlpha = 1f;
    private Vector3 _contentBaseScale = Vector3.one;
    private Sequence _restartSequence;

    private void OnEnable()
    {
        EnsureAnimationRefs();

        if (_acceptButton != null)
            _acceptButton.onClick.AddListener(HandleAcceptClicked);

        if (_doubleRewardButton != null)
            _doubleRewardButton.onClick.AddListener(HandleDoubleRewardClicked);
    }

    private void OnDisable()
    {
        _restartSequence?.Kill();
        _restartSequence = null;

        if (_acceptButton != null)
            _acceptButton.onClick.RemoveListener(HandleAcceptClicked);

        if (_doubleRewardButton != null)
            _doubleRewardButton.onClick.RemoveListener(HandleDoubleRewardClicked);
    }

    protected override void OnShown()
    {
        _restartRequested = false;
        ResetAnimationState();
        SetButtonsInteractable(true);
    }

    private void HandleAcceptClicked()
    {
        AcceptRequested?.Invoke();

        RequestRunRestart(_closeOnAccept);
    }

    private void HandleDoubleRewardClicked()
    {
        DoubleRewardRequested?.Invoke();

        RequestRunRestart(_closeOnDoubleReward);
    }

    private void RequestRunRestart(bool closeOnComplete)
    {
        if (_restartRequested)
            return;

        _restartRequested = true;
        SetButtonsInteractable(false);
        PlayRestartAnimation(() =>
        {
            if (closeOnComplete)
                RequestClose();

            GameplayRunRestartEvents.RequestRestart();
        });
    }

    private void PlayRestartAnimation(Action onComplete)
    {
        EnsureAnimationRefs();
        _restartSequence?.Kill();

        float safeDuration = Mathf.Max(0f, _restartAnimationDuration);
        float safeScale = Mathf.Clamp(_restartAnimationTargetScale, 0.5f, 1f);

        if (safeDuration <= 0f)
        {
            onComplete?.Invoke();
            return;
        }

        _restartSequence = DOTween.Sequence().SetUpdate(true);

        if (_animatedContentRoot != null)
        {
            _restartSequence.Join(
                _animatedContentRoot
                    .DOScale(_contentBaseScale * safeScale, safeDuration)
                    .SetEase(Ease.InOutSine));
        }

        if (_contentCanvasGroup != null)
        {
            _restartSequence.Join(
                _contentCanvasGroup
                    .DOFade(0f, safeDuration)
                    .SetEase(Ease.InSine));
        }

        if (_backgroundImage != null && _backgroundBaseAlpha > 0f)
        {
            _restartSequence.Join(
                _backgroundImage
                    .DOFade(0f, safeDuration)
                    .SetEase(Ease.InSine));
        }

        _restartSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_acceptButton != null)
            _acceptButton.interactable = interactable;

        if (_doubleRewardButton != null)
            _doubleRewardButton.interactable = interactable;
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
        _restartSequence?.Kill();
        _restartSequence = null;

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
