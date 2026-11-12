using System.Runtime.InteropServices;

namespace LukkyBoost.App.Core.Win32;

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(
        out ulong lpIdleTime,
        out ulong lpKernelTime,
        out ulong lpUserTime);

    [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DnsFlushResolverCache();

    [Flags]
    internal enum RecycleBinFlags : uint
    {
        SHERB_NOCONFIRMATION = 0x00000001,
        SHERB_NOPROGRESSUI   = 0x00000002,
        SHERB_NOSOUND        = 0x00000004
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int SHEmptyRecycleBinW(
        IntPtr hwnd,
        string? pszRootPath,
        RecycleBinFlags dwFlags);

    // Reinicio del subsistema de video en caliente: Win + Ctrl + Shift + B
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_LWIN    = 0x5B;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_SHIFT   = 0x10;
    private const ushort VK_B       = 0x42;

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    internal static bool SendWinCtrlShiftB()
    {
        var inputs = new INPUT[]
        {
            CreateKey(VK_LWIN, false),
            CreateKey(VK_CONTROL, false),
            CreateKey(VK_SHIFT, false),
            CreateKey(VK_B, false),
            CreateKey(VK_B, true),
            CreateKey(VK_SHIFT, true),
            CreateKey(VK_CONTROL, true),
            CreateKey(VK_LWIN, true)
        };

        uint dispatched = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return dispatched == inputs.Length;
    }

    private static INPUT CreateKey(ushort vk, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };
}

