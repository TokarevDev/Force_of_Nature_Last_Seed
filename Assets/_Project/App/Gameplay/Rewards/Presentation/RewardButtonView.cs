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

    [Header("Rarity Popup Visuals")]
    [SerializeField] private Image _commonVisual;
    [SerializeField] private Image _rareVisual;
    [SerializeField] private Image _legendaryVisual;

    [Header("Rarity Button Sprites")]
    [SerializeField] private Image _buttonImage;
    [SerializeField] private Sprite _commonButtonSprite;
    [SerializeField] private Sprite _rareButtonSprite;
    [SerializeField] private Sprite _legendaryButtonSprite;

    [Header("Text Highlighting")]
    [SerializeField] private Color32 _numberColor = new(105, 255, 120, 255);

    private RewardChoiceData _data;

    private event Action<RewardChoiceData> _onClick;

    public void Bind(RewardChoiceData data, Action<RewardChoiceData> onClick)
    {
        _data = data;
        _onClick = onClick;

        if (_title != null)
            _title.text = data.Title;

        if (_description != null)
        {
            _description.text = RewardTextFormatter.HighlightNumbers(
                data.Description,
                _numberColor);
        }

        ApplyRarityVisual(data.Rarity);
        ApplyButtonSprite(data.Rarity);

        if (_button == null)
            return;

        _button.interactable = true;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    private void ApplyRarityVisual(RewardRarity rarity)
    {
        SetVisualActive(_commonVisual, rarity == RewardRarity.Common);
        SetVisualActive(_rareVisual, rarity == RewardRarity.Rare);
        SetVisualActive(_legendaryVisual, rarity == RewardRarity.Legendary);
    }

    private static void SetVisualActive(Image image, bool active)
    {
        if (image != null)
            image.gameObject.SetActive(active);
    }

    private void ApplyButtonSprite(RewardRarity rarity)
    {
        Image buttonImage = GetButtonImage();
        Sprite sprite = GetButtonSprite(rarity);

        if (buttonImage == null || sprite == null)
            return;

        buttonImage.sprite = sprite;
    }

    private Image GetButtonImage()
    {
        if (_buttonImage != null)
            return _buttonImage;

        if (_button == null)
            return null;

        _buttonImage = _button.targetGraphic as Image;
        return _buttonImage;
    }

    private Sprite GetButtonSprite(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return _rareButtonSprite;
            case RewardRarity.Legendary:
                return _legendaryButtonSprite;
            default:
                return _commonButtonSprite;
        }
    }

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}
