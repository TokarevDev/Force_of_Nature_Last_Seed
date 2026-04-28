using UnityEngine;

public static class WormSectionHPGenerator
{
    public static int GetHP(int sectionIndex)
    {

        int[] early = { 16, 24, 40, 48, 80, 160 };

        if (sectionIndex < early.Length)
            return early[sectionIndex];

        float baseHP = 160f;
        float multiplier = 1.15f;

        int k = sectionIndex - (early.Length - 1);
        float hp = baseHP * Mathf.Pow(multiplier, k);

        return Mathf.RoundToInt(hp);
    }
}