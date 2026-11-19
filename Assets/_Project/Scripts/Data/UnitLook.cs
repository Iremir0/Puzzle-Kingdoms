namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// Which silhouette the grey-box battle view draws for a unit (D-029). Replaced
    /// by real models or sprites once art exists; the values are stored in assets.
    /// </summary>
    public enum UnitLook
    {
        Generic = 0,
        Warrior = 1,
        Archer = 2,
        Mage = 3,
        GoblinGrunt = 4,
        GoblinArcher = 5,
        GoblinBrute = 6,
        GoblinChief = 7,
        // Chapter 2 (PB-18): drawn with a Chapter 1 silhouette until their own models exist.
        ShieldGoblin = 8,
        GoblinShaman = 9,
        WolfRider = 10,
        GoblinBomber = 11,
        // Player classes of PB-21.
        Defender = 12,
        Support = 13,
    }
}
