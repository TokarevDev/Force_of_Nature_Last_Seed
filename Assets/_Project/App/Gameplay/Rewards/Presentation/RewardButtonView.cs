using System;
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
    [SerializeField] private TMP_Text _title;

    private RewardChoiceData _data;

    private event Action<RewardChoiceData> _onClick;

    public void Bind(RewardChoiceData data, Action<RewardChoiceData> onClick)
    {
        _data = data;
        _onClick = onClick;

        if (_title != null)
            _title.text = data.Modifier.name;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}