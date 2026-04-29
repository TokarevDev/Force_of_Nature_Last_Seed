using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WormProgressPresenter : MonoBehaviour
{
    [SerializeField] private WormCombatController _wormCombat;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private string _format = "Progress: {0}%";

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (_wormCombat != null)
            _wormCombat.DestructionProgressChanged += UpdateProgress;

        int destroyed = _wormCombat != null ? _wormCombat.DestroyedProgressSegments : 0;
        int total = _wormCombat != null ? _wormCombat.TotalProgressSegments : 0;

        UpdateProgress(destroyed, total);
    }

    private void OnDisable()
    {
        if (_wormCombat != null)
            _wormCombat.DestructionProgressChanged -= UpdateProgress;
    }

    private void UpdateProgress(int destroyedSegments, int totalSegments)
    {
        if (_text == null)
            return;

        int progress = 0;

        if (totalSegments > 0)
        {
            float normalized = Mathf.Clamp01(destroyedSegments / (float)totalSegments);
            progress = Mathf.RoundToInt(normalized * 100f);
        }

        _text.SetText(_format, progress);
    }
}
