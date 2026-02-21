namespace FtlSaveEditor.Tests.Fixtures;

using System.IO;
using System.Text;
using FtlSaveEditor.Models;

internal static class FixtureFactory
{
    public static SavedGameState CreateFullStateWithScannerTrap()
    {
        var state = CreateMinimalFormat11State();
        state.PlayerShip.Weapons =
        [
            new WeaponState { WeaponId = "LASER_BURST_2", Armed = true, CooldownTicks = 0 },
            new WeaponState { WeaponId = "ION_STUN", Armed = false, CooldownTicks = 0 },
        ];
        state.PlayerShip.AugmentIds = ["AUTO_COOLDOWN", "REPAIR_ARM"];
        state.CargoIdList = ["DRONE_DEFENSE", "MISSILES_SMALL"];
        state.PlayerExtendedInfo = new ExtendedShipInfo
        {
            WeaponModules =
            [
                CreateWeaponModule(),
                CreateWeaponModule(),
            ],
            DroneModules = []
        };

        state.PlayerShip.OpaqueRoomDoorBytes = CreateScannerTrapOpaqueRoomBytes();
        return state;
    }

    public static byte[] CreateFormat11DataThatTriggersFallback(out byte[] tailBytes)
    {
        tailBytes =
        [
            0x20, 0x17, 0x04, 0x00, // implausible first string length for ship section
            0x7B, 0xB8, 0x00, 0x00,
            0x58, 0x8C, 0x02, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteHeaderAndStateVars(writer);
        writer.Write(tailBytes);
        return ms.ToArray();
    }

    public static HeaderSnapshot ReadHeaderSnapshot(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        int fmt = reader.ReadInt32();
        bool randomNative = fmt >= 11 && reader.ReadInt32() != 0;
        bool dlcEnabled = fmt >= 7 && reader.ReadInt32() != 0;
        int difficulty = reader.ReadInt32();
        int totalShipsDefeated = reader.ReadInt32();
        int totalBeaconsExplored = reader.ReadInt32();
        int totalScrapCollected = reader.ReadInt32();
        int totalCrewHired = reader.ReadInt32();
        string playerShipName = ReadString(reader);
        string playerShipBlueprintId = ReadString(reader);
        int oneBasedSectorNumber = reader.ReadInt32();
        int unknownBeta = reader.ReadInt32();
        int stateVarCount = reader.ReadInt32();
        var vars = new List<StateVar>();
        for (int i = 0; i < stateVarCount; i++)
        {
            vars.Add(new StateVar
            {
                Key = ReadString(reader),
                Value = reader.ReadInt32()
            });
        }

        return new HeaderSnapshot
        {
            FileFormat = fmt,
            RandomNative = randomNative,
            DlcEnabled = dlcEnabled,
            Difficulty = difficulty,
            TotalShipsDefeated = totalShipsDefeated,
            TotalBeaconsExplored = totalBeaconsExplored,
            TotalScrapCollected = totalScrapCollected,
            TotalCrewHired = totalCrewHired,
            PlayerShipName = playerShipName,
            PlayerShipBlueprintId = playerShipBlueprintId,
            OneBasedSectorNumber = oneBasedSectorNumber,
            UnknownBeta = unknownBeta,
            StateVars = vars,
            OffsetAfterStateVars = ms.Position,
        };
    }

    private static SavedGameState CreateMinimalFormat11State()
    {
        return new SavedGameState
        {
            FileFormat = 11,
            RandomNative = false,
            DlcEnabled = true,
            Difficulty = 2,
            TotalShipsDefeated = 4,
            TotalBeaconsExplored = 8,
            TotalScrapCollected = 120,
            TotalCrewHired = 2,
            PlayerShipName = "Fixture Ship",
            PlayerShipBlueprintId = "PLAYER_SHIP_TEST",
            OneBasedSectorNumber = 3,
            UnknownBeta = 0,
            StateVars =
            [
                new StateVar { Key = "fired_shot", Value = 3 },
                new StateVar { Key = "env_danger", Value = 1 },
            ],
            PlayerShip = new ShipState
            {
                ShipBlueprintId = "PLAYER_SHIP_TEST",
                ShipName = "Fixture Ship",
                ShipGfxBaseName = "fixture_cruiser",
                StartingCrew = [],
                Hostile = false,
                JumpChargeTicks = 0,
                Jumping = false,
                JumpAnimTicks = 0,
                HullAmt = 30,
                FuelAmt = 12,
                DronePartsAmt = 4,
                MissilesAmt = 8,
                ScrapAmt = 55,
                Crew = [],
                ReservePowerCapacity = 8,
                Systems = CreateZeroSystems(),
                ShieldsInfo = new ShieldsInfo
                {
                    ShieldLayers = 0,
                    EnergyShieldLayers = 0,
                    EnergyShieldMax = 0,
                    ShieldRechargeTicks = 0,
                    ShieldDropAnimOn = false,
                    ShieldDropAnimTicks = 0,
                    ShieldRaiseAnimOn = false,
                    ShieldRaiseAnimTicks = 0,
                    EnergyShieldAnimOn = false,
                    EnergyShieldAnimTicks = 0,
                    UnknownLambda = 0,
                    UnknownMu = 0,
                },
                Rooms = [],
                Breaches = [],
                Doors = [],
                LockdownCrystals = [],
                Weapons = [],
                Drones = [],
                AugmentIds = [],
            },
            CargoIdList = [],
            SectorTreeSeed = 111,
            SectorLayoutSeed = 222,
            RebelFleetOffset = 0,
            RebelFleetFudge = 0,
            RebelPursuitMod = 0,
            CurrentBeaconId = 0,
            Waiting = false,
            WaitEventSeed = 0,
            UnknownEpsilon = "",
            SectorHazardsVisible = false,
            RebelFlagshipVisible = false,
            RebelFlagshipHop = 0,
            RebelFlagshipMoving = false,
            RebelFlagshipRetreating = false,
            RebelFlagshipBaseTurns = 0,
            SectorVisitation = [],
            SectorNumber = 1,
            SectorIsHiddenCrystalWorlds = false,
            Beacons = [],
            QuestEventMap = [],
            DistantQuestEventList = [],
            UnknownMu = 0,
            Encounter = new EncounterState
            {
                ShipEventSeed = 0,
                SurrenderEventId = "",
                EscapeEventId = "",
                DestroyedEventId = "",
                DeadCrewEventId = "",
                GotAwayEventId = "",
                LastEventId = "",
                UnknownAlpha = 0,
                Text = "",
                AffectedCrewSeed = 0,
                Choices = [],
            },
            NearbyShipPresent = false,
            Environment = new EnvironmentState
            {
                RedGiantPresent = false,
                PulsarPresent = false,
                PdsPresent = false,
                VulnerableShips = 0,
                AsteroidsPresent = false,
                AsteroidField = null,
                SolarFlareFadeTicks = 0,
                HavocTicks = 0,
                PdsTicks = 0,
            },
            Projectiles = [],
            PlayerExtendedInfo = new ExtendedShipInfo
            {
                WeaponModules = [],
                DroneModules = [],
            },
            UnknownNu = 0,
            Autofire = false,
            RebelFlagship = new RebelFlagshipState
            {
                UnknownAlpha = 0,
                PendingStage = 0,
                UnknownGamma = 0,
                UnknownDelta = 0,
                PreviousOccupancy = [],
            },
        };
    }

    private static List<SystemState> CreateZeroSystems()
    {
        var list = new List<SystemState>();
        foreach (var systemType in SystemTypeHelper.GetOrderedSystemTypes(11))
        {
            list.Add(new SystemState
            {
                SystemType = systemType,
                Capacity = 0,
            });
        }

        return list;
    }

    private static WeaponModule CreateWeaponModule()
    {
        return new WeaponModule
        {
            CooldownTicks = 0,
            CooldownGoal = 0,
            SubcooldownTicks = 0,
            SubcooldownTicksGoal = 0,
            Boost = 0,
            Charge = 0,
            CurrentTargetsCount = 0,
            WeaponAnim = CreateAnim(),
            ProtractAnimTicks = 0,
            Firing = false,
            FireWhenReady = false,
            TargetId = 0,
            HackAnim = CreateAnim(),
            IsOnFire = false,
            FireId = 0,
            Autofire = false,
        };
    }

    private static AnimState CreateAnim()
    {
        return new AnimState
        {
            Playing = 0,
            Looping = 0,
            CurrentFrame = 0,
            ProgressTicks = 0,
            Scale = 0,
            X = 0,
            Y = 0,
        };
    }

    private static byte[] CreateScannerTrapOpaqueRoomBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // False-positive pattern for the scanner:
        // weaponCount=0, droneCount=0, augmentCount=1, augment string...
        writer.Write(0);
        writer.Write(0);
        writer.Write(1);
        WriteString(writer, "FAKE_AUG");

        // Break candidate validation via impossible cargo count.
        writer.Write(123456789);
        return ms.ToArray();
    }

    private static void WriteHeaderAndStateVars(BinaryWriter writer)
    {
        writer.Write(11); // file format
        writer.Write(0);  // randomNative
        writer.Write(1);  // dlcEnabled
        writer.Write(2);  // difficulty
        writer.Write(13); // totalShipsDefeated
        writer.Write(46); // totalBeaconsExplored
        writer.Write(557); // totalScrapCollected
        writer.Write(14); // totalCrewHired
        WriteString(writer, "Restricted Fixture");
        WriteString(writer, "PLAYER_SHIP_UNION");
        writer.Write(4); // oneBasedSectorNumber
        writer.Write(0); // unknownBeta

        writer.Write(2); // state var count
        WriteString(writer, "blue_alien");
        writer.Write(2);
        WriteString(writer, "env_danger");
        writer.Write(4);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        if (bytes.Length > 0)
        {
            writer.Write(bytes);
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        int len = reader.ReadInt32();
        if (len <= 0)
        {
            return "";
        }

        var bytes = reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }
}

internal sealed class HeaderSnapshot
{
    public int FileFormat { get; init; }
    public bool RandomNative { get; init; }
    public bool DlcEnabled { get; init; }
    public int Difficulty { get; init; }
    public int TotalShipsDefeated { get; init; }
    public int TotalBeaconsExplored { get; init; }
    public int TotalScrapCollected { get; init; }
    public int TotalCrewHired { get; init; }
    public string PlayerShipName { get; init; } = "";
    public string PlayerShipBlueprintId { get; init; } = "";
    public int OneBasedSectorNumber { get; init; }
    public int UnknownBeta { get; init; }
    public List<StateVar> StateVars { get; init; } = [];
    public long OffsetAfterStateVars { get; init; }
}
