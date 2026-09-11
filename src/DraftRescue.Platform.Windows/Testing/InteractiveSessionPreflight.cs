using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DraftRescue.Platform.Windows.Testing;

/// <summary>
/// Content-free diagnostics for deciding whether a WPF fixture can be validated
/// from the current Windows interactive desktop. This is test infrastructure only;
/// production observation never activates windows and never reads window content.
/// </summary>
public sealed record InteractiveSessionPreflightResult(
    bool UserInteractive,
    int CurrentProcessId,
    int CurrentSessionId,
    bool ForegroundWindowPresent,
    uint ForegroundProcessId,
    int? ForegroundProcessSessionId,
    bool ForegroundSameSession,
    bool InputDesktopAccessible,
    bool ForegroundFocusWindowPresent,
    uint ForegroundThreadId,
    bool ReadyForInteractiveFixture)
{
    public static InteractiveSessionPreflightResult Capture()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var currentProcessId = currentProcess.Id;
        var currentSessionId = currentProcess.SessionId;
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var foregroundPresent = foregroundWindow != nint.Zero;
        uint foregroundProcessId = 0;
        var foregroundThreadId = foregroundPresent
            ? NativeMethods.GetWindowThreadProcessId(foregroundWindow, out foregroundProcessId)
            : 0u;

        if (!foregroundPresent)
        {
            foregroundProcessId = 0;
        }

        int? foregroundSessionId = null;
        if (foregroundProcessId != 0)
        {
            try
            {
                using var foregroundProcess = Process.GetProcessById((int)foregroundProcessId);
                foregroundSessionId = foregroundProcess.SessionId;
            }
            catch (ArgumentException)
            {
                // The foreground process may exit between the Win32 query and the
                // managed lookup. Keep the result fail-closed and content-free.
            }
            catch (InvalidOperationException)
            {
                // Access can be denied or the process can terminate; no exception
                // details are persisted in the evidence record.
            }
        }

        var inputDesktopAccessible = NativeMethods.TryOpenInputDesktop(out var inputDesktop);
        if (inputDesktop != nint.Zero)
        {
            NativeMethods.CloseDesktop(inputDesktop);
        }

        var focusWindowPresent = false;
        if (foregroundThreadId != 0)
        {
            var threadInfo = new NativeMethods.GuiThreadInfo
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
            };
            focusWindowPresent = NativeMethods.GetGUIThreadInfo(foregroundThreadId, ref threadInfo) &&
                threadInfo.hwndFocus != nint.Zero;
        }

        var userInteractive = Environment.UserInteractive;
        var foregroundSameSession = foregroundSessionId == currentSessionId;
        var ready = userInteractive &&
            foregroundPresent &&
            foregroundSameSession &&
            inputDesktopAccessible &&
            focusWindowPresent;

        return new InteractiveSessionPreflightResult(
            userInteractive,
            currentProcessId,
            currentSessionId,
            foregroundPresent,
            foregroundProcessId,
            foregroundSessionId,
            foregroundSameSession,
            inputDesktopAccessible,
            focusWindowPresent,
            foregroundThreadId,
            ready);
    }

    private static class NativeMethods
    {
        private const uint DesktopReadObjects = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        internal struct GuiThreadInfo
        {
            internal uint cbSize;
            internal uint flags;
            internal nint hwndActive;
            internal nint hwndFocus;
            internal nint hwndCapture;
            internal nint hwndMenuOwner;
            internal nint hwndMoveSize;
            internal nint hwndCaret;
        }

        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo threadInfo);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint desiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseDesktop(nint desktopHandle);

        internal static bool TryOpenInputDesktop(out nint desktopHandle)
        {
            desktopHandle = OpenInputDesktop(0, false, DesktopReadObjects);
            return desktopHandle != nint.Zero;
        }
    }
}
