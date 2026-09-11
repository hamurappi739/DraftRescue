# Windows API Research Notes

**Status:** externally verified research notes, not a substitute for per-app experiments.

## Foreground window

Microsoft documents `GetForegroundWindow` as returning the handle of the window with which the user is currently working. It may return null in some transition circumstances. DraftRescue must therefore treat it as an observation snapshot, not an infallible permanent identity.

## WinEvent hooks

Microsoft documents `SetWinEventHook` for subscribing to a range of accessibility/system events. With `WINEVENT_OUTOFCONTEXT`, the callback is not injected into generating processes; events are queued across process boundaries. The registering thread needs a message loop. `WINEVENT_SKIPOWNPROCESS` can exclude DraftRescue's own process.

Design consequence: Phase 1 should test narrow foreground/focus event subscriptions rather than keyboard hooks and must own hook lifetime/message-loop cleanup explicitly.

## UI Automation protected content

UI Automation exposes `AutomationElementInformation.IsPassword`, which indicates protected content. DraftRescue treats `true` as an immediate deny. The property only proves a protected-content signal when true; a false value does not establish that an arbitrary field is safe from credential/banking/private-context policy.

## Text patterns

Microsoft UI Automation distinguishes Value and Text patterns. Documentation notes that simple edit controls may expose `ValuePattern`, while multi-line text controls may expose `TextPattern`; TextPattern is primarily for textual content/ranges and is not itself a universal text insertion API.

Design consequence: read/write capability discovery is per control family. Restore must not assume the same pattern that reads content can write content.

## Windows DPAPI / ProtectedData

Microsoft documents `System.Security.Cryptography.ProtectedData` as a wrapper around Windows DPAPI. `DataProtectionScope.CurrentUser` binds protected data to the current user context. DraftRescue uses this as the MVP Windows protection baseline behind `IDraftProtector`.

## Research discipline

These API facts do not prove Chrome, Edge, Electron, Notepad, WPF, WinUI, or other application behavior. Each supported target still requires the experiments defined by `WINDOWS_UIA_CAPABILITY_MATRIX.md` and `BROWSER_DETECTION_RESEARCH_MATRIX.md`.

## UI Automation threading

Microsoft's Windows UI Automation threading guidance recommends desktop-wide UI Automation client calls from a separate non-UI MTA thread and warns that calling UIA against desktop elements from the application's UI thread can cause severe slowness or hangs. Event subscription/removal also needs disciplined non-UI-thread ownership. DraftRescue therefore adopts ADR 0016.

## Windows session end / shutdown

Microsoft documents `WM_QUERYENDSESSION` as the pre-session-end query and recommends returning promptly, deferring cleanup until `WM_ENDSESSION`. Microsoft shutdown performance guidance notes that unresponsive GUI applications can be force-terminated after the shutdown timeout. DraftRescue therefore treats session-end as a bounded finalization path, not its primary durability mechanism (ADR 0017).

Microsoft also recommends applications save state periodically rather than relying on shutdown-time work. This matches DraftRescue's current-snapshot checkpoint model.

## Restart Manager

Microsoft documents `RegisterApplicationRestart` for registering the command line Windows/Restart Manager may use to restart an application. DraftRescue may evaluate this for managed update/restart scenarios; any restart arguments must remain content-free.

## Startup apps

Microsoft documents common Win32 startup mechanisms including Run/RunOnce keys and Startup folders, and Windows exposes user controls for startup applications. DraftRescue's final registration mechanism is packaging-dependent, but it must remain per-user, visible/manageable, and reconcilable with OS state.

## Packaging / MSIX

Microsoft's current Windows packaging guidance supports packaging classic desktop/.NET applications with MSIX and App Installer-based update flows. This is a preferred direction to validate, not yet a locked implementation choice.

### Research URLs (verified 2026-08-31)

- https://learn.microsoft.com/windows/win32/shutdown/wm-queryendsession
- https://learn.microsoft.com/windows/win32/shutdown/shutting-down
- https://learn.microsoft.com/windows/win32/rstmgr/guidelines-for-applications
- https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-registerapplicationrestart
- https://learn.microsoft.com/windows/win32/w8cookbook/startup-apps
- https://learn.microsoft.com/windows/win32/setupapi/run-and-runonce-registry-keys
- https://learn.microsoft.com/windows/apps/package-and-deploy/packaging/
- https://learn.microsoft.com/windows/msix/app-package-updates
