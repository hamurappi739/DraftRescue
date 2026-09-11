# Startup Registration Specification

**Status:** accepted semantics; mechanism selection deferred to packaging ADR.

## 1. Product behavior

Setting:

```text
Start DraftRescue when I sign in    [On/Off]
```

Recommended default after normal first-run completion: On.

Turning it Off must remove/disable DraftRescue's own startup registration. It must not affect unrelated startup entries.

## 2. Startup launch mode

Automatic launch uses an internal background marker, for example conceptually `--background-startup`.

Requirements:

- no main window pop-up;
- no recovery preview shown automatically;
- initialize repository/retention/observation;
- tray icon appears according to lifecycle design;
- no text is placed in command-line arguments.

Explicit Start-menu/shortcut launch opens/focuses the main window.

## 3. Single instance

Only one active DraftRescue observer/composition root may run per interactive user session.

A second explicit launch should signal the existing instance to open/focus its window, not create a second capture pipeline.

The single-instance IPC channel must not carry draft body text unless a later reviewed design explicitly requires it. Baseline: commands only (`OpenWindow`, `OpenSettings`, `Quit`).

## 4. Mechanism options

Mechanism depends on package model:

- packaged/MSIX startup facilities where appropriate;
- per-user Windows startup registration such as HKCU Run only if chosen by ADR for an unpackaged installer;
- avoid machine-wide startup unless there is a proven requirement.

Do not use scheduled tasks as a hidden fallback merely to bypass normal user startup controls.

## 5. User control

Windows may expose startup controls in Settings/Task Manager. DraftRescue must tolerate the OS/user disabling startup externally.

The app's setting must reconcile actual registration state on next Settings open rather than assuming its cached value is authoritative.

## 6. Failures

Failure to register startup:

- does not fail installation/application startup;
- is shown as a settings-level structural error if user explicitly toggled it;
- logs no sensitive content.

## 7. Tests

- enable/disable idempotently;
- external disable then settings refresh;
- login launch stays background;
- explicit launch opens UI;
- duplicate launch activates existing instance;
- malformed/old startup entry repaired only if it belongs to DraftRescue;
- uninstall removes DraftRescue registration;
- command line contains no draft metadata.
