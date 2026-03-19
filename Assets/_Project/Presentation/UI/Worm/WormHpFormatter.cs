public static class WormHpFormatter
{
    public static string Format(int value)
    {
        if (value < 10000)
            return value.ToString();

        if (value < 1000000)
            return (value / 1000f).ToString("0.#") + "K";

        return (value / 1000000f).ToString("0.#") + "M";
    }
}