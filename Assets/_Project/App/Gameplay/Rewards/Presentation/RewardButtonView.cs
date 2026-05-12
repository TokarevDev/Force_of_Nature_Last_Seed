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
    [SerializeField] private Color32 _weaponUnlockValueColor = new(218, 138, 255, 255);
    [SerializeField] private string _weaponUnlockValueFallback = "NEW";

    private RewardChoiceData _data;

    private event Action<RewardChoiceData> _onClick;

    public void Bind(
        RewardChoiceData data,
        RewardPresentationData presentation,
        Action<RewardChoiceData> onClick)
    {
        _data = data;
        _onClick = onClick;

        ApplyTextColors(presentation.Kind);
        SetText(_title, data.Title);
        SetText(_description, data.Description);
        SetValueText(data.ValueText, presentation.Kind);
        ApplyTargetIcon(presentation.IconProfile);

        ApplyCardVisual(data.Rarity, presentation.Kind);

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

    private void OnClick()
    {
        _onClick?.Invoke(_data);
    }
}
