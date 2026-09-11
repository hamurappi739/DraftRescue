namespace DraftRescue.Application.Models;

/// <summary>
/// Bounded, content-free metadata for the currently focused UI Automation element.
/// </summary>
public sealed record FocusedElementMetadata(
    int ProcessId,
    nint NativeWindowHandle,
    UiaControlKind ControlKind,
    UiaFrameworkKind FrameworkKind,
    UiaBooleanSignal IsEditable,
    UiaBooleanSignal IsReadOnly,
    UiaBooleanSignal IsPassword,
    UiaBooleanSignal IsEnabled,
    UiaBooleanSignal IsKeyboardFocusable,
    UiaBooleanSignal HasKeyboardFocus,
    UiaBooleanSignal IsOffscreen,
    string? AutomationIdToken,
    string? ClassNameToken,
    UiaCapabilityHints CapabilityHints)
{
    /// <summary>
    /// Token integrity relationship. It is structural metadata only and never authorizes a content read.
    /// </summary>
    public IntegrityCompatibility IntegrityCompatibility { get; init; } = IntegrityCompatibility.Unknown;
}
