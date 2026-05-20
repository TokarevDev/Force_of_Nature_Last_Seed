using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PopupRoot : MonoBehaviour
{
    [SerializeField] private List<PopupView> _popups = new();
    [SerializeField] private bool _registerChildPopups = true;
    [SerializeField] private bool _hidePopupsOnAwake = true;
    [SerializeField] private bool _pauseTimeWhileModalVisible = true;

    private readonly Dictionary<string, PopupView> _popupsById = new();
    private readonly List<PopupView> _registeredPopups = new();
    private readonly List<PopupView> _queuedPopups = new();

    private PopupView _activePopup;
    private float _timeScaleBeforeLock = 1f;
    private bool _hasTimeScaleLock;
    private bool _hasInputLock;

    private void Awake()
    {
        RefreshRegistry();

        if (_hidePopupsOnAwake)
            HideAllPopups();
    }

    private void OnEnable()
    {
        PopupEvents.ShowRequested += HandleShowRequested;
        PopupEvents.HideActiveRequested += HandleHideActiveRequested;
    }

    private void OnDisable()
    {
        PopupEvents.ShowRequested -= HandleShowRequested;
        PopupEvents.HideActiveRequested -= HandleHideActiveRequested;
        _queuedPopups.Clear();
        HideActiveInternal(true, false);
    }

    private void OnDestroy()
    {
        UnregisterPopups();
        ReleaseGameplayLock();
    }

    public bool Show(string popupId)
    {
        if (string.IsNullOrEmpty(popupId))
            return false;

        if (!_popupsById.TryGetValue(popupId, out PopupView popup))
        {
            RefreshRegistry();

            if (!_popupsById.TryGetValue(popupId, out popup))
            {
                Debug.LogWarning($"PopupRoot: popup '{popupId}' is not registered.", this);
                return false;
            }
        }

        Show(popup);
        return true;
    }

    public void Show(PopupView popup)
    {
        if (popup == null)
            return;

        RegisterPopup(popup);

        if (_activePopup == popup && popup.IsVisible)
            return;

        if (_activePopup != null)
        {
            EnqueuePopup(popup);
            return;
        }

        ShowNow(popup);
    }

    public void HideActive(bool releaseGameplayLock = true)
    {
        _queuedPopups.Clear();
        HideActiveInternal(releaseGameplayLock, false);
    }

    private void HideActiveInternal(
        bool releaseGameplayLock,
        bool showQueuedPopup)
    {
        PopupView popup = _activePopup;

        if (popup != null)
        {
            _activePopup = null;
            popup.Hide();
        }

        if (_activePopup != null)
            return;

        if (releaseGameplayLock)
        {
            if (showQueuedPopup && TryShowNextQueuedPopup())
                return;

            ReleaseGameplayLock();
        }
    }

    public void ReleaseGameplayLock()
    {
        if (_hasInputLock)
        {
            GameplayInputBlocker.PopLock();
            _hasInputLock = false;
        }

        if (!_hasTimeScaleLock)
            return;

        Time.timeScale = _timeScaleBeforeLock;
        _hasTimeScaleLock = false;
    }

    public void RefreshRegistry()
    {
        UnregisterPopups();
        _popupsById.Clear();

        if (_popups != null)
        {
            for (int i = 0; i < _popups.Count; i++)
            {
                RegisterPopup(_popups[i]);
            }
        }

        if (!_registerChildPopups)
            return;

        PopupView[] childPopups = GetComponentsInChildren<PopupView>(true);

        for (int i = 0; i < childPopups.Length; i++)
        {
            RegisterPopup(childPopups[i]);
        }
    }

    private void RegisterPopup(PopupView popup)
    {
        if (popup == null)
            return;

        if (_registeredPopups.Contains(popup))
            return;

        popup.CloseRequested += HandlePopupCloseRequested;
        _registeredPopups.Add(popup);

        string popupId = popup.PopupId;

        if (string.IsNullOrEmpty(popupId))
            return;

        if (_popupsById.ContainsKey(popupId))
        {
            Debug.LogWarning($"PopupRoot: duplicate popup id '{popupId}'.", popup);
            return;
        }

        _popupsById.Add(popupId, popup);
    }

    private void UnregisterPopups()
    {
        for (int i = 0; i < _registeredPopups.Count; i++)
        {
            if (_registeredPopups[i] != null)
                _registeredPopups[i].CloseRequested -= HandlePopupCloseRequested;
        }

        _registeredPopups.Clear();
        _queuedPopups.Clear();
    }

    private void HandlePopupCloseRequested(PopupView popup)
    {
        if (popup == _activePopup)
        {
            HideActiveInternal(true, true);
            return;
        }

        if (_queuedPopups.Remove(popup))
            return;

        popup?.Hide();
    }

    private void HandleHideActiveRequested()
    {
        HideActive();
    }

    private void HandleShowRequested(string popupId)
    {
        Show(popupId);
    }

    private void EnqueuePopup(PopupView popup)
    {
        if (popup == null)
            return;

        if (_queuedPopups.Contains(popup))
            return;

        _queuedPopups.Add(popup);
    }

    private bool TryShowNextQueuedPopup()
    {
        while (_queuedPopups.Count > 0)
        {
            PopupView popup = _queuedPopups[0];
            _queuedPopups.RemoveAt(0);

            if (popup == null)
                continue;

            ShowNow(popup);
            return true;
        }

        return false;
    }

    private void ShowNow(PopupView popup)
    {
        if (popup == null)
            return;

        RegisterPopup(popup);
        _queuedPopups.Remove(popup);

        LockGameplay();
        _activePopup = popup;
        _activePopup.Show();
    }

    private void HideAllPopups()
    {
        _queuedPopups.Clear();

        for (int i = 0; i < _registeredPopups.Count; i++)
        {
            if (_registeredPopups[i] != null)
                _registeredPopups[i].Hide();
        }
    }

    private void LockGameplay()
    {
        if (!_hasInputLock)
        {
            GameplayInputBlocker.PushLock();
            _hasInputLock = true;
        }

        if (!_pauseTimeWhileModalVisible || _hasTimeScaleLock)
            return;

        _timeScaleBeforeLock = Time.timeScale;
        Time.timeScale = 0f;
        _hasTimeScaleLock = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (_popups == null)
            _popups = new List<PopupView>();

        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            if (_popups[i] == null)
                _popups.RemoveAt(i);
        }

        PopupView[] childPopups = GetComponentsInChildren<PopupView>(true);

        for (int i = 0; i < childPopups.Length; i++)
        {
            if (!_popups.Contains(childPopups[i]))
                _popups.Add(childPopups[i]);
        }
    }
#endif
}
