namespace FtlSaveEditor.SaveFile;

using System.IO;
using System.Text;
using FtlSaveEditor.Models;

public class SaveFileWriter
{
    private BinaryWriter _writer = null!;

    private void WriteInt(int value) => _writer.Write(value);
    private void WriteBool(bool value) => _writer.Write(value ? 1 : 0);
    private void WriteMinMaxedInt(int value) => _writer.Write(value);
    private void WriteString(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteInt(bytes.Length);
        if (bytes.Length > 0) _writer.Write(bytes);
    }

    public byte[] Write(SavedGameState state)
    {
        using var ms = new MemoryStream();
        _writer = new BinaryWriter(ms);
        WriteSavedGame(state);
        return ms.ToArray();
    }

    // ========================================================================
    // Saved Game (top-level)
    // ========================================================================

    private void WriteSavedGame(SavedGameState state)
    {
        int fmt = state.FileFormat;

        // -- Header --
        WriteInt(fmt);
        if (fmt >= 11) WriteBool(state.RandomNative);
        if (fmt >= 7) WriteBool(state.DlcEnabled);
        WriteInt(state.Difficulty);
        WriteInt(state.TotalShipsDefeated);
        WriteInt(state.TotalBeaconsExplored);
        WriteInt(state.TotalScrapCollected);
        WriteInt(state.TotalCrewHired);
        WriteString(state.PlayerShipName);
        WriteString(state.PlayerShipBlueprintId);
        WriteInt(state.OneBasedSectorNumber);
        WriteInt(state.UnknownBeta);

        // State vars
        WriteInt(state.StateVars.Count);
        foreach (var sv in state.StateVars)
        {
            WriteString(sv.Key);
            WriteInt(sv.Value);
        }

        // -- Player Ship --
        WriteShipState(state.PlayerShip, fmt);

        // -- Cargo --
        WriteInt(state.CargoIdList.Count);
        foreach (var item in state.CargoIdList) WriteString(item);

        // -- Sector Map --
        WriteInt(state.SectorTreeSeed);
        WriteInt(state.SectorLayoutSeed);
        WriteInt(state.RebelFleetOffset);
        WriteInt(state.RebelFleetFudge);
        WriteInt(state.RebelPursuitMod);

        if (fmt >= 7)
        {
            WriteInt(state.CurrentBeaconId);
            WriteBool(state.Waiting);
            WriteInt(state.WaitEventSeed);
            WriteString(state.UnknownEpsilon);
            WriteBool(state.SectorHazardsVisible);
            WriteBool(state.RebelFlagshipVisible);
            WriteInt(state.RebelFlagshipHop);
            WriteBool(state.RebelFlagshipMoving);
            WriteBool(state.RebelFlagshipRetreating);
            WriteInt(state.RebelFlagshipBaseTurns);
        }

        if (fmt == 2)
        {
            WriteBool(state.F2SectorHazardsVisible);
            WriteBool(state.F2RebelFlagshipVisible);
            WriteInt(state.F2RebelFlagshipHop);
            WriteBool(state.F2RebelFlagshipMoving);
        }

        // Sector visitation
        WriteInt(state.SectorVisitation.Count);
        foreach (var visited in state.SectorVisitation) WriteBool(visited);
        WriteInt(state.SectorNumber);
        WriteBool(state.SectorIsHiddenCrystalWorlds);

        // Beacons
        WriteInt(state.Beacons.Count);
        foreach (var beacon in state.Beacons) WriteBeaconState(beacon, fmt);

        // Quest events
        WriteInt(state.QuestEventMap.Count);
        foreach (var qe in state.QuestEventMap)
        {
            WriteString(qe.QuestEventId);
            WriteInt(qe.QuestBeaconId);
        }
        WriteInt(state.DistantQuestEventList.Count);
        foreach (var dqe in state.DistantQuestEventList) WriteString(dqe);

        // Format 2 tail
        if (fmt == 2)
        {
            WriteInt(state.F2CurrentBeaconId);
            WriteBool(state.NearbyShipPresent);
            if (state.NearbyShipPresent)
            {
                if (state.NearbyShip != null) WriteShipState(state.NearbyShip, fmt);
                if (state.F2RebelFlagship != null)
                    WriteRebelFlagshipState(state.F2RebelFlagship);
            }
        }

        // Format 7+
        if (fmt >= 7)
        {
            WriteInt(state.UnknownMu);
            if (state.Encounter != null) WriteEncounterState(state.Encounter, fmt);

            WriteBool(state.NearbyShipPresent);
            if (state.NearbyShipPresent)
            {
                if (state.NearbyShip != null) WriteShipState(state.NearbyShip, fmt);
                if (state.NearbyShipAi != null) WriteNearbyShipAI(state.NearbyShipAi);
            }

            if (state.Environment != null) WriteEnvironmentState(state.Environment);

            // Projectiles
            WriteInt(state.Projectiles.Count);
            foreach (var proj in state.Projectiles)
                WriteProjectileState(proj, fmt);

            // Extended ship info
            if (state.PlayerExtendedInfo != null)
                WriteExtendedShipInfo(state.PlayerExtendedInfo, state.PlayerShip, fmt);
            if (state.NearbyShipPresent && state.NearbyExtendedInfo != null && state.NearbyShip != null)
                WriteExtendedShipInfo(state.NearbyExtendedInfo, state.NearbyShip, fmt);

            WriteInt(state.UnknownNu);
            if (state.NearbyShipPresent && state.UnknownXi != null)
                WriteInt(state.UnknownXi.Value);
            WriteBool(state.Autofire);

            if (state.RebelFlagship != null)
                WriteRebelFlagshipState(state.RebelFlagship);
        }
    }

    // ========================================================================
    // Ship State
    // ========================================================================

    private void WriteShipState(ShipState ship, int fmt)
    {
        WriteString(ship.ShipBlueprintId);
        WriteString(ship.ShipName);
        WriteString(ship.ShipGfxBaseName);

        WriteInt(ship.StartingCrew.Count);
        foreach (var sc in ship.StartingCrew)
        {
            WriteString(sc.Race);
            WriteString(sc.Name);
        }

        if (fmt >= 7)
        {
            WriteBool(ship.Hostile);
            WriteInt(ship.JumpChargeTicks);
            WriteBool(ship.Jumping);
            WriteInt(ship.JumpAnimTicks);
        }

        WriteInt(ship.HullAmt);
        WriteInt(ship.FuelAmt);
        WriteInt(ship.DronePartsAmt);
        WriteInt(ship.MissilesAmt);
        WriteInt(ship.ScrapAmt);

        WriteInt(ship.Crew.Count);
        foreach (var crew in ship.Crew) WriteCrewState(crew, fmt);

        WriteInt(ship.ReservePowerCapacity);
        foreach (var sys in ship.Systems) WriteSystemState(sys, fmt);

        if (fmt >= 7)
        {
            if (ship.ClonebayInfo != null)
            {
                WriteInt(ship.ClonebayInfo.BuildTicks);
                WriteInt(ship.ClonebayInfo.BuildTicksGoal);
                WriteInt(ship.ClonebayInfo.DoomTicks);
            }
            if (ship.BatteryInfo != null)
            {
                WriteBool(ship.BatteryInfo.Active);
                WriteInt(ship.BatteryInfo.UsedBattery);
                WriteInt(ship.BatteryInfo.DischargeTicks);
            }
            if (ship.ShieldsInfo != null)
            {
                WriteInt(ship.ShieldsInfo.ShieldLayers);
                WriteInt(ship.ShieldsInfo.EnergyShieldLayers);
                WriteInt(ship.ShieldsInfo.EnergyShieldMax);
                WriteInt(ship.ShieldsInfo.ShieldRechargeTicks);
                WriteBool(ship.ShieldsInfo.ShieldDropAnimOn);
                WriteInt(ship.ShieldsInfo.ShieldDropAnimTicks);
                WriteBool(ship.ShieldsInfo.ShieldRaiseAnimOn);
                WriteInt(ship.ShieldsInfo.ShieldRaiseAnimTicks);
                WriteBool(ship.ShieldsInfo.EnergyShieldAnimOn);
                WriteInt(ship.ShieldsInfo.EnergyShieldAnimTicks);
                WriteInt(ship.ShieldsInfo.UnknownLambda);
                WriteInt(ship.ShieldsInfo.UnknownMu);
            }
            if (ship.CloakingInfo != null)
            {
                WriteInt(ship.CloakingInfo.UnknownAlpha);
                WriteInt(ship.CloakingInfo.UnknownBeta);
                WriteInt(ship.CloakingInfo.CloakTicksGoal);
                WriteMinMaxedInt(ship.CloakingInfo.CloakTicks);
            }
        }

        WriteInt(ship.Rooms.Count);
        foreach (var room in ship.Rooms) WriteRoomState(room, fmt);

        WriteInt(ship.Breaches.Count);
        foreach (var b in ship.Breaches)
        {
            WriteInt(b.X);
            WriteInt(b.Y);
            WriteInt(b.Health);
        }

        WriteInt(ship.Doors.Count);
        foreach (var door in ship.Doors) WriteDoorState(door, fmt);

        if (fmt >= 7) WriteInt(ship.CloakAnimTicks);

        if (fmt >= 8)
        {
            WriteInt(ship.LockdownCrystals.Count);
            foreach (var c in ship.LockdownCrystals) WriteLockdownCrystal(c);
        }

        WriteInt(ship.Weapons.Count);
        foreach (var weapon in ship.Weapons)
        {
            WriteString(weapon.WeaponId);
            WriteBool(weapon.Armed);
            if (fmt == 2) WriteInt(weapon.CooldownTicks);
        }

        WriteInt(ship.Drones.Count);
        foreach (var drone in ship.Drones) WriteDroneState(drone);

        WriteInt(ship.AugmentIds.Count);
        foreach (var aug in ship.AugmentIds) WriteString(aug);
    }

    // ========================================================================
    // Crew
    // ========================================================================

    private void WriteCrewState(CrewState crew, int fmt)
    {
        WriteString(crew.Name);
        WriteString(crew.Race);
        WriteBool(crew.EnemyBoardingDrone);
        WriteInt(crew.Health);
        WriteInt(crew.SpriteX);
        WriteInt(crew.SpriteY);
        WriteInt(crew.RoomId);
        WriteInt(crew.RoomSquare);
        WriteBool(crew.PlayerControlled);

        if (fmt >= 7)
        {
            WriteInt(crew.CloneReady);
            WriteInt(crew.DeathOrder);
            WriteInt(crew.SpriteTintIndices.Count);
            foreach (var tint in crew.SpriteTintIndices) WriteInt(tint);
            WriteBool(crew.MindControlled);
            WriteInt(crew.SavedRoomSquare);
            WriteInt(crew.SavedRoomId);
        }

        WriteInt(crew.PilotSkill);
        WriteInt(crew.EngineSkill);
        WriteInt(crew.ShieldSkill);
        WriteInt(crew.WeaponSkill);
        WriteInt(crew.RepairSkill);
        WriteInt(crew.CombatSkill);
        WriteBool(crew.Male);

        WriteInt(crew.Repairs);
        WriteInt(crew.CombatKills);
        WriteInt(crew.PilotedEvasions);
        WriteInt(crew.JumpsSurvived);
        WriteInt(crew.SkillMasteriesEarned);

        if (fmt >= 7)
        {
            WriteInt(crew.StunTicks);
            WriteInt(crew.HealthBoost);
            WriteInt(crew.ClonebayPriority);
            WriteInt(crew.DamageBoost);
            WriteInt(crew.UnknownLambda);
            WriteInt(crew.UniversalDeathCount);
        }

        if (fmt >= 8)
        {
            WriteBool(crew.PilotMasteryOne);
            WriteBool(crew.PilotMasteryTwo);
            WriteBool(crew.EngineMasteryOne);
            WriteBool(crew.EngineMasteryTwo);
            WriteBool(crew.ShieldMasteryOne);
            WriteBool(crew.ShieldMasteryTwo);
            WriteBool(crew.WeaponMasteryOne);
            WriteBool(crew.WeaponMasteryTwo);
            WriteBool(crew.RepairMasteryOne);
            WriteBool(crew.RepairMasteryTwo);
            WriteBool(crew.CombatMasteryOne);
            WriteBool(crew.CombatMasteryTwo);
        }

        if (fmt >= 7)
        {
            WriteBool(crew.UnknownNu);
            if (crew.TeleportAnim != null) WriteAnimState(crew.TeleportAnim);
            WriteBool(crew.UnknownPhi);
        }

        if (fmt >= 7 && crew.Race == "crystal")
        {
            if (crew.LockdownRechargeTicks != null) WriteInt(crew.LockdownRechargeTicks.Value);
            if (crew.LockdownRechargeTicksGoal != null) WriteInt(crew.LockdownRechargeTicksGoal.Value);
            if (crew.UnknownOmega != null) WriteInt(crew.UnknownOmega.Value);
        }
    }

    // ========================================================================
    // System, Room, Door, etc.
    // ========================================================================

    private void WriteSystemState(SystemState sys, int fmt)
    {
        WriteInt(sys.Capacity);
        if (sys.Capacity == 0) return;

        WriteInt(sys.Power);
        WriteInt(sys.DamagedBars);
        WriteInt(sys.IonizedBars);
        WriteMinMaxedInt(sys.DeionizationTicks);
        WriteInt(sys.RepairProgress);
        WriteInt(sys.DamageProgress);

        if (fmt >= 7)
        {
            WriteInt(sys.BatteryPower);
            WriteInt(sys.HackLevel);
            WriteBool(sys.Hacked);
            WriteInt(sys.TemporaryCapacityCap);
            WriteInt(sys.TemporaryCapacityLoss);
            WriteInt(sys.TemporaryCapacityDivisor);
        }
    }

    private void WriteRoomState(RoomState room, int fmt)
    {
        WriteInt(room.Oxygen);
        foreach (var sq in room.Squares)
        {
            WriteInt(sq.FireHealth);
            WriteInt(sq.IgnitionProgress);
            WriteInt(sq.ExtinguishmentProgress);
        }
        if (fmt >= 7)
        {
            WriteInt(room.StationSquare);
            WriteInt(room.StationDirection);
        }
    }

    private void WriteDoorState(DoorState door, int fmt)
    {
        if (fmt >= 7)
        {
            WriteInt(door.CurrentMaxHealth);
            WriteInt(door.Health);
            WriteInt(door.NominalHealth);
        }
        WriteBool(door.Open);
        WriteBool(door.WalkingThrough);
        if (fmt >= 7)
        {
            WriteInt(door.UnknownDelta);
            WriteInt(door.UnknownEpsilon);
        }
    }

    private void WriteLockdownCrystal(LockdownCrystal c)
    {
        WriteInt(c.CurrentPositionX);
        WriteInt(c.CurrentPositionY);
        WriteInt(c.Speed);
        WriteInt(c.GoalPositionX);
        WriteInt(c.GoalPositionY);
        WriteBool(c.Arrived);
        WriteBool(c.Done);
        WriteInt(c.Lifetime);
        WriteBool(c.SuperFreeze);
        WriteInt(c.LockingRoom);
        WriteInt(c.AnimDirection);
        WriteInt(c.ShardProgress);
    }

    private void WriteDroneState(DroneState drone)
    {
        WriteString(drone.DroneId);
        WriteBool(drone.Armed);
        WriteBool(drone.PlayerControlled);
        WriteInt(drone.BodyX);
        WriteInt(drone.BodyY);
        WriteInt(drone.BodyRoomId);
        WriteInt(drone.BodyRoomSquare);
        WriteInt(drone.Health);
    }

    // ========================================================================
    // Beacon & Store
    // ========================================================================

    private void WriteBeaconState(BeaconState beacon, int fmt)
    {
        WriteInt(beacon.VisitCount);
        if (beacon.VisitCount > 0)
        {
            WriteString(beacon.BgStarscapeImage ?? "");
            WriteString(beacon.BgSpriteImage ?? "");
            WriteInt(beacon.BgSpritePosX ?? 0);
            WriteInt(beacon.BgSpritePosY ?? 0);
            WriteInt(beacon.BgSpriteRotation ?? 0);
        }

        WriteBool(beacon.Seen);
        WriteBool(beacon.EnemyPresent);
        if (beacon.EnemyPresent)
        {
            WriteString(beacon.ShipEventId ?? "");
            WriteString(beacon.AutoBlueprintId ?? "");
            WriteInt(beacon.ShipEventSeed ?? 0);
        }

        WriteInt(beacon.FleetPresence);
        WriteBool(beacon.UnderAttack);
        WriteBool(beacon.StorePresent);
        if (beacon.StorePresent && beacon.Store != null)
            WriteStoreState(beacon.Store, fmt);

        if (fmt >= 8) WriteBool(beacon.UnknownEta);
    }

    private void WriteStoreState(StoreState store, int fmt)
    {
        if (fmt >= 7) WriteInt(store.ShelfCount);
        foreach (var shelf in store.Shelves) WriteStoreShelf(shelf, fmt);
        WriteInt(store.Fuel);
        WriteInt(store.Missiles);
        WriteInt(store.DroneParts);
    }

    private void WriteStoreShelf(StoreShelf shelf, int fmt)
    {
        WriteInt(shelf.ItemType);
        foreach (var item in shelf.Items)
        {
            WriteInt(item.Available);
            if (item.Available >= 0)
            {
                WriteString(item.ItemId ?? "");
                if (fmt >= 8) WriteInt(item.ExtraData ?? 0);
            }
        }
    }

    // ========================================================================
    // Encounter, AI, Environment
    // ========================================================================

    private void WriteEncounterState(EncounterState enc, int fmt)
    {
        WriteInt(enc.ShipEventSeed);
        WriteString(enc.SurrenderEventId);
        WriteString(enc.EscapeEventId);
        WriteString(enc.DestroyedEventId);
        WriteString(enc.DeadCrewEventId);
        WriteString(enc.GotAwayEventId);
        WriteString(enc.LastEventId);
        if (fmt >= 11 && enc.UnknownAlpha != null) WriteInt(enc.UnknownAlpha.Value);
        WriteString(enc.Text);
        WriteInt(enc.AffectedCrewSeed);
        WriteInt(enc.Choices.Count);
        foreach (var choice in enc.Choices) WriteInt(choice);
    }

    private void WriteNearbyShipAI(NearbyShipAIState ai)
    {
        WriteBool(ai.Surrendered);
        WriteBool(ai.Escaping);
        WriteBool(ai.Destroyed);
        WriteInt(ai.SurrenderThreshold);
        WriteInt(ai.EscapeThreshold);
        WriteInt(ai.EscapeTicks);
        WriteBool(ai.StalemateTriggered);
        WriteInt(ai.StalemateTicks);
        WriteInt(ai.BoardingAttempts);
        WriteInt(ai.BoardersNeeded);
    }

    private void WriteEnvironmentState(EnvironmentState env)
    {
        WriteBool(env.RedGiantPresent);
        WriteBool(env.PulsarPresent);
        WriteBool(env.PdsPresent);
        WriteInt(env.VulnerableShips);
        WriteBool(env.AsteroidsPresent);
        if (env.AsteroidField != null)
        {
            WriteInt(env.AsteroidField.UnknownAlpha);
            WriteInt(env.AsteroidField.StrayRockTicks);
            WriteInt(env.AsteroidField.UnknownGamma);
            WriteInt(env.AsteroidField.BgDriftTicks);
            WriteInt(env.AsteroidField.CurrentTarget);
        }
        WriteInt(env.SolarFlareFadeTicks);
        WriteInt(env.HavocTicks);
        WriteInt(env.PdsTicks);
    }

    // ========================================================================
    // Projectiles
    // ========================================================================

    private void WriteProjectileState(ProjectileState proj, int fmt)
    {
        WriteInt(proj.ProjectileType);
        if (proj.ProjectileType == (int)Models.ProjectileType.Invalid) return;

        WriteInt(proj.CurrentPositionX);
        WriteInt(proj.CurrentPositionY);
        WriteInt(proj.PreviousPositionX);
        WriteInt(proj.PreviousPositionY);
        WriteInt(proj.Speed);
        WriteInt(proj.GoalPositionX);
        WriteInt(proj.GoalPositionY);
        WriteInt(proj.Heading);
        WriteInt(proj.OwnerId);
        WriteInt(proj.SelfId);
        WriteDamageState(proj.Damage);
        WriteInt(proj.Lifespan);
        WriteInt(proj.DestinationSpace);
        WriteInt(proj.CurrentSpace);
        WriteInt(proj.TargetId);
        WriteBool(proj.Dead);
        WriteString(proj.DeathAnimId);
        WriteString(proj.FlightAnimId);
        WriteAnimState(proj.DeathAnim);
        WriteAnimState(proj.FlightAnim);
        WriteInt(proj.VelocityX);
        WriteInt(proj.VelocityY);
        WriteBool(proj.Missed);
        WriteBool(proj.HitTarget);
        WriteString(proj.HitSolidSound);
        WriteString(proj.HitShieldSound);
        WriteString(proj.MissSound);
        WriteMinMaxedInt(proj.EntryAngle);
        WriteBool(proj.StartedDying);
        WriteBool(proj.PassedTarget);
        WriteInt(proj.BroadcastType);
        WriteBool(proj.BroadcastTarget);

        if (proj.ExtendedInfo != null)
        {
            switch (proj.ExtendedInfo.Type)
            {
                case "Laser":
                    WriteInt(proj.ExtendedInfo.UnknownAlpha);
                    WriteInt(proj.ExtendedInfo.Spin);
                    break;
                case "Bomb":
                    WriteInt(proj.ExtendedInfo.UnknownAlpha);
                    WriteInt(proj.ExtendedInfo.FuseTicks);
                    WriteInt(proj.ExtendedInfo.UnknownGamma);
                    WriteInt(proj.ExtendedInfo.UnknownDelta);
                    WriteBool(proj.ExtendedInfo.Arrived);
                    break;
                case "Beam":
                    WriteInt(proj.ExtendedInfo.EmissionEndX);
                    WriteInt(proj.ExtendedInfo.EmissionEndY);
                    WriteInt(proj.ExtendedInfo.StrafeSourceX);
                    WriteInt(proj.ExtendedInfo.StrafeSourceY);
                    WriteInt(proj.ExtendedInfo.StrafeEndX);
                    WriteInt(proj.ExtendedInfo.StrafeEndY);
                    WriteInt(proj.ExtendedInfo.UnknownBetaX);
                    WriteInt(proj.ExtendedInfo.UnknownBetaY);
                    WriteInt(proj.ExtendedInfo.SwathEndX);
                    WriteInt(proj.ExtendedInfo.SwathEndY);
                    WriteInt(proj.ExtendedInfo.SwathStartX);
                    WriteInt(proj.ExtendedInfo.SwathStartY);
                    WriteInt(proj.ExtendedInfo.UnknownGamma);
                    WriteInt(proj.ExtendedInfo.SwathLength);
                    WriteInt(proj.ExtendedInfo.UnknownDelta);
                    WriteInt(proj.ExtendedInfo.UnknownEpsilonX);
                    WriteInt(proj.ExtendedInfo.UnknownEpsilonY);
                    WriteInt(proj.ExtendedInfo.UnknownZeta);
                    WriteInt(proj.ExtendedInfo.UnknownEta);
                    WriteInt(proj.ExtendedInfo.EmissionAngle);
                    WriteBool(proj.ExtendedInfo.UnknownIota);
                    WriteBool(proj.ExtendedInfo.UnknownKappa);
                    WriteBool(proj.ExtendedInfo.FromDronePod);
                    WriteBool(proj.ExtendedInfo.UnknownMu);
                    WriteBool(proj.ExtendedInfo.UnknownNu);
                    break;
                case "Pds":
                    if (fmt >= 11)
                    {
                        WriteInt(proj.ExtendedInfo.UnknownAlpha);
                        WriteInt(proj.ExtendedInfo.UnknownBeta);
                        WriteInt(proj.ExtendedInfo.UnknownGamma);
                        WriteInt(proj.ExtendedInfo.UnknownDelta);
                        WriteInt(proj.ExtendedInfo.UnknownEpsilon);
                        if (proj.ExtendedInfo.UnknownZetaAnim != null)
                            WriteAnimState(proj.ExtendedInfo.UnknownZetaAnim);
                    }
                    break;
            }
        }
    }

    // ========================================================================
    // Damage & Animation
    // ========================================================================

    private void WriteDamageState(DamageState d)
    {
        WriteInt(d.HullDamage);
        WriteInt(d.ShieldPiercing);
        WriteInt(d.FireChance);
        WriteInt(d.BreachChance);
        WriteInt(d.IonDamage);
        WriteInt(d.SystemDamage);
        WriteInt(d.PersonnelDamage);
        WriteBool(d.HullBuster);
        WriteInt(d.OwnerId);
        WriteInt(d.SelfId);
        WriteBool(d.Lockdown);
        WriteBool(d.CrystalShard);
        WriteInt(d.StunChance);
        WriteInt(d.StunAmount);
    }

    private void WriteAnimState(AnimState a)
    {
        WriteBool(a.Playing);
        WriteBool(a.Looping);
        WriteInt(a.CurrentFrame);
        WriteInt(a.ProgressTicks);
        WriteInt(a.Scale);
        WriteInt(a.X);
        WriteInt(a.Y);
    }

    // ========================================================================
    // Extended Ship Info
    // ========================================================================

    private void WriteExtendedShipInfo(ExtendedShipInfo ext, ShipState ship, int fmt)
    {
        if (ext.HackingInfo != null)
        {
            WriteInt(ext.HackingInfo.TargetSystemType);
            WriteInt(ext.HackingInfo.UnknownBeta);
            WriteBool(ext.HackingInfo.DronePodVisible);
            WriteInt(ext.HackingInfo.UnknownDelta);
            WriteInt(ext.HackingInfo.DisruptionTicks);
            WriteInt(ext.HackingInfo.DisruptionTicksGoal);
            WriteBool(ext.HackingInfo.Disrupting);
            WriteHackingDronePod(ext.HackingInfo.DronePod);
        }

        if (ext.MindControlInfo != null)
        {
            WriteInt(ext.MindControlInfo.MindControlTicks);
            WriteInt(ext.MindControlInfo.MindControlTicksGoal);
        }

        foreach (var wm in ext.WeaponModules) WriteWeaponModule(wm, fmt);
        foreach (var dm in ext.DroneModules) WriteDroneModule(dm);
    }

    private void WriteWeaponModule(WeaponModule wm, int fmt)
    {
        WriteInt(wm.CooldownTicks);
        WriteInt(wm.CooldownGoal);
        WriteInt(wm.SubcooldownTicks);
        WriteInt(wm.SubcooldownTicksGoal);
        WriteInt(wm.Boost);
        WriteInt(wm.Charge);
        WriteInt(wm.CurrentTargetsCount);
        WriteAnimState(wm.WeaponAnim);
        WriteInt(wm.ProtractAnimTicks);
        WriteBool(wm.Firing);
        WriteBool(wm.FireWhenReady);
        WriteInt(wm.TargetId);
        if (fmt >= 9 && wm.HackAnim != null) WriteAnimState(wm.HackAnim);
        WriteBool(wm.IsOnFire);
        WriteInt(wm.FireId);
        WriteBool(wm.Autofire);
    }

    private void WriteDroneModule(DroneModule dm)
    {
        WriteBool(dm.Deployed);
        WriteBool(dm.Armed);
        if (dm.ExtendedDroneInfo != null)
        {
            WriteInt(dm.ExtendedDroneInfo.BodyX);
            WriteInt(dm.ExtendedDroneInfo.BodyY);
            WriteInt(dm.ExtendedDroneInfo.CurrentSpace);
            WriteInt(dm.ExtendedDroneInfo.DestinationSpace);
            WriteInt(dm.ExtendedDroneInfo.CurrentPositionX);
            WriteInt(dm.ExtendedDroneInfo.CurrentPositionY);
            WriteInt(dm.ExtendedDroneInfo.PreviousPositionX);
            WriteInt(dm.ExtendedDroneInfo.PreviousPositionY);
            WriteInt(dm.ExtendedDroneInfo.GoalPositionX);
            WriteInt(dm.ExtendedDroneInfo.GoalPositionY);
        }
    }

    // ========================================================================
    // Hacking Drone Pod
    // ========================================================================

    private void WriteHackingDronePod(DronePodState pod)
    {
        WriteInt(pod.MourningTicks);
        WriteInt(pod.CurrentSpace);
        WriteInt(pod.DestinationSpace);
        WriteMinMaxedInt(pod.CurrentPositionX);
        WriteMinMaxedInt(pod.CurrentPositionY);
        WriteMinMaxedInt(pod.PreviousPositionX);
        WriteMinMaxedInt(pod.PreviousPositionY);
        WriteMinMaxedInt(pod.GoalPositionX);
        WriteMinMaxedInt(pod.GoalPositionY);
        WriteMinMaxedInt(pod.UnknownEpsilon);
        WriteMinMaxedInt(pod.UnknownZeta);
        WriteMinMaxedInt(pod.NextTargetX);
        WriteMinMaxedInt(pod.NextTargetY);
        WriteMinMaxedInt(pod.UnknownIota);
        WriteMinMaxedInt(pod.UnknownKappa);
        WriteInt(pod.BuildupTicks);
        WriteInt(pod.StationaryTicks);
        WriteInt(pod.CooldownTicks);
        WriteInt(pod.OrbitAngle);
        WriteInt(pod.TurretAngle);
        WriteInt(pod.UnknownXi);
        WriteMinMaxedInt(pod.HopsToLive);
        WriteInt(pod.UnknownPi);
        WriteInt(pod.UnknownRho);
        WriteInt(pod.OverloadTicks);
        WriteInt(pod.UnknownTau);
        WriteInt(pod.UnknownUpsilon);
        WriteInt(pod.DeltaPositionX);
        WriteInt(pod.DeltaPositionY);
        WriteAnimState(pod.DeathAnim);
        // Hacking drone-specific extension
        WriteInt(pod.AttachPositionX);
        WriteInt(pod.AttachPositionY);
        WriteInt(pod.HackUnknownGamma);
        WriteInt(pod.HackUnknownDelta);
        WriteAnimState(pod.LandingAnim);
        WriteAnimState(pod.ExtensionAnim);
    }

    // ========================================================================
    // Rebel Flagship
    // ========================================================================

    private void WriteRebelFlagshipState(RebelFlagshipState fs)
    {
        WriteInt(fs.UnknownAlpha);
        WriteInt(fs.PendingStage);
        WriteInt(fs.UnknownGamma);
        WriteInt(fs.UnknownDelta);
        WriteInt(fs.PreviousOccupancy.Count);
        foreach (var occ in fs.PreviousOccupancy) WriteInt(occ);
    }
}
