using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class WinPopupView : PopupView
{
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _doubleRewardButton;
    [SerializeField] private bool _closeOnAccept = true;
    [SerializeField] private bool _closeOnDoubleReward = true;

    public event Action AcceptRequested;

    public event Action DoubleRewardRequested;

    private void OnEnable()
    {
        if (_acceptButton != null)
            _acceptButton.onClick.AddListener(HandleAcceptClicked);

        if (_doubleRewardButton != null)
            _doubleRewardButton.onClick.AddListener(HandleDoubleRewardClicked);
    }

    private void OnDisable()
    {
        if (_acceptButton != null)
            _acceptButton.onClick.RemoveListener(HandleAcceptClicked);

        if (_doubleRewardButton != null)
            _doubleRewardButton.onClick.RemoveListener(HandleDoubleRewardClicked);
    }

    private void HandleAcceptClicked()
    {
        AcceptRequested?.Invoke();

        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);

        if (_closeOnAccept)
            RequestClose();
    }

    private void HandleDoubleRewardClicked()
    {
        DoubleRewardRequested?.Invoke();

        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);

        if (_closeOnDoubleReward)
            RequestClose();
    }
}