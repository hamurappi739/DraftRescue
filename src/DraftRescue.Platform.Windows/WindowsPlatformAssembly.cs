namespace DraftRescue.Platform.Windows;

/// <summary>
/// Phase 0 marker. Win32 and UI Automation implementations belong in this assembly
/// in later explicitly approved phases.
/// </summary>
public static class WindowsPlatformAssembly
{
    public static Type MarkerType => typeof(WindowsPlatformAssembly);
}
