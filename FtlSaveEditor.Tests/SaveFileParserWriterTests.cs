namespace FtlSaveEditor.Tests;

using System.Linq;
using FtlSaveEditor.Models;
using FtlSaveEditor.SaveFile;
using FtlSaveEditor.Tests.Fixtures;

public class SaveFileParserWriterTests
{
    [Fact]
    public void ScannerRegression_SelectsValidatedWeaponSection()
    {
        var state = FixtureFactory.CreateFullStateWithScannerTrap();
        var writer = new SaveFileWriter();
        var parser = new SaveFileParser();

        byte[] bytes = writer.Write(state);
        var parsed = parser.Parse(bytes);

        Assert.Equal(SaveParseMode.Full, parsed.ParseMode);
        Assert.Equal(EditorCapability.Full, parsed.Capabilities);
        Assert.Equal(2, parsed.PlayerShip.Weapons.Count);
        Assert.Equal("LASER_BURST_2", parsed.PlayerShip.Weapons[0].WeaponId);
        Assert.Equal("ION_STUN", parsed.PlayerShip.Weapons[1].WeaponId);
        Assert.Equal(2, parsed.CargoIdList.Count);
    }

    [Fact]
    public void OpaqueRoomDoorBytes_ArePreservedAcrossRoundTrip()
    {
        var state = FixtureFactory.CreateFullStateWithScannerTrap();
        var writer = new SaveFileWriter();
        var parser = new SaveFileParser();

        byte[] originalBytes = writer.Write(state);
        var parsed = parser.Parse(originalBytes);
        Assert.True(parsed.PlayerShip.OpaqueRoomDoorBytes.SequenceEqual(state.PlayerShip.OpaqueRoomDoorBytes));

        byte[] rewritten = writer.Write(parsed);
        var reparsed = parser.Parse(rewritten);
        Assert.True(reparsed.PlayerShip.OpaqueRoomDoorBytes.SequenceEqual(state.PlayerShip.OpaqueRoomDoorBytes));
    }

    [Fact]
    public void Format11Fallback_ReturnsRestrictedModeWithOpaqueTail()
    {
        byte[] data = FixtureFactory.CreateFormat11DataThatTriggersFallback(out var tailBytes);
        var parser = new SaveFileParser();

        var parsed = parser.Parse(data);

        Assert.Equal(SaveParseMode.RestrictedOpaqueTail, parsed.ParseMode);
        Assert.Equal(EditorCapability.Metadata | EditorCapability.StateVars, parsed.Capabilities);
        Assert.True(parsed.OpaqueTailBytes.SequenceEqual(tailBytes));
        Assert.NotEmpty(parsed.ParseWarnings);
        Assert.NotEmpty(parsed.ParseDiagnostics);
        Assert.False(string.IsNullOrWhiteSpace(parsed.ParseDiagnostics[0].LogPath));
    }

    [Fact]
    public void RestrictedWriter_PreservesTailAndUpdatesHeader()
    {
        byte[] data = FixtureFactory.CreateFormat11DataThatTriggersFallback(out var originalTail);
        var parser = new SaveFileParser();
        var writer = new SaveFileWriter();

        var parsed = parser.Parse(data);
        Assert.Equal(SaveParseMode.RestrictedOpaqueTail, parsed.ParseMode);

        parsed.PlayerShipName = "Edited Restricted";
        parsed.TotalScrapCollected = 9999;
        parsed.StateVars[0].Value = 77;

        byte[] rewritten = writer.Write(parsed);
        Assert.True(rewritten.AsSpan(rewritten.Length - originalTail.Length).SequenceEqual(originalTail));

        var header = FixtureFactory.ReadHeaderSnapshot(rewritten);
        Assert.Equal("Edited Restricted", header.PlayerShipName);
        Assert.Equal(9999, header.TotalScrapCollected);
        Assert.Equal(77, header.StateVars[0].Value);
    }
}
