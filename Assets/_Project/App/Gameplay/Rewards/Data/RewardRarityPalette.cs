using UnityEngine;

public static class RewardRarityPalette
{
    public static Color GetColor(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Rare:
                return new Color(0.22f, 0.48f, 1f);
            case RewardRarity.Legendary:
                return new Color(1f, 0.52f, 0.12f);
            default:
                return new Color(0.23f, 0.72f, 0.28f);
        }
    }
}
