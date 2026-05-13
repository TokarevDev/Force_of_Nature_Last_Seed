using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardFlagWindTimeDriver : MonoBehaviour
{
    private static readonly int RewardFlagWindTimeId = Shader.PropertyToID("_RewardFlagWindTime");

    private void OnEnable()
    {
        UpdateShaderTime();
    }

    private void LateUpdate()
    {
        UpdateShaderTime();
    }

    private static void UpdateShaderTime()
    {
        Shader.SetGlobalFloat(RewardFlagWindTimeId, Time.unscaledTime);
    }
}
