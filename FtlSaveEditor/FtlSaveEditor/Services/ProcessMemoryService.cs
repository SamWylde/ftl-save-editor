using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FtlSaveEditor.Services;

/// <summary>
/// Low-level wrapper around kernel32 P/Invoke for reading/writing
/// another process's memory, plus memory scanning.
/// </summary>
public static class ProcessMemoryService
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int VirtualQueryEx(
        IntPtr hProcess, IntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private const uint MEM_COMMIT = 0x1000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_WRITECOPY = 0x08;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
    private const uint PAGE_READONLY = 0x02;
    private const uint PAGE_EXECUTE_READ = 0x20;

    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    /// <summary>
    /// Find the FTL process. Returns null if not running.
    /// </summary>
    public static Process? FindFtlProcess()
    {
        var procs = Process.GetProcessesByName("FTLGame");
        return procs.Length > 0 ? procs[0] : null;
    }

    /// <summary>
    /// Open a process handle with read+write access.
    /// Caller must call CloseProcessHandle when done.
    /// </summary>
    public static IntPtr OpenProcessHandle(int processId)
    {
        return OpenProcess(
            PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION,
            false, processId);
    }

    public static void CloseProcessHandle(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
            CloseHandle(handle);
    }

    /// <summary>
    /// Read a 4-byte integer from the target process at the given address.
    /// </summary>
    public static int ReadInt32(IntPtr processHandle, IntPtr address)
    {
        byte[] buffer = new byte[4];
        ReadProcessMemory(processHandle, address, buffer, 4, out _);
        return BitConverter.ToInt32(buffer, 0);
    }

    /// <summary>
    /// Write a 4-byte integer to the target process at the given address.
    /// </summary>
    public static bool WriteInt32(IntPtr processHandle, IntPtr address, int value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteProcessMemory(processHandle, address, buffer, 4, out _);
    }

    /// <summary>
    /// Read a block of bytes from the target process.
    /// </summary>
    public static byte[] ReadBytes(IntPtr processHandle, IntPtr address, int count)
    {
        byte[] buffer = new byte[count];
        ReadProcessMemory(processHandle, address, buffer, count, out _);
        return buffer;
    }

    /// <summary>
    /// Follow a pointer chain: start at baseAddress, read the pointer stored there,
    /// add each successive offset, and read again. Returns the final resolved address.
    /// </summary>
    public static IntPtr ResolvePointerChain(
        IntPtr processHandle, IntPtr baseAddress, params int[] offsets)
    {
        IntPtr current = baseAddress;
        for (int i = 0; i < offsets.Length; i++)
        {
            int value = ReadInt32(processHandle, current);
            current = (IntPtr)(value + offsets[i]);
        }
        return current;
    }

    /// <summary>
    /// Get the base address of the main module for a process.
    /// </summary>
    public static IntPtr GetModuleBaseAddress(Process process)
    {
        try
        {
            return process.MainModule?.BaseAddress ?? IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Array-of-bytes pattern scan within a memory region.
    /// Wildcard bytes are represented as null in the pattern.
    /// Returns the address of the first match, or IntPtr.Zero.
    /// </summary>
    public static IntPtr AoBScan(
        IntPtr processHandle, IntPtr startAddress, int regionSize, byte?[] pattern)
    {
        byte[] memory = ReadBytes(processHandle, startAddress, regionSize);

        for (int i = 0; i <= memory.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (pattern[j] is byte expected && memory[i + j] != expected)
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return (IntPtr)((long)startAddress + i);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Scan all readable memory regions of a process for a 4-byte int32 value.
    /// Returns all addresses where the value was found.
    /// Should be called from a background thread.
    /// </summary>
    public static List<IntPtr> ScanForInt32(IntPtr processHandle, int targetValue)
    {
        var results = new List<IntPtr>();
        byte[] targetBytes = BitConverter.GetBytes(targetValue);
        long address = 0;
        int mbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (address < 0x7FFFFFFF) // 32-bit address space (FTL is 32-bit)
        {
            int queryResult = VirtualQueryEx(processHandle, (IntPtr)address,
                out MEMORY_BASIC_INFORMATION mbi, mbiSize);
            if (queryResult == 0) break;

            long regionSize = (long)mbi.RegionSize;
            if (regionSize <= 0) break;

            if (mbi.State == MEM_COMMIT && IsReadableProtection(mbi.Protect) && regionSize <= 100_000_000)
            {
                try
                {
                    byte[] buffer = new byte[regionSize];
                    if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, (int)regionSize, out int bytesRead)
                        && bytesRead >= 4)
                    {
                        for (int i = 0; i <= bytesRead - 4; i += 4) // align to 4 bytes
                        {
                            if (buffer[i] == targetBytes[0] &&
                                buffer[i + 1] == targetBytes[1] &&
                                buffer[i + 2] == targetBytes[2] &&
                                buffer[i + 3] == targetBytes[3])
                            {
                                results.Add((IntPtr)((long)mbi.BaseAddress + i));
                            }
                        }
                    }
                }
                catch
                {
                    // Skip unreadable regions
                }
            }

            address = (long)mbi.BaseAddress + regionSize;
        }

        return results;
    }

    /// <summary>
    /// Scan all readable memory regions for multiple int32 values in a single pass.
    /// Returns a dictionary mapping each target value to the list of addresses where it was found.
    /// Much more efficient than calling ScanForInt32 N times.
    /// Should be called from a background thread.
    /// </summary>
    public static Dictionary<int, List<IntPtr>> ScanForMultipleInt32(
        IntPtr processHandle, int[] targetValues)
    {
        var results = new Dictionary<int, List<IntPtr>>();
        foreach (int val in targetValues)
        {
            if (!results.ContainsKey(val))
                results[val] = new List<IntPtr>();
        }

        long address = 0;
        int mbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

        while (address < 0x7FFFFFFF)
        {
            int queryResult = VirtualQueryEx(processHandle, (IntPtr)address,
                out MEMORY_BASIC_INFORMATION mbi, mbiSize);
            if (queryResult == 0) break;

            long regionSize = (long)mbi.RegionSize;
            if (regionSize <= 0) break;

            if (mbi.State == MEM_COMMIT && IsReadableProtection(mbi.Protect)
                && regionSize <= 100_000_000)
            {
                try
                {
                    byte[] buffer = new byte[regionSize];
                    if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer,
                        (int)regionSize, out int bytesRead) && bytesRead >= 4)
                    {
                        for (int i = 0; i <= bytesRead - 4; i += 4)
                        {
                            int memVal = BitConverter.ToInt32(buffer, i);
                            if (results.ContainsKey(memVal))
                            {
                                results[memVal].Add(
                                    (IntPtr)((long)mbi.BaseAddress + i));
                            }
                        }
                    }
                }
                catch { }
            }

            address = (long)mbi.BaseAddress + regionSize;
        }

        return results;
    }

    /// <summary>
    /// Refine a previous scan: read each candidate address and keep only those
    /// that now contain the new target value.
    /// </summary>
    public static List<IntPtr> RefineScan(IntPtr processHandle, List<IntPtr> candidates, int newTargetValue)
    {
        var refined = new List<IntPtr>();
        foreach (var addr in candidates)
        {
            try
            {
                int current = ReadInt32(processHandle, addr);
                if (current == newTargetValue)
                    refined.Add(addr);
            }
            catch
            {
                // Address no longer readable, skip
            }
        }
        return refined;
    }

    private static bool IsReadableProtection(uint protect)
    {
        return protect is PAGE_READWRITE or PAGE_WRITECOPY
            or PAGE_EXECUTE_READWRITE or PAGE_EXECUTE_WRITECOPY
            or PAGE_READONLY or PAGE_EXECUTE_READ;
    }
}
