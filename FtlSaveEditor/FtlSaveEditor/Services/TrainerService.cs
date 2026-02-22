using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace FtlSaveEditor.Services;

/// <summary>
/// Represents a single trainable game value with freeze support.
/// </summary>
public class TrainedValue : INotifyPropertyChanged
{
    public string Name { get; }
    public string Description { get; }

    private int _currentValue;
    public int CurrentValue
    {
        get => _currentValue;
        set { if (_currentValue != value) { _currentValue = value; OnPropertyChanged(); } }
    }

    private int _desiredValue;
    public int DesiredValue
    {
        get => _desiredValue;
        set { if (_desiredValue != value) { _desiredValue = value; OnPropertyChanged(); } }
    }

    private bool _isFrozen;
    public bool IsFrozen
    {
        get => _isFrozen;
        set { if (_isFrozen != value) { _isFrozen = value; OnPropertyChanged(); } }
    }

    public IntPtr ResolvedAddress { get; set; }
    public bool IsResolved => ResolvedAddress != IntPtr.Zero;

    public TrainedValue(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>
/// Known FTL version profiles with their memory offsets.
/// </summary>
public class FtlVersionProfile
{
    public string VersionName { get; init; } = "";
    public int BaseOffset { get; init; }
    public int HullOffset { get; init; }
    public int FuelOffset { get; init; }
    public int DronePartsOffset { get; init; }
    public int MissilesOffset { get; init; }
    public int ScrapOffset { get; init; }
    public bool MissilesUsePointerChain { get; init; }
    public int[] MissilesPointerChain { get; init; } = [];
}

public class TrainerService : INotifyPropertyChanged
{
    public static TrainerService Instance { get; } = new();

    private IntPtr _processHandle;
    private Process? _process;
    private DispatcherTimer? _refreshTimer;
    private FtlVersionProfile? _activeProfile;

    private bool _isAttached;
    public bool IsAttached
    {
        get => _isAttached;
        private set { _isAttached = value; OnPropertyChanged(); }
    }

    private string _statusText = "Not attached — launch FTL and click Attach";
    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    private string _detectedVersion = "";
    public string DetectedVersion
    {
        get => _detectedVersion;
        private set { _detectedVersion = value; OnPropertyChanged(); }
    }

    private int _refreshIntervalMs = 250;
    public int RefreshIntervalMs
    {
        get => _refreshIntervalMs;
        set
        {
            if (_refreshIntervalMs != value)
            {
                _refreshIntervalMs = value;
                OnPropertyChanged();
                if (_refreshTimer != null)
                {
                    _refreshTimer.Interval = TimeSpan.FromMilliseconds(value);
                }
            }
        }
    }

    public TrainedValue Hull { get; } = new("Hull", "Ship hull points");
    public TrainedValue Scrap { get; } = new("Scrap", "Currency for stores");
    public TrainedValue Fuel { get; } = new("Fuel", "Jumps remaining");
    public TrainedValue Missiles { get; } = new("Missiles", "Missile ammunition");
    public TrainedValue DroneParts { get; } = new("Drone Parts", "Drone deployment parts");

    public TrainedValue[] AllValues => [Hull, Scrap, Fuel, Missiles, DroneParts];

    // Known version profiles — these offsets need empirical verification.
    // Starting with community-documented values for vanilla FTL 1.6.x.
    private static readonly FtlVersionProfile[] KnownProfiles =
    [
        new FtlVersionProfile
        {
            VersionName = "FTL 1.6.9+ (Steam/GOG)",
            BaseOffset = 0x8E3500,
            HullOffset = 0x5F0,
            FuelOffset = 0x700,
            ScrapOffset = 0x760,
            DronePartsOffset = 0xBF0,
            MissilesUsePointerChain = true,
            MissilesPointerChain = [0x88, 0x2B8],
        },
    ];

    public bool TryAttach()
    {
        var proc = ProcessMemoryService.FindFtlProcess();
        if (proc == null)
        {
            StatusText = "FTL is not running";
            return false;
        }

        _process = proc;
        _processHandle = ProcessMemoryService.OpenProcessHandle(proc.Id);

        if (_processHandle == IntPtr.Zero)
        {
            StatusText = "Failed to open process — try running as administrator";
            return false;
        }

        bool resolved = TryResolveAddresses();

        IsAttached = true;
        if (resolved)
        {
            StatusText = $"Attached to FTL (PID {proc.Id}) — addresses resolved";
        }
        else
        {
            StatusText = $"Attached to FTL (PID {proc.Id}) — could not auto-detect addresses (use manual override)";
            DetectedVersion = "Unknown version";
        }

        StartRefreshTimer();
        return true;
    }

    public void Detach()
    {
        StopRefreshTimer();
        ProcessMemoryService.CloseProcessHandle(_processHandle);
        _processHandle = IntPtr.Zero;
        _process = null;
        _activeProfile = null;
        IsAttached = false;
        DetectedVersion = "";
        StatusText = "Detached";

        foreach (var v in AllValues)
        {
            v.IsFrozen = false;
            v.ResolvedAddress = IntPtr.Zero;
            v.CurrentValue = 0;
        }
    }

    public void WriteValue(TrainedValue value)
    {
        if (!IsAttached || !value.IsResolved) return;
        try
        {
            ProcessMemoryService.WriteInt32(_processHandle, value.ResolvedAddress, value.DesiredValue);
            value.CurrentValue = value.DesiredValue;
        }
        catch
        {
            StatusText = $"Failed to write {value.Name}";
        }
    }

    public void MaxAllResources()
    {
        foreach (var v in AllValues)
        {
            v.DesiredValue = v.Name == "Hull" ? 30 : 999;
            WriteValue(v);
        }
    }

    public void UnfreezeAll()
    {
        foreach (var v in AllValues)
            v.IsFrozen = false;
    }

    private bool TryResolveAddresses()
    {
        if (_process == null) return false;

        var moduleBase = ProcessMemoryService.GetModuleBaseAddress(_process);
        if (moduleBase == IntPtr.Zero) return false;

        foreach (var profile in KnownProfiles)
        {
            try
            {
                var gameStateAddr = (IntPtr)((long)moduleBase + profile.BaseOffset);

                // Sanity check: read scrap and fuel, validate they're in reasonable ranges
                int testScrap = ProcessMemoryService.ReadInt32(
                    _processHandle, (IntPtr)((long)gameStateAddr + profile.ScrapOffset));
                int testFuel = ProcessMemoryService.ReadInt32(
                    _processHandle, (IntPtr)((long)gameStateAddr + profile.FuelOffset));

                if (testScrap >= 0 && testScrap < 100000 && testFuel >= 0 && testFuel < 1000)
                {
                    _activeProfile = profile;
                    DetectedVersion = profile.VersionName;

                    Hull.ResolvedAddress = (IntPtr)((long)gameStateAddr + profile.HullOffset);
                    Fuel.ResolvedAddress = (IntPtr)((long)gameStateAddr + profile.FuelOffset);
                    Scrap.ResolvedAddress = (IntPtr)((long)gameStateAddr + profile.ScrapOffset);
                    DroneParts.ResolvedAddress = (IntPtr)((long)gameStateAddr + profile.DronePartsOffset);

                    if (profile.MissilesUsePointerChain)
                    {
                        Missiles.ResolvedAddress = ProcessMemoryService.ResolvePointerChain(
                            _processHandle, gameStateAddr, profile.MissilesPointerChain);
                    }
                    else
                    {
                        Missiles.ResolvedAddress = (IntPtr)((long)gameStateAddr + profile.MissilesOffset);
                    }

                    return true;
                }
            }
            catch
            {
                // Profile didn't work, try next
            }
        }

        return false;
    }

    private void StartRefreshTimer()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_refreshIntervalMs)
        };
        _refreshTimer.Tick += OnRefreshTick;
        _refreshTimer.Start();
    }

    private void StopRefreshTimer()
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        if (!IsAttached || _processHandle == IntPtr.Zero) return;

        // Check if process is still alive
        if (_process == null || _process.HasExited)
        {
            Detach();
            StatusText = "FTL process exited";
            return;
        }

        foreach (var v in AllValues)
        {
            if (!v.IsResolved) continue;

            try
            {
                if (v.IsFrozen)
                {
                    ProcessMemoryService.WriteInt32(_processHandle, v.ResolvedAddress, v.DesiredValue);
                    v.CurrentValue = v.DesiredValue;
                }
                else
                {
                    v.CurrentValue = ProcessMemoryService.ReadInt32(_processHandle, v.ResolvedAddress);
                }
            }
            catch
            {
                // Memory read/write failed — address may have shifted
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
