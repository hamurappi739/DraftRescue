namespace DraftRescue.Application.Models;

public enum UiaBooleanSignal
{
    Unknown = 0,
    False = 1,
    True = 2
}

public enum UiaControlKind
{
    Unknown = 0,
    Edit = 1,
    Document = 2,
    Pane = 3,
    Window = 4,
    Button = 5,
    Menu = 6,
    Dialog = 7,
    Other = 8
}

public enum UiaFrameworkKind
{
    Unknown = 0,
    Win32 = 1,
    Wpf = 2,
    WinUi = 3,
    Chromium = 4,
    Avalonia = 5,
    Other = 6
}

[Flags]
public enum UiaCapabilityHints
{
    None = 0,
    ValuePatternAvailable = 1,
    TextPatternAvailable = 2,
    LegacyPatternAvailable = 4
}
