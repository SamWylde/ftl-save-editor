namespace FtlSaveEditor.Data;

/// <summary>
/// Vanilla + Advanced Edition blueprint IDs for weapons, drones, and augments.
/// Modded IDs (Multiverse, Hyperspace) are not included — the editors dynamically
/// merge in any IDs found in the currently loaded save file, and users can always
/// type custom IDs for modded items.
/// </summary>
public static class ItemIds
{
    public static readonly string[] Weapons =
    [
        // Lasers
        "LASER_BURST_1",
        "LASER_BURST_2",
        "LASER_BURST_2_A",
        "LASER_BURST_3",
        "LASER_BURST_5",
        "LASER_HEAVY_1",
        "LASER_HEAVY_2",
        "LASER_HULL_1",
        "LASER_HULL_2",
        "LASER_CHAINGUN",
        "LASER_CHAINGUN_2",
        "LASER_CHARGEGUN",
        "LASER_CHARGEGUN_2",
        // Ion
        "ION_1",
        "ION_2",
        "ION_4",
        "ION_STUN",
        "ION_CHARGEGUN",
        "ION_CHAINGUN",
        // Missiles
        "MISSILES_1",
        "MISSILES_2",
        "MISSILES_3",
        "MISSILES_BURST",
        "MISSILES_BREACH",
        "MISSILES_HULL",
        "MISSILE_CHARGEGUN",
        // Beams
        "BEAM_1",
        "BEAM_2",
        "BEAM_3",
        "BEAM_LONG",
        "BEAM_FIRE",
        "BEAM_BIO",
        "BEAM_HULL",
        // Bombs
        "BOMB_1",
        "BOMB_FIRE",
        "BOMB_BREACH_1",
        "BOMB_BREACH_2",
        "BOMB_ION",
        "BOMB_HEAL",
        "BOMB_LOCK",
        "BOMB_STUN",
        "BOMB_HEAL_SYSTEM",
        // Crystal
        "CRYSTAL_BURST_1",
        "CRYSTAL_BURST_2",
        "CRYSTAL_HEAVY_1",
        "CRYSTAL_HEAVY_2",
        // Flak
        "SHOTGUN_PLAYER",
        "SHOTGUN_2",
    ];

    public static readonly string[] Drones =
    [
        "COMBAT_1",
        "COMBAT_2",
        "COMBAT_BEAM",
        "COMBAT_BEAM_2",
        "COMBAT_FIRE",
        "COMBAT_ION",
        "DEFENSE_1",
        "DEFENSE_2",
        "ANTI_DRONE",
        "SHIP_REPAIR",
        "REPAIR",
        "BATTLE",
        "BOARDER",
        "BOARDER_ION",
        "DRONE_SHIELD_PLAYER",
    ];

    public static readonly string[] Augments =
    [
        "ADV_SCANNERS",
        "AUTO_COOLDOWN",
        "BACKUP_DNA",
        "BATTERY_BOOSTER",
        "CLOAK_FIRE",
        "DEFENSE_SCRAMBLER",
        "DRONE_RECOVERY",
        "DRONE_SPEED",
        "ENERGY_SHIELD",
        "EXPLOSIVE_REPLICATOR",
        "FIRE_EXTINGUISHERS",
        "FLEET_DISTRACTION",
        "FTL_BOOSTER",
        "FTL_JAMMER",
        "FTL_JUMPER",
        "HACKING_STUN",
        "ION_ARMOR",
        "LIFE_SCANNER",
        "NANO_MEDBAY",
        "O2_MASKS",
        "REPAIR_ARM",
        "ROCK_ARMOR",
        "SCRAP_COLLECTOR",
        "SHIELD_RECHARGE",
        "SLUG_GEL",
        "CREW_STIMS",
        "CRYSTAL_SHARDS",
        "STASIS_POD",
        "SYSTEM_CASING",
        "TELEPORT_HEAL",
        "WEAPON_PREIGNITE",
        "ZOLTAN_BYPASS",
    ];
}
