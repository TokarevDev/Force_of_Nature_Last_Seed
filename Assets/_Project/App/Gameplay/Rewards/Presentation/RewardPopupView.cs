using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

/// <summary>
/// Displays reward choices and handles selection.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardPopupView : PopupView
{
    [SerializeField] private List<RewardButtonView> _buttons;
    [SerializeField] private RewardVisualCatalog _visualCatalog;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _adRerollButton;
    [SerializeField] private Button _takeAllButton;

    [Header("Popup Animation")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform[] _topSlideGroups;
    [SerializeField] private float _rootFadeDuration = 0.12f;
    [SerializeField] private float _topEnterOffset = 160f;
    [SerializeField] private float _topEnterDuration = 0.28f;
    [SerializeField] private float _rewardEnterOffset = -230f;
    [SerializeField] private float _rewardEnterDuration = 0.32f;
    [SerializeField] private float _rewardEnterStagger = 0.045f;
    [SerializeField] private float _actionEnterOffset = -90f;
    [SerializeField] private float _actionEnterDuration = 0.16f;
    [SerializeField] private Ease _topEnterEase = Ease.OutCubic;
    [SerializeField] private Ease _rewardEnterEase = Ease.OutCubic;
    [SerializeField] private Ease _rewardScaleEase = Ease.OutBack;

    [Header("Reward Refresh Animation")]
    [SerializeField] private float _refreshCardStagger = 0.055f;
    [SerializeField] private float _refreshOutDuration = 0.12f;
    [SerializeField] private float _refreshInDuration = 0.22f;
    [SerializeField] private Ease _refreshOutEase = Ease.InCubic;
    [SerializeField] private Ease _refreshInEase = Ease.OutBack;

    [Header("Selection Dismiss Animation")]
    [SerializeField] private float _selectionFocusDuration = 0.22f;
    [SerializeField] private float _selectionGrowDuration = 0.16f;
    [SerializeField] private float _selectionExitDuration = 0.22f;
    [SerializeField] private float _selectionScaleMultiplier = 1.05f;
    [SerializeField] private float _selectionExitScaleMultiplier = 0.96f;
    [SerializeField] private float _selectionExitOffset = -230f;
    [SerializeField] private float _unselectedExitDuration = 0.22f;
    [SerializeField] private float _unselectedExitStagger = 0.045f;
    [SerializeField] private float _unselectedExitScaleMultiplier = 0.96f;
    [SerializeField] private float _unselectedExitOffset = -230f;
    [SerializeField] private float _topExitOffset = 160f;
    [SerializeField] private float _actionExitOffset = -90f;
    [SerializeField] private Ease _selectionFocusEase = Ease.OutBack;
    [SerializeField] private Ease _selectionExitEase = Ease.InCubic;
    [SerializeField] private Ease _unselectedExitEase = Ease.InCubic;

    [Header("Animation Audio")]
    [SerializeField] private AudioSource _animationAudioSource;
    [SerializeField] private AudioClip _showWhooshClip;
    [SerializeField] private AudioClip _showSettleClip;
    [SerializeField] private AudioClip _refreshClip;
    [SerializeField] private AudioClip _cardRevealClip;
    [SerializeField, Range(0f, 1f)] private float _animationVolume = 1f;

    [Header("Action State Text")]
    [SerializeField] private TMP_Text _rerollAttemptsText;
    [SerializeField] private TMP_Text _adRerollAttemptsText;
    [SerializeField] private TMP_Text _takeAllAttemptsText;
    [SerializeField] private TMP_Text _guaranteeText;
    [SerializeField] private TMP_Text _adRerollGuaranteeText;
    [SerializeField] private string _attemptsFormat = "attempts left: x{0}";
    [SerializeField] private string _guaranteeFormat = "guarantee: {0}";
    [SerializeField] private string _adGuaranteeFormat = "guarantee: {0}";

    [Header("Text Colors")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);
    [SerializeField] private Color32 _commonRarityColor = new(95, 220, 130, 255);
    [SerializeField] private Color32 _rareRarityColor = new(80, 180, 255, 255);
    [SerializeField] private Color32 _legendaryRarityColor = new(255, 155, 70, 255);

    public event Action<RewardChoiceData> Selected;
    public event Action RerollRequested;
    public event Action AdRerollRequested;
    public event Action TakeAllRequested;

    private RectTransformState[] _topGroupStates;
    private RectTransformState[] _actionButtonStates;
    private Sequence _showSequence;
    private Sequence _refreshSequence;
    private Sequence _dismissSequence;
    private Coroutine _interactionGateCoroutine;
    private RewardPopupState _currentState;
    private bool _hasCurrentState;
    private bool _hasBoundChoices;
    private bool _hasCachedAnimationState;
    private bool _isTransitioning;
    private bool _interactionGateOpen;

    private bool CanAcceptInteraction => _interactionGateOpen && !_isTransitioning;

    private void OnEnable()
    {
        SubscribeActionButtons();
    }

    private void OnDisable()
    {
        StopInteractionGate();
        KillAnimations();
        _isTransitioning = false;
        UnsubscribeActionButtons();
    }

    public bool Bind(
        List<RewardChoiceData> choices,
        RewardPopupState state,
        bool animateChoiceChanges = false)
    {
        if (choices == null || choices.Count == 0 || _buttons == null || _buttons.Count == 0)
        {
            Debug.LogWarning("RewardPopupView: reward choices or buttons are not assigned.", this);
            RequestClose();
            return false;
        }

        bool shouldAnimateRefresh = animateChoiceChanges && IsVisible && _hasBoundChoices;

        _currentState = state;
        _hasCurrentState = true;

        if (shouldAnimateRefresh)
        {
            PlayRewardRefresh(choices, state);
            return true;
        }

        ApplyChoices(choices, false);
        ApplyState(state, false);
        _hasBoundChoices = true;

        if (IsVisible)
            StartInteractionGateWhenSafe();

        return true;
    }

    public void Close()
    {
        RequestClose();
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        if (!interactable)
        {
            CloseInteractionGate();
            return;
        }

        StartInteractionGateWhenSafe();
    }

    protected override void OnShown()
    {
        PlayShowAnimation();
    }

    protected override void OnHidden()
    {
        CloseInteractionGate();
        KillAnimations();
        ResetAnimatedLayout();
        _isTransitioning = false;
        _hasBoundChoices = false;
    }

    private void OnClicked(RewardChoiceData data)
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        Selected?.Invoke(data);
        PlaySelectionDismiss(data);
    }

    private void OnRerollClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        RerollRequested?.Invoke();
    }

    private void OnTakeAllClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        TakeAllRequested?.Invoke();
    }

    private void OnAdRerollClicked()
    {
        if (!CanAcceptInteraction)
            return;

        CloseInteractionGate();
        AdRerollRequested?.Invoke();
    }

    private void PlayShowAnimation()
    {
        EnsureAnimationStateCached();
        BeginTransition();
        ResetAnimatedLayout();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        PrepareTopGroupsForEnter();
        PrepareRewardButtonsForEnter();
        PrepareActionButtonsForEnter();

        _showSequence = DOTween.Sequence().SetUpdate(true);
        _showSequence.InsertCallback(0f, () => PlayClip(_showWhooshClip));

        if (_canvasGroup != null)
            _showSequence.Insert(0f, _canvasGroup.DOFade(1f, _rootFadeDuration).SetEase(Ease.OutSine));

        InsertTopEnterTweens(_showSequence, 0.02f);
        InsertRewardEnterTweens(_showSequence, 0.1f);
        InsertActionEnterTweens(_showSequence, 0.3f);

        _showSequence.InsertCallback(0.28f, () => PlayClip(_showSettleClip));
        _showSequence.InsertCallback(0.14f, () => PlayClip(_cardRevealClip));
        _showSequence.OnComplete(CompleteTransitionAndStartGate);
    }

    private void PlayRewardRefresh(List<RewardChoiceData> choices, RewardPopupState state)
    {
        EnsureAnimationStateCached();
        BeginTransition();

        _refreshSequence?.Kill(false);
        _refreshSequence = DOTween.Sequence().SetUpdate(true);
        _refreshSequence.InsertCallback(0f, () => PlayClip(_refreshClip));

        float lastDelay = 0f;

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button == null)
                continue;

            if (i >= choices.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);

            float delay = i * _refreshCardStagger;
            lastDelay = delay;
            Tween tween = button.CreateRefreshTween(
                choices[i],
                GetPresentation(choices[i]),
                OnClicked,
                delay,
                _refreshOutDuration,
                _refreshInDuration,
                _refreshOutEase,
                _refreshInEase);

            _refreshSequence.Join(tween);
        }

        _refreshSequence.InsertCallback(
            lastDelay + _refreshOutDuration + 0.02f,
            () => PlayClip(_cardRevealClip));
        _refreshSequence.AppendCallback(() =>
        {
            ApplyState(state, false);
            _hasBoundChoices = true;
        });
        _refreshSequence.OnComplete(CompleteTransitionAndStartGate);
    }

    private void PlaySelectionDismiss(RewardChoiceData selectedChoice)
    {
        EnsureAnimationStateCached();
        BeginTransition();

        _showSequence?.Kill(false);
        _showSequence = null;

        _refreshSequence?.Kill(false);
        _refreshSequence = null;

        _dismissSequence?.Kill(false);
        _dismissSequence = null;

        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        RewardButtonView selectedButton = FindBoundButton(selectedChoice);

        if (selectedButton == null)
        {
            RequestClose();
            return;
        }

        _dismissSequence = DOTween.Sequence().SetUpdate(true);

        float actionExitStart = 0f;
        float rewardExitStart = Mathf.Max(0f, _actionEnterDuration * 0.5f);
        float selectedExitStart = InsertRewardDismissTweens(
            _dismissSequence,
            selectedButton,
            rewardExitStart);
        float topExitStart = selectedExitStart;

        InsertActionExitTweens(_dismissSequence, actionExitStart);
        InsertTopExitTweens(_dismissSequence, topExitStart);

        if (_canvasGroup != null)
        {
            float lastMotionEnd = Mathf.Max(
                selectedExitStart + _selectionExitDuration,
                topExitStart + _topEnterDuration);
            float rootFadeStart = Mathf.Max(0f, lastMotionEnd - _rootFadeDuration);
            _dismissSequence.Insert(rootFadeStart, _canvasGroup.DOFade(0f, _rootFadeDuration).SetEase(Ease.InSine));
        }

        _dismissSequence.OnComplete(() =>
        {
            _dismissSequence = null;
            RequestClose();
        });
    }

    private void BeginTransition()
    {
        _isTransitioning = true;
        CloseInteractionGate();
    }

    private void CompleteTransitionAndStartGate()
    {
        _isTransitioning = false;
        ResetAnimatedLayout();
        StartInteractionGateWhenSafe();
    }

    private void ApplyChoices(List<RewardChoiceData> choices, bool interactable)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button == null)
                continue;

            if (i >= choices.Count)
            {
                button.KillAnimations();
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);
            button.Bind(
                choices[i],
                GetPresentation(choices[i]),
                OnClicked,
                interactable);
            button.ResetAnimatedState();
        }
    }

    private RewardPresentationData GetPresentation(RewardChoiceData choice)
    {
        return _visualCatalog != null
            ? _visualCatalog.GetPresentation(choice.Category)
            : new RewardPresentationData(null, RewardPresentationKind.StatUpgrade);
    }

    private void SubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.AddListener(OnRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.AddListener(OnAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.AddListener(OnTakeAllClicked);
    }

    private void UnsubscribeActionButtons()
    {
        if (_rerollButton != null)
            _rerollButton.onClick.RemoveListener(OnRerollClicked);

        if (_adRerollButton != null)
            _adRerollButton.onClick.RemoveListener(OnAdRerollClicked);

        if (_takeAllButton != null)
            _takeAllButton.onClick.RemoveListener(OnTakeAllClicked);
    }

    private void ApplyState(RewardPopupState state, bool interactable)
    {
        _currentState = state;
        _hasCurrentState = true;

        ApplyRerollButtonMode(state);
        ApplyAttemptsText(_rerollAttemptsText, state.FreeRerollAttemptsLeft);
        ApplyAttemptsText(_adRerollAttemptsText, state.AdRerollAttemptsLeft);
        ApplyAttemptsText(_takeAllAttemptsText, state.TakeAllAttemptsLeft);

        ApplyGuaranteeText(_guaranteeText, state.GuaranteeRarity);
        ApplyAdGuaranteeText(_adRerollGuaranteeText, state.AdRerollGuaranteeRarity);

        SetInteractionEnabled(interactable);

        if (!_isTransitioning)
            ResetActionButtonsToBase();
    }

    private void ApplyRerollButtonMode(RewardPopupState state)
    {
        bool showFreeReroll = state.UseFreeRerollButton;

        if (_rerollButton != null)
            _rerollButton.gameObject.SetActive(showFreeReroll);

        if (_adRerollButton != null)
            _adRerollButton.gameObject.SetActive(!showFreeReroll);
    }

    private void ApplyGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
            _guaranteeFormat,
            rarity,
            _commonRarityColor,
            _rareRarityColor,
            _legendaryRarityColor);
    }

    private void ApplyAdGuaranteeText(TMP_Text text, RewardRarity rarity)
    {
        if (text == null)
            return;

        text.text = RewardTextFormatter.FormatRarityLine(
            _adGuaranteeFormat,
            rarity,
            _commonRarityColor,
            _rareRarityColor,
            _legendaryRarityColor,
            _numberColor);
    }

    private void ApplyAttemptsText(TMP_Text text, int attemptsLeft)
    {
        if (text == null)
            return;

        string value = string.Format(_attemptsFormat, Mathf.Max(0, attemptsLeft));
        text.text = RewardTextFormatter.HighlightAttempts(value, _numberColor);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        _interactionGateOpen = enabled;

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = enabled;
        }

        if (_buttons != null)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                    _buttons[i].SetInteractable(enabled);
            }
        }

        bool hasState = _hasCurrentState;

        if (_rerollButton != null)
            _rerollButton.interactable = enabled && hasState && _currentState.CanFreeReroll;

        if (_adRerollButton != null)
            _adRerollButton.interactable = enabled && hasState && _currentState.CanAdReroll;

        if (_takeAllButton != null)
            _takeAllButton.interactable = enabled && hasState && _currentState.CanTakeAll;
    }

    private void CloseInteractionGate()
    {
        StopInteractionGate();
        SetInteractionEnabled(false);
    }

    private void StartInteractionGateWhenSafe()
    {
        StopInteractionGate();

        if (!isActiveAndEnabled)
            return;

        _interactionGateCoroutine = StartCoroutine(WaitForPointerReleaseThenEnable());
    }

    private void StopInteractionGate()
    {
        if (_interactionGateCoroutine == null)
            return;

        StopCoroutine(_interactionGateCoroutine);
        _interactionGateCoroutine = null;
    }

    private IEnumerator WaitForPointerReleaseThenEnable()
    {
        yield return null;

        while (IsAnyPointerPressed())
            yield return null;

        yield return null;

        _interactionGateCoroutine = null;

        if (!isActiveAndEnabled || _isTransitioning)
            yield break;

        SetInteractionEnabled(true);
    }

    private void EnsureAnimationStateCached()
    {
        if (_hasCachedAnimationState)
            return;

        _topGroupStates = CreateStates(_topSlideGroups);
        _actionButtonStates = CreateActionButtonStates();
        _hasCachedAnimationState = true;
    }

    private RectTransformState[] CreateActionButtonStates()
    {
        RectTransform reroll = GetRectTransform(_rerollButton);
        RectTransform adReroll = GetRectTransform(_adRerollButton);
        RectTransform takeAll = GetRectTransform(_takeAllButton);

        int count = 0;

        if (reroll != null)
            count++;

        if (adReroll != null)
            count++;

        if (takeAll != null)
            count++;

        RectTransformState[] states = new RectTransformState[count];
        int index = 0;

        if (reroll != null)
            states[index++] = new RectTransformState(reroll);

        if (adReroll != null)
            states[index++] = new RectTransformState(adReroll);

        if (takeAll != null)
            states[index] = new RectTransformState(takeAll);

        return states;
    }

    private static RectTransformState[] CreateStates(RectTransform[] rects)
    {
        if (rects == null || rects.Length == 0)
            return Array.Empty<RectTransformState>();

        int count = 0;

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                count++;
        }

        RectTransformState[] states = new RectTransformState[count];
        int index = 0;

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                states[index++] = new RectTransformState(rects[i]);
        }

        return states;
    }

    private static RectTransform GetRectTransform(Button button)
    {
        return button != null
            ? button.transform as RectTransform
            : null;
    }

    private void PrepareTopGroupsForEnter()
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            _topGroupStates[i].Kill();
            _topGroupStates[i].Prepare(_topEnterOffset, 0.98f);
        }
    }

    private void PrepareRewardButtonsForEnter()
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null && _buttons[i].gameObject.activeSelf)
                _buttons[i].PrepareEnter(_rewardEnterOffset, 0.96f);
        }
    }

    private void PrepareActionButtonsForEnter()
    {
        for (int i = 0; i < _actionButtonStates.Length; i++)
        {
            _actionButtonStates[i].Kill();
            _actionButtonStates[i].Prepare(_actionEnterOffset, 0.98f);
        }
    }

    private void InsertTopEnterTweens(Sequence sequence, float startTime)
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                startTime,
                _topGroupStates[i].CreateEnterTween(
                    _topEnterDuration,
                    _topEnterEase,
                    Ease.OutBack));
        }
    }

    private void InsertRewardEnterTweens(Sequence sequence, float startTime)
    {
        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button == null || !button.gameObject.activeSelf)
                continue;

            sequence.Insert(
                startTime + i * _rewardEnterStagger,
                button.CreateEnterTween(
                    _rewardEnterDuration,
                    _rewardEnterEase,
                    _rewardScaleEase));
        }
    }

    private void InsertActionEnterTweens(Sequence sequence, float startTime)
    {
        int activeIndex = 0;

        for (int i = 0; i < _actionButtonStates.Length; i++)
        {
            if (!_actionButtonStates[i].IsActive)
                continue;

            sequence.Insert(
                startTime + activeIndex * 0.035f,
                _actionButtonStates[i].CreateEnterTween(
                    _actionEnterDuration,
                    Ease.OutCubic,
                    Ease.OutBack));
            activeIndex++;
        }
    }

    private float InsertRewardDismissTweens(
        Sequence sequence,
        RewardButtonView selectedButton,
        float startTime)
    {
        if (_buttons == null)
            return _selectionFocusDuration;

        int unselectedIndex = 0;
        float lastUnselectedExitEnd = startTime;

        for (int i = _buttons.Count - 1; i >= 0; i--)
        {
            RewardButtonView button = _buttons[i];

            if (button == null || !button.gameObject.activeSelf)
                continue;

            if (button == selectedButton)
                continue;

            float delay = startTime + unselectedIndex * _unselectedExitStagger;
            sequence.Insert(
                delay,
                button.CreateUnselectedDismissTween(
                    _unselectedExitDuration,
                    _unselectedExitOffset,
                    _unselectedExitScaleMultiplier,
                    _unselectedExitEase));
            lastUnselectedExitEnd = Mathf.Max(lastUnselectedExitEnd, delay + _unselectedExitDuration);
            unselectedIndex++;
        }

        float selectedExitStart = Mathf.Max(
            _selectionFocusDuration,
            lastUnselectedExitEnd + _unselectedExitStagger);

        sequence.Insert(
            0f,
            selectedButton.CreateSelectedDismissTween(
                selectedExitStart,
                _selectionGrowDuration,
                _selectionExitDuration,
                _selectionExitOffset,
                _selectionScaleMultiplier,
                _selectionExitScaleMultiplier,
                _selectionFocusEase,
                _selectionExitEase));

        return selectedExitStart;
    }

    private void InsertTopExitTweens(Sequence sequence, float startTime)
    {
        for (int i = 0; i < _topGroupStates.Length; i++)
        {
            sequence.Insert(
                startTime,
                _topGroupStates[i].CreateExitTween(
                    _topExitOffset,
                    0.98f,
                    _topEnterDuration,
                    _unselectedExitEase,
                    _unselectedExitEase));
        }
    }

    private void InsertActionExitTweens(Sequence sequence, float startTime)
    {
        int activeIndex = 0;

        for (int i = _actionButtonStates.Length - 1; i >= 0; i--)
        {
            if (!_actionButtonStates[i].IsActive)
                continue;

            sequence.Insert(
                startTime + activeIndex * 0.035f,
                _actionButtonStates[i].CreateExitTween(
                    _actionExitOffset,
                    0.98f,
                    _actionEnterDuration,
                    _unselectedExitEase,
                    _unselectedExitEase));
            activeIndex++;
        }
    }

    private RewardButtonView FindBoundButton(RewardChoiceData choice)
    {
        if (_buttons == null)
            return null;

        for (int i = 0; i < _buttons.Count; i++)
        {
            RewardButtonView button = _buttons[i];

            if (button != null && button.IsBoundTo(choice))
                return button;
        }

        return null;
    }

    private void ResetAnimatedLayout()
    {
        EnsureAnimationStateCached();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < _topGroupStates.Length; i++)
            _topGroupStates[i].Reset();

        ResetActionButtonsToBase();

        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].ResetAnimatedState();
        }
    }

    private void ResetActionButtonsToBase()
    {
        if (_actionButtonStates == null)
            return;

        for (int i = 0; i < _actionButtonStates.Length; i++)
            _actionButtonStates[i].Reset();
    }

    private void KillAnimations()
    {
        if (_canvasGroup != null)
            _canvasGroup.DOKill();

        _showSequence?.Kill(false);
        _showSequence = null;

        _refreshSequence?.Kill(false);
        _refreshSequence = null;

        _dismissSequence?.Kill(false);
        _dismissSequence = null;

        if (_topGroupStates != null)
        {
            for (int i = 0; i < _topGroupStates.Length; i++)
                _topGroupStates[i].Kill();
        }

        if (_actionButtonStates != null)
        {
            for (int i = 0; i < _actionButtonStates.Length; i++)
                _actionButtonStates[i].Kill();
        }

        if (_buttons == null)
            return;

        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
                _buttons[i].KillAnimations();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (_animationAudioSource == null || clip == null)
            return;

        _animationAudioSource.PlayOneShot(clip, _animationVolume);
    }

    private static bool IsAnyPointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null)
        {
            var touches = touchscreen.touches;

            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    return true;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.leftButton.isPressed)
            return true;

        Pen pen = Pen.current;

        if (pen != null && pen.tip.isPressed)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0 || Input.GetMouseButton(0))
            return true;
#endif

        return false;
    }

    private readonly struct RectTransformState
    {
        private readonly RectTransform _rectTransform;
        private readonly Vector2 _anchoredPosition;
        private readonly Vector3 _localScale;

        public RectTransformState(RectTransform rectTransform)
        {
            _rectTransform = rectTransform;
            _anchoredPosition = rectTransform != null
                ? rectTransform.anchoredPosition
                : Vector2.zero;
            _localScale = rectTransform != null
                ? rectTransform.localScale
                : Vector3.one;
        }

        public bool IsActive => _rectTransform != null && _rectTransform.gameObject.activeSelf;

        public void Prepare(float yOffset, float scaleMultiplier)
        {
            if (_rectTransform == null)
                return;

            _rectTransform.anchoredPosition = _anchoredPosition + new Vector2(0f, yOffset);
            _rectTransform.localScale = _localScale * scaleMultiplier;
        }

        public Tween CreateEnterTween(float duration, Ease moveEase, Ease scaleEase)
        {
            Sequence sequence = DOTween.Sequence();

            if (_rectTransform == null)
                return sequence;

            sequence.Join(_rectTransform.DOAnchorPos(_anchoredPosition, duration).SetEase(moveEase));
            sequence.Join(_rectTransform.DOScale(_localScale, duration).SetEase(scaleEase));
            return sequence;
        }

        public Tween CreateExitTween(
            float yOffset,
            float scaleMultiplier,
            float duration,
            Ease moveEase,
            Ease scaleEase)
        {
            Sequence sequence = DOTween.Sequence();

            if (_rectTransform == null)
                return sequence;

            sequence.Join(_rectTransform.DOAnchorPos(_anchoredPosition + new Vector2(0f, yOffset), duration).SetEase(moveEase));
            sequence.Join(_rectTransform.DOScale(_localScale * scaleMultiplier, duration).SetEase(scaleEase));
            return sequence;
        }

        public void Reset()
        {
            if (_rectTransform == null)
                return;

            Kill();
            _rectTransform.anchoredPosition = _anchoredPosition;
            _rectTransform.localScale = _localScale;
        }

        public void Kill()
        {
            if (_rectTransform != null)
                _rectTransform.DOKill();
        }
    }
}
