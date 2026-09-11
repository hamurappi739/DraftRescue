using System.Runtime.InteropServices;
using DraftRescue.Application.Models;

namespace DraftRescue.Platform.Windows.Security;

/// <summary>
/// Reads only process-token integrity levels. It never opens UIA content or elevates the caller.
/// </summary>
public static class ProcessIntegrityCompatibilityReader
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenIntegrityLevel = 25;

    public static IntegrityCompatibility Compare(int targetProcessId, int ownProcessId)
    {
        if (targetProcessId <= 0 || ownProcessId <= 0 ||
            !TryReadIntegrityRid(targetProcessId, out var targetRid) ||
            !TryReadIntegrityRid(ownProcessId, out var ownRid))
        {
            return IntegrityCompatibility.Unknown;
        }

        return targetRid <= ownRid
            ? IntegrityCompatibility.Compatible
            : IntegrityCompatibility.Incompatible;
    }

    private static bool TryReadIntegrityRid(int processId, out uint rid)
    {
        rid = 0;
        var process = NativeMethods.OpenProcess(ProcessQueryLimitedInformation, false, (uint)processId);
        if (process == 0)
        {
            return false;
        }

        try
        {
            if (!NativeMethods.OpenProcessToken(process, TokenQuery, out var token) || token == 0)
            {
                return false;
            }

            try
            {
                if (!NativeMethods.GetTokenInformation(token, TokenIntegrityLevel, IntPtr.Zero, 0, out var required) || required <= 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != NativeMethods.ErrorInsufficientBuffer || required <= 0)
                    {
                        return false;
                    }
                }

                var buffer = Marshal.AllocHGlobal((int)required);
                try
                {
                    if (!NativeMethods.GetTokenInformation(token, TokenIntegrityLevel, buffer, required, out _))
                    {
                        return false;
                    }

                    var sid = Marshal.ReadIntPtr(buffer);
                    if (sid == IntPtr.Zero)
                    {
                        return false;
                    }

                    var count = Marshal.ReadByte(NativeMethods.GetSidSubAuthorityCount(sid));
                    if (count == 0)
                    {
                        return false;
                    }

                    var subAuthority = NativeMethods.GetSidSubAuthority(sid, (uint)(count - 1));
                    if (subAuthority == IntPtr.Zero)
                    {
                        return false;
                    }

                    rid = (uint)Marshal.ReadInt32(subAuthority);
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(token);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static class NativeMethods
    {
        internal const int ErrorInsufficientBuffer = 122;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(nint tokenHandle, int tokenInformationClass, nint tokenInformation, int tokenInformationLength, out int returnLength);

        [DllImport("advapi32.dll")]
        internal static extern nint GetSidSubAuthorityCount(nint sid);

        [DllImport("advapi32.dll")]
        internal static extern nint GetSidSubAuthority(nint sid, uint subAuthorityIndex);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(nint handle);
    }
}
