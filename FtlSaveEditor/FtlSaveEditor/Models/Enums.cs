namespace FtlSaveEditor.Models;

public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

public enum SystemType
{
    Shields = 0,
    Engines = 1,
    Oxygen = 2,
    Weapons = 3,
    DroneControl = 4,
    Medbay = 5,
    Pilot = 6,
    Sensors = 7,
    Doors = 8,
    Teleporter = 9,
    Cloaking = 10,
    Artillery = 11,
    Battery = 12,
    Clonebay = 13,
    MindControl = 14,
    Hacking = 15
}

public enum FleetPresence
{
    None = 0,
    Rebel = 1,
    Federation = 2,
    Both = 3
}

public enum StoreItemType
{
    Weapon = 0,
    Drone = 1,
    Augment = 2,
    Crew = 3,
    System = 4
}

public enum HazardVulnerability
{
    PlayerShip = 0,
    NearbyShip = 1,
    BothShips = 2
}

public enum ProjectileType
{
    Invalid = 0,
    LaserOrBurst = 1,
    RockOrExplosion = 2,
    Missile = 3,
    Bomb = 4,
    Beam = 5,
    Pds = 6
}

public static class SystemTypeHelper
{
    public static readonly Dictionary<SystemType, string> DisplayNames = new()
    {
        [SystemType.Shields] = "Shields",
        [SystemType.Engines] = "Engines",
        [SystemType.Oxygen] = "Oxygen",
        [SystemType.Weapons] = "Weapons",
        [SystemType.DroneControl] = "Drone Control",
        [SystemType.Medbay] = "Medbay",
        [SystemType.Pilot] = "Pilot",
        [SystemType.Sensors] = "Sensors",
        [SystemType.Doors] = "Doors",
        [SystemType.Teleporter] = "Teleporter",
        [SystemType.Cloaking] = "Cloaking",
        [SystemType.Artillery] = "Artillery",
        [SystemType.Battery] = "Battery",
        [SystemType.Clonebay] = "Clonebay",
        [SystemType.MindControl] = "Mind Control",
        [SystemType.Hacking] = "Hacking"
    };

    public static SystemType[] GetOrderedSystemTypes(int formatVersion)
    {
        var list = new List<SystemType>
        {
            SystemType.Shields, SystemType.Engines, SystemType.Oxygen,
            SystemType.Weapons, SystemType.DroneControl, SystemType.Medbay,
            SystemType.Pilot, SystemType.Sensors, SystemType.Doors,
            SystemType.Teleporter, SystemType.Cloaking, SystemType.Artillery
        };
        if (formatVersion >= 7)
        {
            list.Add(SystemType.Battery);
            list.Add(SystemType.Clonebay);
            list.Add(SystemType.MindControl);
            list.Add(SystemType.Hacking);
        }
        return list.ToArray();
    }
}
