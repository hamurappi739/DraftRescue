# Phase 1 Windows API Verification Notes

**Verified:** 2026-08-31 against Microsoft Learn. These are API facts used to constrain experiments; they are not proof that a target is safe/supported.

## SetWinEventHook

Microsoft documents that:

- `SetWinEventHook` can subscribe to events from all processes on the current desktop when process/thread filters are zero;
- `WINEVENT_OUTOFCONTEXT` keeps the callback out of the target process and queues events across process boundaries;
- out-of-context events are delivered on the same thread that registered the hook;
- the registering thread must have a message loop;
- `WINEVENT_SKIPOWNPROCESS` can suppress own-process events;
- the callback lifetime must be retained in managed code and the hook must later be removed.

Source: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook

## Event constants

Microsoft documents:

- `EVENT_SYSTEM_FOREGROUND = 0x0003`, generated when the foreground window changes;
- `EVENT_OBJECT_FOCUS = 0x8005`, an accessibility object focus event.

DraftRescue's baseline experiment uses `EVENT_SYSTEM_FOREGROUND` plus UI Automation's global focus-changed event. `EVENT_OBJECT_FOCUS` is a comparison/fallback experiment only; do not subscribe to broad object-event ranges by default.

Source: https://learn.microsoft.com/en-us/windows/win32/winauto/event-constants

## UI Automation focus events

UI Automation exposes a global focus-changed event and APIs for registering focus, property, and structure event handlers.

Sources:

- https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-eventsoverview
- https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-eventsforclients

## UI Automation property retrieval and caching

Microsoft documents that UI Automation property/control-pattern access can require cross-process calls and that cache requests can retrieve selected properties/patterns in a batch. Therefore DraftRescue uses a narrow audited cache allowlist rather than broad `Current` property probing.

Sources:

- https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomationcacherequest
- https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-cachingforclients

## IsPassword

Microsoft documents `AutomationElement.IsPassword` as a boolean indicating whether the element contains protected content. `true` is therefore a trusted hard deny signal for DraftRescue.

A `false` result is **not** treated by DraftRescue as proof that a field is safe. That stronger fail-closed rule is a DraftRescue product/security decision, not a Microsoft API guarantee.

Source: https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.automationelementinformation.ispassword?view=windowsdesktop-10.0

## AutomationId stability limitation

Microsoft notes that AutomationId can distinguish siblings but is not globally unique and is not guaranteed stable across application releases/builds. DraftRescue therefore treats it as one profile-specific structural signal rather than universal durable identity.

Source: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-usefortesting

## Consequences for implementation

1. Keep all platform calls inside `DraftRescue.Platform.Windows`.
2. Run desktop-wide UIA on the dedicated worker defined in `WINDOWS_PLATFORM_THREADING_SPEC.md`.
3. Keep WinEvent/UIA callbacks content-free and short.
4. Batch only audited Tier A/B properties.
5. Treat provider errors/default/missing values as uncertainty, not safe defaults.
6. Never infer durable field identity from one UIA identifier.
7. Re-run this verification before implementation if the Windows SDK/UIA stack materially changes.
