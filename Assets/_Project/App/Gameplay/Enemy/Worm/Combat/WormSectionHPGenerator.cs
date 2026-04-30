public static class WormSectionHPGenerator
{
    private const int FallbackBaseHp = 1;

    public static int GetHP(int sectionIndex)
    {
        return GetHP(sectionIndex, 1);
    }

    public static int GetHP(int sectionIndex, int levelNumber)
    {
        return FallbackBaseHp;
    }
}
