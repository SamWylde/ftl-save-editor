using System.Collections.Generic;

namespace FtlSaveEditor.Models;

[System.Flags]
public enum EditorCapability
{
    None = 0,
    Metadata = 1 << 0,
    StateVars = 1 << 1,
    Ship = 1 << 2,
    Crew = 1 << 3,
    Systems = 1 << 4,
    Weapons = 1 << 5,
    Drones = 1 << 6,
    Augments = 1 << 7,
    Cargo = 1 << 8,
    Beacons = 1 << 9,
    Misc = 1 << 10,
    Full =
        StateVars |
        Ship |
        Crew |
        Systems |
        Weapons |
        Drones |
        Augments |
        Cargo |
        Beacons |
        Misc
}

public enum SaveParseMode
{
    Full,
    PartialPlayerShipOpaqueTail,
    RestrictedOpaqueTail
}

public class ParseDiagnostic
{
    public string Section { get; set; } = "";
    public long? ByteOffset { get; set; }
    public string Message { get; set; } = "";
    public string? LogPath { get; set; }
}

public class SavedGameState
{
    public SaveParseMode ParseMode { get; set; } = SaveParseMode.Full;
    public byte[] OpaquePrePlayerShipBytes { get; set; } = [];
    public byte[] OpaqueTailBytes { get; set; } = [];
    public EditorCapability Capabilities { get; set; } = EditorCapability.Full;
    public List<string> ParseWarnings { get; set; } = new();
    public List<ParseDiagnostic> ParseDiagnostics { get; set; } = new();
    public int FileFormat { get; set; }
    public bool RandomNative { get; set; }
    public bool DlcEnabled { get; set; }
    public int Difficulty { get; set; }
    public int TotalShipsDefeated { get; set; }
    public int TotalBeaconsExplored { get; set; }
    public int TotalScrapCollected { get; set; }
    public int TotalCrewHired { get; set; }
    public string PlayerShipName { get; set; } = "";
    public string PlayerShipBlueprintId { get; set; } = "";
    public int OneBasedSectorNumber { get; set; }
    public int UnknownBeta { get; set; }
    public List<StateVar> StateVars { get; set; } = new();
    public ShipState PlayerShip { get; set; } = new();
    public List<string> CargoIdList { get; set; } = new();
    public int SectorTreeSeed { get; set; }
    public int SectorLayoutSeed { get; set; }
    public int RebelFleetOffset { get; set; }
    public int RebelFleetFudge { get; set; }
    public int RebelPursuitMod { get; set; }
    public int CurrentBeaconId { get; set; }
    public bool Waiting { get; set; }
    public int WaitEventSeed { get; set; }
    public string UnknownEpsilon { get; set; } = "";
    public bool SectorHazardsVisible { get; set; }
    public bool RebelFlagshipVisible { get; set; }
    public int RebelFlagshipHop { get; set; }
    public bool RebelFlagshipMoving { get; set; }
    public bool RebelFlagshipRetreating { get; set; }
    public int RebelFlagshipBaseTurns { get; set; }
    public bool F2SectorHazardsVisible { get; set; }
    public bool F2RebelFlagshipVisible { get; set; }
    public int F2RebelFlagshipHop { get; set; }
    public bool F2RebelFlagshipMoving { get; set; }
    public List<bool> SectorVisitation { get; set; } = new();
    public int SectorNumber { get; set; }
    public bool SectorIsHiddenCrystalWorlds { get; set; }
    public List<BeaconState> Beacons { get; set; } = new();
    public List<QuestEvent> QuestEventMap { get; set; } = new();
    public List<string> DistantQuestEventList { get; set; } = new();
    public int F2CurrentBeaconId { get; set; }
    public int UnknownMu { get; set; }
    public EncounterState? Encounter { get; set; }
    public bool NearbyShipPresent { get; set; }
    public ShipState? NearbyShip { get; set; }
    public NearbyShipAIState? NearbyShipAi { get; set; }
    public EnvironmentState? Environment { get; set; }
    public List<ProjectileState> Projectiles { get; set; } = new();
    public ExtendedShipInfo? PlayerExtendedInfo { get; set; }
    public ExtendedShipInfo? NearbyExtendedInfo { get; set; }
    public int UnknownNu { get; set; }
    public int? UnknownXi { get; set; }
    public bool Autofire { get; set; }
    public RebelFlagshipState? RebelFlagship { get; set; }
    public RebelFlagshipState? F2RebelFlagship { get; set; }
}

public class StateVar
{
    public string Key { get; set; } = "";
    public int Value { get; set; }
}

public class QuestEvent
{
    public string QuestEventId { get; set; } = "";
    public int QuestBeaconId { get; set; }
}

public class ShipState
{
    public string ShipBlueprintId { get; set; } = "";
    public string ShipName { get; set; } = "";
    public string ShipGfxBaseName { get; set; } = "";
    public string? ExtraShipStringBeforeCrew { get; set; }
    public List<StartingCrewMember> StartingCrew { get; set; } = new();
    public bool Hostile { get; set; }
    public int JumpChargeTicks { get; set; }
    public bool Jumping { get; set; }
    public int JumpAnimTicks { get; set; }
    public int HullAmt { get; set; }
    public int FuelAmt { get; set; }
    public int DronePartsAmt { get; set; }
    public int MissilesAmt { get; set; }
    public int ScrapAmt { get; set; }
    public List<CrewState> Crew { get; set; } = new();
    public int ReservePowerCapacity { get; set; }
    public List<SystemState> Systems { get; set; } = new();
    public ClonebayInfo? ClonebayInfo { get; set; }
    public BatteryInfo? BatteryInfo { get; set; }
    public ShieldsInfo? ShieldsInfo { get; set; }
    public CloakingInfo? CloakingInfo { get; set; }
    public List<RoomState> Rooms { get; set; } = new();
    public List<BreachState> Breaches { get; set; } = new();
    public List<DoorState> Doors { get; set; } = new();
    public int CloakAnimTicks { get; set; }
    public List<LockdownCrystal> LockdownCrystals { get; set; } = new();
    public byte[] OpaqueRoomDoorBytes { get; set; } = [];
    /// <summary>
    /// In partial parse mode: all bytes from crew count through end of room/door data
    /// (crew, systems, clonebay/battery/shields/cloaking, rooms/breaches/doors).
    /// Contains Hyperspace extensions that we cannot parse.
    /// Empty when crew was successfully parsed from the opaque interior.
    /// </summary>
    public byte[] OpaqueShipInteriorBytes { get; set; } = [];
    /// <summary>
    /// In partial parse mode when crew parsing succeeds: all bytes after the last crew member
    /// (reserve power, systems, clonebay/battery/shields/cloaking, rooms/breaches/doors).
    /// </summary>
    public byte[] OpaquePostCrewBytes { get; set; } = [];
    public List<WeaponState> Weapons { get; set; } = new();
    public List<DroneState> Drones { get; set; } = new();
    public List<string> AugmentIds { get; set; } = new();
}

public class StartingCrewMember
{
    public string Race { get; set; } = "";
    public string Name { get; set; } = "";
}

public class CrewState
{
    /// <summary>
    /// Opaque HS inline extension bytes BEFORE the origColorRace/origRace strings.
    /// Contains: 2 sentinel ints + powerCount + variable power data + resourceCount + variable resource data.
    /// Empty for vanilla saves.
    /// </summary>
    public byte[] HsInlinePreStringBytes { get; set; } = [];
    /// <summary>
    /// Opaque HS inline extension bytes AFTER the origColorRace/origRace strings.
    /// Contains: 4 customTele ints + boostCount + variable boost data + 6 extra ints.
    /// Empty for vanilla saves.
    /// </summary>
    public byte[] HsInlinePostStringBytes { get; set; } = [];
    /// <summary>
    /// The origColorRace string from the HS inline extension. Empty for vanilla saves.
    /// </summary>
    public string HsOriginalColorRace { get; set; } = "";
    /// <summary>
    /// The origRace string from the HS inline extension. Empty for vanilla saves.
    /// </summary>
    public string HsOriginalRace { get; set; } = "";
    public string Name { get; set; } = "";
    public string Race { get; set; } = "";
    public bool EnemyBoardingDrone { get; set; }
    public int Health { get; set; }
    public int SpriteX { get; set; }
    public int SpriteY { get; set; }
    public int RoomId { get; set; }
    public int RoomSquare { get; set; }
    public bool PlayerControlled { get; set; }
    public int CloneReady { get; set; }
    public int DeathOrder { get; set; }
    public List<int> SpriteTintIndices { get; set; } = new();
    public bool MindControlled { get; set; }
    public int SavedRoomSquare { get; set; }
    public int SavedRoomId { get; set; }
    public int PilotSkill { get; set; }
    public int EngineSkill { get; set; }
    public int ShieldSkill { get; set; }
    public int WeaponSkill { get; set; }
    public int RepairSkill { get; set; }
    public int CombatSkill { get; set; }
    public bool Male { get; set; }
    public int Repairs { get; set; }
    public int CombatKills { get; set; }
    public int PilotedEvasions { get; set; }
    public int JumpsSurvived { get; set; }
    public int SkillMasteriesEarned { get; set; }
    public int StunTicks { get; set; }
    public int HealthBoost { get; set; }
    public int ClonebayPriority { get; set; }
    public int DamageBoost { get; set; }
    public int UnknownLambda { get; set; }
    public int UniversalDeathCount { get; set; }
    public bool PilotMasteryOne { get; set; }
    public bool PilotMasteryTwo { get; set; }
    public bool EngineMasteryOne { get; set; }
    public bool EngineMasteryTwo { get; set; }
    public bool ShieldMasteryOne { get; set; }
    public bool ShieldMasteryTwo { get; set; }
    public bool WeaponMasteryOne { get; set; }
    public bool WeaponMasteryTwo { get; set; }
    public bool RepairMasteryOne { get; set; }
    public bool RepairMasteryTwo { get; set; }
    public bool CombatMasteryOne { get; set; }
    public bool CombatMasteryTwo { get; set; }
    public bool UnknownNu { get; set; }
    public AnimState? TeleportAnim { get; set; }
    public bool UnknownPhi { get; set; }
    public int? LockdownRechargeTicks { get; set; }
    public int? LockdownRechargeTicksGoal { get; set; }
    public int? UnknownOmega { get; set; }
}

public class SystemState
{
    public SystemType SystemType { get; set; }
    public int Capacity { get; set; }
    public int Power { get; set; }
    public int DamagedBars { get; set; }
    public int IonizedBars { get; set; }
    public int DeionizationTicks { get; set; } = int.MinValue;
    public int RepairProgress { get; set; }
    public int DamageProgress { get; set; }
    public int BatteryPower { get; set; }
    public int HackLevel { get; set; }
    public bool Hacked { get; set; }
    public int TemporaryCapacityCap { get; set; }
    public int TemporaryCapacityLoss { get; set; }
    public int TemporaryCapacityDivisor { get; set; }
}

public class ClonebayInfo
{
    public int BuildTicks { get; set; }
    public int BuildTicksGoal { get; set; }
    public int DoomTicks { get; set; }
}

public class BatteryInfo
{
    public bool Active { get; set; }
    public int UsedBattery { get; set; }
    public int DischargeTicks { get; set; }
}

public class ShieldsInfo
{
    public int ShieldLayers { get; set; }
    public int EnergyShieldLayers { get; set; }
    public int EnergyShieldMax { get; set; }
    public int ShieldRechargeTicks { get; set; }
    public bool ShieldDropAnimOn { get; set; }
    public int ShieldDropAnimTicks { get; set; }
    public bool ShieldRaiseAnimOn { get; set; }
    public int ShieldRaiseAnimTicks { get; set; }
    public bool EnergyShieldAnimOn { get; set; }
    public int EnergyShieldAnimTicks { get; set; }
    public int UnknownLambda { get; set; }
    public int UnknownMu { get; set; }
}

public class CloakingInfo
{
    public int UnknownAlpha { get; set; }
    public int UnknownBeta { get; set; }
    public int CloakTicksGoal { get; set; }
    public int CloakTicks { get; set; }
}

public class RoomState
{
    public int Oxygen { get; set; }
    public List<SquareState> Squares { get; set; } = new();
    public int StationSquare { get; set; } = -2;
    public int StationDirection { get; set; } = 4;
}

public class SquareState
{
    public int FireHealth { get; set; }
    public int IgnitionProgress { get; set; }
    public int ExtinguishmentProgress { get; set; }
}

public class BreachState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Health { get; set; }
}

public class DoorState
{
    public int CurrentMaxHealth { get; set; }
    public int Health { get; set; }
    public int NominalHealth { get; set; }
    public bool Open { get; set; }
    public bool WalkingThrough { get; set; }
    public int UnknownDelta { get; set; }
    public int UnknownEpsilon { get; set; }
}

public class LockdownCrystal
{
    public int CurrentPositionX { get; set; }
    public int CurrentPositionY { get; set; }
    public int Speed { get; set; }
    public int GoalPositionX { get; set; }
    public int GoalPositionY { get; set; }
    public bool Arrived { get; set; }
    public bool Done { get; set; }
    public int Lifetime { get; set; }
    public bool SuperFreeze { get; set; }
    public int LockingRoom { get; set; }
    public int AnimDirection { get; set; }
    public int ShardProgress { get; set; }
}

public class WeaponState
{
    public string WeaponId { get; set; } = "";
    public bool Armed { get; set; }
    public int CooldownTicks { get; set; }
}

public class DroneState
{
    public string DroneId { get; set; } = "";
    public bool Armed { get; set; }
    public bool PlayerControlled { get; set; }
    public int BodyX { get; set; }
    public int BodyY { get; set; }
    public int BodyRoomId { get; set; }
    public int BodyRoomSquare { get; set; }
    public int Health { get; set; }
}

public class BeaconState
{
    public int VisitCount { get; set; }
    public string? BgStarscapeImage { get; set; }
    public string? BgSpriteImage { get; set; }
    public int? BgSpritePosX { get; set; }
    public int? BgSpritePosY { get; set; }
    public int? BgSpriteRotation { get; set; }
    public bool Seen { get; set; }
    public bool EnemyPresent { get; set; }
    public string? ShipEventId { get; set; }
    public string? AutoBlueprintId { get; set; }
    public int? ShipEventSeed { get; set; }
    public int FleetPresence { get; set; }
    public bool UnderAttack { get; set; }
    public bool StorePresent { get; set; }
    public StoreState? Store { get; set; }
    public bool UnknownEta { get; set; }
}

public class StoreState
{
    public int ShelfCount { get; set; }
    public List<StoreShelf> Shelves { get; set; } = new();
    public int Fuel { get; set; }
    public int Missiles { get; set; }
    public int DroneParts { get; set; }
}

public class StoreShelf
{
    public int ItemType { get; set; }
    public List<StoreItem> Items { get; set; } = new();
}

public class StoreItem
{
    public int Available { get; set; }
    public string? ItemId { get; set; }
    public int? ExtraData { get; set; }
}

public class EncounterState
{
    public int ShipEventSeed { get; set; }
    public string SurrenderEventId { get; set; } = "";
    public string EscapeEventId { get; set; } = "";
    public string DestroyedEventId { get; set; } = "";
    public string DeadCrewEventId { get; set; } = "";
    public string GotAwayEventId { get; set; } = "";
    public string LastEventId { get; set; } = "";
    public int? UnknownAlpha { get; set; }
    public string Text { get; set; } = "";
    public int AffectedCrewSeed { get; set; }
    public List<int> Choices { get; set; } = new();
}

public class NearbyShipAIState
{
    public bool Surrendered { get; set; }
    public bool Escaping { get; set; }
    public bool Destroyed { get; set; }
    public int SurrenderThreshold { get; set; }
    public int EscapeThreshold { get; set; }
    public int EscapeTicks { get; set; }
    public bool StalemateTriggered { get; set; }
    public int StalemateTicks { get; set; }
    public int BoardingAttempts { get; set; }
    public int BoardersNeeded { get; set; }
}

public class EnvironmentState
{
    public bool RedGiantPresent { get; set; }
    public bool PulsarPresent { get; set; }
    public bool PdsPresent { get; set; }
    public int VulnerableShips { get; set; }
    public bool AsteroidsPresent { get; set; }
    public AsteroidFieldState? AsteroidField { get; set; }
    public int SolarFlareFadeTicks { get; set; }
    public int HavocTicks { get; set; }
    public int PdsTicks { get; set; }
}

public class AsteroidFieldState
{
    public int UnknownAlpha { get; set; }
    public int StrayRockTicks { get; set; }
    public int UnknownGamma { get; set; }
    public int BgDriftTicks { get; set; }
    public int CurrentTarget { get; set; }
}

public class ProjectileState
{
    public int ProjectileType { get; set; }
    public int CurrentPositionX { get; set; }
    public int CurrentPositionY { get; set; }
    public int PreviousPositionX { get; set; }
    public int PreviousPositionY { get; set; }
    public int Speed { get; set; }
    public int GoalPositionX { get; set; }
    public int GoalPositionY { get; set; }
    public int Heading { get; set; }
    public int OwnerId { get; set; }
    public int SelfId { get; set; }
    public DamageState Damage { get; set; } = new();
    public int Lifespan { get; set; }
    public int DestinationSpace { get; set; }
    public int CurrentSpace { get; set; }
    public int TargetId { get; set; }
    public bool Dead { get; set; }
    public string DeathAnimId { get; set; } = "";
    public string FlightAnimId { get; set; } = "";
    public AnimState DeathAnim { get; set; } = new();
    public AnimState FlightAnim { get; set; } = new();
    public int VelocityX { get; set; }
    public int VelocityY { get; set; }
    public bool Missed { get; set; }
    public bool HitTarget { get; set; }
    public string HitSolidSound { get; set; } = "";
    public string HitShieldSound { get; set; } = "";
    public string MissSound { get; set; } = "";
    public int EntryAngle { get; set; }
    public bool StartedDying { get; set; }
    public bool PassedTarget { get; set; }
    public int BroadcastType { get; set; }
    public bool BroadcastTarget { get; set; }
    public ExtendedProjectileInfo? ExtendedInfo { get; set; }
}

public class ExtendedProjectileInfo
{
    public string Type { get; set; } = "";
    // Laser
    public int UnknownAlpha { get; set; }
    public int Spin { get; set; }
    // Bomb
    public int FuseTicks { get; set; }
    public int UnknownGamma { get; set; }
    public int UnknownDelta { get; set; }
    public bool Arrived { get; set; }
    // Beam
    public int EmissionEndX { get; set; }
    public int EmissionEndY { get; set; }
    public int StrafeSourceX { get; set; }
    public int StrafeSourceY { get; set; }
    public int StrafeEndX { get; set; }
    public int StrafeEndY { get; set; }
    public int UnknownBetaX { get; set; }
    public int UnknownBetaY { get; set; }
    public int SwathEndX { get; set; }
    public int SwathEndY { get; set; }
    public int SwathStartX { get; set; }
    public int SwathStartY { get; set; }
    public int SwathLength { get; set; }
    public int UnknownEpsilonX { get; set; }
    public int UnknownEpsilonY { get; set; }
    public int UnknownZeta { get; set; }
    public int UnknownEta { get; set; }
    public int EmissionAngle { get; set; }
    public bool UnknownIota { get; set; }
    public bool UnknownKappa { get; set; }
    public bool FromDronePod { get; set; }
    public bool UnknownMu { get; set; }
    public bool UnknownNu { get; set; }
    // Pds
    public int UnknownBeta { get; set; }
    public int UnknownEpsilon { get; set; }
    public AnimState? UnknownZetaAnim { get; set; }
}

public class DamageState
{
    public int HullDamage { get; set; }
    public int ShieldPiercing { get; set; }
    public int FireChance { get; set; }
    public int BreachChance { get; set; }
    public int IonDamage { get; set; }
    public int SystemDamage { get; set; }
    public int PersonnelDamage { get; set; }
    public bool HullBuster { get; set; }
    public int OwnerId { get; set; }
    public int SelfId { get; set; }
    public bool Lockdown { get; set; }
    public bool CrystalShard { get; set; }
    public int StunChance { get; set; }
    public int StunAmount { get; set; }
}

public class AnimState
{
    public int Playing { get; set; }
    public int Looping { get; set; }
    public int CurrentFrame { get; set; }
    public int ProgressTicks { get; set; }
    public int Scale { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class ExtendedShipInfo
{
    public HackingInfo? HackingInfo { get; set; }
    public MindControlInfo? MindControlInfo { get; set; }
    public List<WeaponModule> WeaponModules { get; set; } = new();
    public List<DroneModule> DroneModules { get; set; } = new();
}

public class HackingInfo
{
    public int TargetSystemType { get; set; }
    public int UnknownBeta { get; set; }
    public bool DronePodVisible { get; set; }
    public int UnknownDelta { get; set; }
    public int DisruptionTicks { get; set; }
    public int DisruptionTicksGoal { get; set; }
    public bool Disrupting { get; set; }
    public DronePodState DronePod { get; set; } = new();
}

public class DronePodState
{
    public int MourningTicks { get; set; }
    public int CurrentSpace { get; set; }
    public int DestinationSpace { get; set; }
    public int CurrentPositionX { get; set; }
    public int CurrentPositionY { get; set; }
    public int PreviousPositionX { get; set; }
    public int PreviousPositionY { get; set; }
    public int GoalPositionX { get; set; }
    public int GoalPositionY { get; set; }
    public int UnknownEpsilon { get; set; }
    public int UnknownZeta { get; set; }
    public int NextTargetX { get; set; }
    public int NextTargetY { get; set; }
    public int UnknownIota { get; set; }
    public int UnknownKappa { get; set; }
    public int BuildupTicks { get; set; }
    public int StationaryTicks { get; set; }
    public int CooldownTicks { get; set; }
    public int OrbitAngle { get; set; }
    public int TurretAngle { get; set; }
    public int UnknownXi { get; set; }
    public int HopsToLive { get; set; }
    public int UnknownPi { get; set; }
    public int UnknownRho { get; set; }
    public int OverloadTicks { get; set; }
    public int UnknownTau { get; set; }
    public int UnknownUpsilon { get; set; }
    public int DeltaPositionX { get; set; }
    public int DeltaPositionY { get; set; }
    public AnimState DeathAnim { get; set; } = new();
    // Hacking drone-specific extension fields
    public int AttachPositionX { get; set; }
    public int AttachPositionY { get; set; }
    public int HackUnknownGamma { get; set; }
    public int HackUnknownDelta { get; set; }
    public AnimState LandingAnim { get; set; } = new();
    public AnimState ExtensionAnim { get; set; } = new();
}

public class MindControlInfo
{
    public int MindControlTicks { get; set; }
    public int MindControlTicksGoal { get; set; }
}

public class WeaponModule
{
    public int CooldownTicks { get; set; }
    public int CooldownGoal { get; set; }
    public int SubcooldownTicks { get; set; }
    public int SubcooldownTicksGoal { get; set; }
    public int Boost { get; set; }
    public int Charge { get; set; }
    public int CurrentTargetsCount { get; set; }
    public AnimState WeaponAnim { get; set; } = new();
    public int ProtractAnimTicks { get; set; }
    public bool Firing { get; set; }
    public bool FireWhenReady { get; set; }
    public int TargetId { get; set; }
    public AnimState? HackAnim { get; set; }
    public bool IsOnFire { get; set; }
    public int FireId { get; set; }
    public bool Autofire { get; set; }
}

public class DroneModule
{
    public bool Deployed { get; set; }
    public bool Armed { get; set; }
    public ExtendedDroneInfo? ExtendedDroneInfo { get; set; }
}

public class ExtendedDroneInfo
{
    public int BodyX { get; set; }
    public int BodyY { get; set; }
    public int CurrentSpace { get; set; }
    public int DestinationSpace { get; set; }
    public int CurrentPositionX { get; set; }
    public int CurrentPositionY { get; set; }
    public int PreviousPositionX { get; set; }
    public int PreviousPositionY { get; set; }
    public int GoalPositionX { get; set; }
    public int GoalPositionY { get; set; }
}

public class RebelFlagshipState
{
    public int UnknownAlpha { get; set; }
    public int PendingStage { get; set; }
    public int UnknownGamma { get; set; }
    public int UnknownDelta { get; set; }
    public List<int> PreviousOccupancy { get; set; } = new();
}
