using System.Runtime.InteropServices;

namespace DraftRescue.Platform.Windows.Testing;

/// <summary>
/// Test-only foreground activation for the managed WPF fixture. Production observation
/// paths never call this helper and never attempt to activate a user's window.
/// </summary>
public static class SyntheticWindowActivator
{
    public static bool TryActivate(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        NativeMethods.AllowSetForegroundWindow(NativeMethods.AllowSetForegroundWindowAnyProcess);
        var currentThreadId = NativeMethods.GetCurrentThreadId();
        var foregroundThreadId = NativeMethods.GetWindowThreadProcessId(
            NativeMethods.GetForegroundWindow(),
            out _);
        var attached = foregroundThreadId != 0 && foregroundThreadId != currentThreadId &&
            NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
        try
        {
            NativeMethods.ShowWindow(windowHandle, NativeMethods.ShowWindowRestore);
            NativeMethods.BringWindowToTop(windowHandle);
            NativeMethods.SetActiveWindow(windowHandle);
            return NativeMethods.SetForegroundWindow(windowHandle);
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    /// <summary>
    /// Test-only verification that the supplied synthetic fixture handle is the
    /// current foreground window. This reads only a window handle; it never reads
    /// title, control text, clipboard data, or any other target content.
    /// </summary>
    public static bool IsForeground(nint windowHandle)
    {
        return windowHandle != nint.Zero && NativeMethods.GetForegroundWindow() == windowHandle;
    }

    private static class NativeMethods
    {
        internal const int AllowSetForegroundWindowAnyProcess = -1;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(nint windowHandle);

        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BringWindowToTop(nint windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint SetActiveWindow(nint windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(nint windowHandle, int command);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AttachThreadInput(uint sourceThreadId, uint targetThreadId, bool attach);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        internal const int ShowWindowRestore = 9;
    }
}
