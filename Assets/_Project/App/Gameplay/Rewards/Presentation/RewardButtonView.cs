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
    [SerializeField] private TMP_Text _description;

    private RewardChoiceData _data;

    private event Action<RewardChoiceData> _onClick;

    public void Bind(RewardChoiceData data, Action<RewardChoiceData> onClick)
    {
        _data = data;
        _onClick = onClick;

        if (_title != null)
            _title.text = data.Title;

        if (_description != null)
            _description.text = data.Description;

        if (_button != null)
            ApplyRarityColor(data.Rarity);

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    private void ApplyRarityColor(RewardRarity rarity)
    {
        Color color = RewardRarityPalette.GetColor(rarity);
        ColorBlock colors = _button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.disabledColor = Color.Lerp(color, Color.gray, 0.5f);
        _button.colors = colors;
    }

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}
