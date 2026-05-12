using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Game/Rewards/UI/Icon Profile")]
public sealed class RewardIconProfile : ScriptableObject
{
    [SerializeField] private Sprite _sprite;
    [SerializeField] private Vector2 _anchoredPosition;
    [SerializeField] private Vector2 _sizeDelta = new(130f, 130f);
    [SerializeField] private Vector2 _scale = Vector2.one;
    [SerializeField] private float _rotationZ;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private bool _preserveAspect = true;

    public Sprite Sprite => _sprite;

    public void ApplyTo(Image image)
    {
        if (image == null)
            return;

        image.enabled = _sprite != null;

        if (_sprite == null)
            return;

        image.sprite = _sprite;
        image.color = _color;
        image.preserveAspect = _preserveAspect;

        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = _anchoredPosition;
        rect.sizeDelta = _sizeDelta;
        rect.localScale = new Vector3(_scale.x, _scale.y, 1f);
        rect.localRotation = Quaternion.Euler(0f, 0f, _rotationZ);
    }
}
