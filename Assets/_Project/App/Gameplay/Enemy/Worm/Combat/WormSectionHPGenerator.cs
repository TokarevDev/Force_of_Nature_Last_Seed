using UnityEngine;

public static class WormSectionHPGenerator
{
    private static readonly int[] EarlyLevelOneHp =
    {
        16,
        24,
        40,
        48,
        80,
        160
    };

    private const float LevelOneSectionGrowth = 1.08f;
    private const float PerLevelMultiplier = 1.43f;

    public static int GetHP(int sectionIndex)
    {
        return GetHP(sectionIndex, 1);
    }

    public static int GetHP(int sectionIndex, int levelNumber)
    {
        int safeSectionIndex = Mathf.Max(0, sectionIndex);
        int safeLevelNumber = Mathf.Max(1, levelNumber);

        int levelOneHp = GetLevelOneHp(safeSectionIndex);
        float levelMultiplier = Mathf.Pow(PerLevelMultiplier, safeLevelNumber - 1);
        float scaledHp = levelOneHp * levelMultiplier;

        if (scaledHp >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(1, Mathf.RoundToInt(scaledHp));
    }

    private static int GetLevelOneHp(int sectionIndex)
    {
        if (sectionIndex < EarlyLevelOneHp.Length)
            return EarlyLevelOneHp[sectionIndex];

        int growthStep = sectionIndex - (EarlyLevelOneHp.Length - 1);
        float hp = EarlyLevelOneHp[^1] * Mathf.Pow(LevelOneSectionGrowth, growthStep);

        return Mathf.RoundToInt(hp);
    }
}
