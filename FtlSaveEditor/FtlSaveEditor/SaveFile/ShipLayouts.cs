namespace FtlSaveEditor.SaveFile;

public static class ShipLayouts
{
    private static readonly Dictionary<string, int[]> Layouts = new()
    {
        // Kestrel A/B/C
        ["PLAYER_SHIP_HARD"] = [2, 2, 4, 1, 2, 2, 4, 2, 4, 2, 1, 4, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_HARD_2"] = [2, 2, 4, 1, 2, 2, 4, 2, 4, 2, 1, 4, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_HARD_3"] = [2, 2, 4, 1, 2, 2, 4, 2, 4, 2, 1, 4, 1, 1, 2, 1, 1],
        // Engi A/B/C
        ["PLAYER_SHIP_CIRCLE"] = [2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_CIRCLE_2"] = [2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_CIRCLE_3"] = [2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        // Federation A/B/C
        ["PLAYER_SHIP_FEDERATION"] = [4, 2, 2, 2, 2, 4, 2, 2, 4, 4, 2, 2, 1, 1, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_FEDERATION_2"] = [4, 2, 2, 2, 2, 4, 2, 2, 4, 4, 2, 2, 1, 1, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_FEDERATION_3"] = [4, 2, 2, 2, 2, 4, 2, 2, 4, 4, 2, 2, 1, 1, 1, 1, 2, 1, 1],
        // Zoltan A/B/C
        ["PLAYER_SHIP_ENERGY"] = [2, 4, 2, 4, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_ENERGY_2"] = [2, 4, 2, 4, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_ENERGY_3"] = [2, 4, 2, 4, 2, 2, 2, 2, 1, 1, 2, 2, 1, 1],
        // Mantis A/B/C
        ["PLAYER_SHIP_MANTIS"] = [4, 2, 4, 2, 2, 2, 4, 2, 4, 2, 1, 1, 2, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_MANTIS_2"] = [4, 2, 4, 2, 2, 2, 4, 2, 4, 2, 1, 1, 2, 1, 1, 2, 1, 1],
        ["PLAYER_SHIP_MANTIS_3"] = [4, 2, 4, 2, 2, 2, 4, 2, 4, 2, 1, 1, 2, 1, 1, 2, 1, 1],
        // Rock A/B/C
        ["PLAYER_SHIP_ROCK"] = [2, 2, 4, 2, 2, 2, 4, 2, 2, 2, 1, 4, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_ROCK_2"] = [2, 2, 4, 2, 2, 2, 4, 2, 2, 2, 1, 4, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_ROCK_3"] = [2, 2, 4, 2, 2, 2, 4, 2, 2, 2, 1, 4, 1, 1, 2, 2, 1, 1],
        // Slug A/B/C
        ["PLAYER_SHIP_JELLY"] = [2, 2, 4, 2, 2, 4, 2, 4, 2, 1, 2, 2, 1, 1, 1, 1, 1],
        ["PLAYER_SHIP_JELLY_2"] = [2, 2, 4, 2, 2, 4, 2, 4, 2, 1, 2, 2, 1, 1, 1, 1, 1],
        ["PLAYER_SHIP_JELLY_3"] = [2, 2, 4, 2, 2, 4, 2, 4, 2, 1, 2, 2, 1, 1, 1, 1, 1],
        // Stealth A/B/C
        ["PLAYER_SHIP_STEALTH"] = [4, 2, 2, 2, 2, 2, 4, 2, 2, 4, 1, 1, 2, 1],
        ["PLAYER_SHIP_STEALTH_2"] = [4, 2, 2, 2, 2, 2, 4, 2, 2, 4, 1, 1, 2, 1],
        ["PLAYER_SHIP_STEALTH_3"] = [4, 2, 2, 2, 2, 2, 4, 2, 2, 4, 1, 1, 2, 1],
        // Crystal A/B
        ["PLAYER_SHIP_CRYSTAL"] = [2, 2, 4, 2, 4, 2, 2, 2, 2, 4, 1, 1, 2, 2, 1, 1],
        ["PLAYER_SHIP_CRYSTAL_2"] = [2, 2, 4, 2, 4, 2, 2, 2, 2, 4, 1, 1, 2, 2, 1, 1],
        // Lanius A/B
        ["PLAYER_SHIP_ANAEROBIC"] = [2, 2, 4, 2, 2, 2, 4, 2, 4, 2, 1, 2, 2, 1, 1, 1, 1],
        ["PLAYER_SHIP_ANAEROBIC_2"] = [2, 2, 4, 2, 2, 2, 4, 2, 4, 2, 1, 2, 2, 1, 1, 1, 1],
    };

    public static int[]? LookupShipLayout(string blueprintId)
    {
        return Layouts.TryGetValue(blueprintId, out var layout)
            ? (int[])layout.Clone()
            : null;
    }
}
