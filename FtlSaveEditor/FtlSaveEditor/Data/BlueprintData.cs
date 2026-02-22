namespace FtlSaveEditor.Data;

public record WeaponBlueprint(
    string Id,
    string Title,
    string Description,
    string Type,
    int Damage,
    int Shots,
    int Power,
    int Cost,
    double Cooldown);

public record DroneBlueprint(
    string Id,
    string Title,
    string Description,
    string Type,
    int Power,
    int Cost);

public record AugBlueprint(
    string Id,
    string Title,
    string Description,
    int Cost,
    bool Stackable);

public class ModBlueprints
{
    public Dictionary<string, WeaponBlueprint> Weapons { get; set; } = new();
    public Dictionary<string, DroneBlueprint> Drones { get; set; } = new();
    public Dictionary<string, AugBlueprint> Augments { get; set; } = new();
    public HashSet<string> CrewRaces { get; set; } = new();

    public bool HasData => Weapons.Count > 0 || Drones.Count > 0 || Augments.Count > 0;
}
