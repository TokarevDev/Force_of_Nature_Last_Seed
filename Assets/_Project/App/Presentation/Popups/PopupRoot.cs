using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PopupRoot : MonoBehaviour
{
    [SerializeField] private PopupView[] _popups;
    [SerializeField] private bool _hidePopupsOnAwake = true;
    [SerializeField] private bool _pauseTimeWhileModalVisible = true;

    private readonly Dictionary<string, PopupView> _popupsById = new();

    private PopupView _activePopup;
    private float _timeScaleBeforeLock = 1f;
    private bool _hasTimeScaleLock;
    private bool _hasInputLock;

    private void Awake()
    {
        RegisterPopups();

        if (_hidePopupsOnAwake)
            HideAllPopups();
    }

    private void OnDestroy()
    {
        ReleaseGameplayLock();
    }

    public bool Show(string popupId)
    {
        if (string.IsNullOrEmpty(popupId))
            return false;

        if (!_popupsById.TryGetValue(popupId, out PopupView popup))
        {
            Debug.LogWarning($"PopupRoot: popup '{popupId}' is not registered.", this);
            return false;
        }

        Show(popup);
        return true;
    }

    public void Show(PopupView popup)
    {
        if (popup == null)
            return;

        if (_activePopup == popup && popup.IsVisible)
            return;

        if (_activePopup != null)
            _activePopup.Hide();

        LockGameplay();
        _activePopup = popup;
        _activePopup.Show();
    }

    public void HideActive(bool releaseGameplayLock = true)
    {
        if (_activePopup != null)
        {
            _activePopup.Hide();
            _activePopup = null;
        }

        if (releaseGameplayLock)
            ReleaseGameplayLock();
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

    private void RegisterPopups()
    {
        _popupsById.Clear();

        if (_popups == null)
            return;

        for (int i = 0; i < _popups.Length; i++)
        {
            PopupView popup = _popups[i];

            if (popup == null)
                continue;

            string popupId = popup.PopupId;

            if (string.IsNullOrEmpty(popupId))
                continue;

            if (_popupsById.ContainsKey(popupId))
            {
                Debug.LogWarning($"PopupRoot: duplicate popup id '{popupId}'.", popup);
                continue;
            }

            _popupsById.Add(popupId, popup);
        }
    }

    private void HideAllPopups()
    {
        if (_popups == null)
            return;

        for (int i = 0; i < _popups.Length; i++)
        {
            if (_popups[i] != null)
                _popups[i].Hide();
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

        _popups = GetComponentsInChildren<PopupView>(true);
    }
#endif
}
