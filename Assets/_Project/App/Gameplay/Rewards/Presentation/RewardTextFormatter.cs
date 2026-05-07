using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RewardTextFormatter
{
    private static readonly List<HighlightRange> Ranges = new(8);
    private static readonly StringBuilder Builder = new(128);

    public static string HighlightAttempts(string text, Color32 numberColor)
    {
        return HighlightNumbers(text, numberColor);
    }

    public static string HighlightNumbers(string text, Color32 numberColor)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        Ranges.Clear();
        AddNumberRanges(text, ToHex(numberColor));
        return BuildHighlightedText(text);
    }

    public static string FormatRarityLine(
        string format,
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor)
    {
        string rarityText = GetRarityText(rarity);
        string color = ToHex(GetRarityColor(rarity, commonColor, rareColor, legendaryColor));
        return string.Format(format, $"<color=#{color}>{rarityText}</color>");
    }

    public static string FormatRarityLine(
        string format,
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor,
        Color32 numberColor)
    {
        const string rarityToken = "__RARITY__";

        string rarityText = GetRarityText(rarity);
        string rarityColor = ToHex(GetRarityColor(rarity, commonColor, rareColor, legendaryColor));
        string highlightedFormat = HighlightNumbers(format.Replace("{0}", rarityToken), numberColor);
        return highlightedFormat.Replace(
            rarityToken,
            $"<color=#{rarityColor}>{rarityText}</color>");
    }

    private static void AddNumberRanges(string text, string colorHex)
    {
        int i = 0;

        while (i < text.Length)
        {
            if (!IsNumberStart(text, i))
            {
                i++;
                continue;
            }

            int start = i;
            bool hasDigit = false;

            if (text[i] == '+' || text[i] == '-' || text[i] == 'x' || text[i] == 'X')
                i++;

            while (i < text.Length && IsNumberBody(text[i]))
            {
                if (char.IsDigit(text[i]))
                    hasDigit = true;

                i++;
            }

            if (hasDigit)
                TryAddRange(start, i - start, colorHex);
        }
    }

    private static bool IsNumberStart(string text, int index)
    {
        char c = text[index];

        if (char.IsDigit(c))
            return true;

        if ((c == '+' || c == '-' || c == 'x' || c == 'X')
            && index + 1 < text.Length
            && char.IsDigit(text[index + 1]))
        {
            return true;
        }

        return false;
    }

    private static bool IsNumberBody(char c)
    {
        return char.IsDigit(c)
            || c == '.'
            || c == ','
            || c == '/'
            || c == '%';
    }

    private static bool TryAddRange(int start, int length, string colorHex)
    {
        if (length <= 0)
            return false;

        int end = start + length;

        for (int i = 0; i < Ranges.Count; i++)
        {
            HighlightRange range = Ranges[i];
            int rangeEnd = range.Start + range.Length;

            if (start < rangeEnd && end > range.Start)
                return false;
        }

        Ranges.Add(new HighlightRange(start, length, colorHex));
        return true;
    }

    private static string BuildHighlightedText(string text)
    {
        if (Ranges.Count == 0)
            return text;

        Ranges.Sort(CompareRanges);
        Builder.Clear();

        int cursor = 0;

        for (int i = 0; i < Ranges.Count; i++)
        {
            HighlightRange range = Ranges[i];

            if (range.Start > cursor)
                Builder.Append(text, cursor, range.Start - cursor);

            Builder.Append("<color=#");
            Builder.Append(range.ColorHex);
            Builder.Append(">");
            Builder.Append(text, range.Start, range.Length);
            Builder.Append("</color>");

            cursor = range.Start + range.Length;
        }

        if (cursor < text.Length)
            Builder.Append(text, cursor, text.Length - cursor);

        return Builder.ToString();
    }

    private static int CompareRanges(HighlightRange left, HighlightRange right)
    {
        return left.Start.CompareTo(right.Start);
    }

    private static string GetRarityText(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return "Rare";
            case RewardRarity.Legendary:
                return "Legendary";
            default:
                return "Common";
        }
    }

    private static Color32 GetRarityColor(
        RewardRarity rarity,
        Color32 commonColor,
        Color32 rareColor,
        Color32 legendaryColor)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return rareColor;
            case RewardRarity.Legendary:
                return legendaryColor;
            default:
                return commonColor;
        }
    }

    private static string ToHex(Color32 color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private readonly struct HighlightRange
    {
        public HighlightRange(int start, int length, string colorHex)
        {
            Start = start;
            Length = length;
            ColorHex = colorHex;
        }

        public int Start { get; }
        public int Length { get; }
        public string ColorHex { get; }
    }
}
