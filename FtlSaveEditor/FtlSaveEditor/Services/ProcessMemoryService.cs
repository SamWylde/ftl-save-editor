using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FtlSaveEditor.Services;

/// <summary>
/// Low-level wrapper around kernel32 P/Invoke for reading/writing
/// another process's memory.
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
}
