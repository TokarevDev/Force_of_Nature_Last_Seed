using System.Globalization;

public static class WormHpFormatter
{
    private const int Thousand = 1000;
    private const int TenThousand = 10000;
    private const int Million = 1000000;
    private const int TenMillion = 10000000;

    public static string Format(int value)
    {
        int safeValue = UnityEngine.Mathf.Max(0, value);

        if (safeValue < Thousand)
            return safeValue.ToString(CultureInfo.InvariantCulture);

        if (safeValue < TenThousand)
            return FormatSingleDecimal(safeValue / (float)Thousand) + "k";

        if (safeValue < Million)
            return (safeValue / Thousand).ToString(CultureInfo.InvariantCulture) + "k";

        if (safeValue < TenMillion)
            return FormatSingleDecimal(safeValue / (float)Million) + "m";

        return (safeValue / Million).ToString(CultureInfo.InvariantCulture) + "m";
    }

    private static string FormatSingleDecimal(float value)
    {
        float rounded = UnityEngine.Mathf.Round(value * 10f) * 0.1f;
        float capped = UnityEngine.Mathf.Min(9.9f, rounded);

        return capped.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
