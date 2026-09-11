# System Lifecycle and Tray UX Specification

**Status:** accepted MVP behavior baseline.

## 1. Background utility model

DraftRescue is a background Windows utility. The main window is a management/recovery surface, not the process lifetime.

Closing the main window hides/closes the window but leaves DraftRescue running in the background.

## 2. Tray icon

When DraftRescue is running, a tray icon provides a minimal explicit presence and exit path.

MVP tray menu:

```text
Open DraftRescue
Settings
──────────────
Quit DraftRescue
```

No live text status, draft snippets, activity counts, or recent-typing menu appears in the tray.

## 3. Tray click

Primary click opens/focuses the DraftRescue Recovery window. If already open, bring it to the foreground without creating a duplicate window.

## 4. Quit

Quit means stop observation/protection until DraftRescue is started again.

If the user explicitly chooses Quit, show a compact confirmation:

```text
Quit DraftRescue?

New drafts won't be protected while DraftRescue is closed.
Existing recoverable drafts will remain until they expire.

[Cancel] [Quit]
```

No manipulative wording.

On confirmed Quit:

1. stop accepting new observations;
2. bounded best-effort checkpoint of already eligible dirty draft state;
3. stop event hooks/platform observers;
4. dispose resources;
5. exit process.

Do not block indefinitely to save the latest characters.

## 5. Start with Windows

MVP setting:

```text
Start DraftRescue when I sign in    [On/Off]
```

Recommended product default: **On after normal installation/first launch**, clearly visible in Settings and easy to disable. The product's core promise otherwise silently disappears after reboot.

Implementation mechanism is deferred to packaging/startup ADR; UI semantics are accepted now.

## 6. App startup

Normal login/startup launch should begin in background without forcing the main window in front of the user.

Explicit user launch from Start menu/shortcut opens the main window.

A command-line/startup marker may distinguish these cases internally; exact packaging mechanism is later work.

## 7. Recovery notification

DraftRescue does not notify on typing/checkpointing.

A notification may be shown when a meaningful recoverable draft becomes newly actionable after a relevant app/context returns, subject to notification deduplication. Baseline copy:

```text
DraftRescue found a draft you can recover.
[Open DraftRescue]
```

No draft text/snippet in OS notification.

Notification behavior is not required for the earliest recovery UI milestone and may ship later.

## 8. No Pause mode in MVP

Do not add a global Pause toggle initially. It introduces ambiguous protection state and support complexity. Users can Quit DraftRescue or disable startup.

Per-app profile enable/disable may be added later without bypassing global privacy rules.

## 9. Process crash

After DraftRescue itself crashes/restarts, only the last successfully committed protected snapshot can be recovered. Product wording must not imply every last character is guaranteed.

## 10. Windows shutdown/logoff

Detailed session-end behavior is defined in `WINDOWS_SESSION_END_AND_RESTART_SPEC.md`: normal periodic checkpoints provide durability; shutdown handlers remain bounded and do not block the user's shutdown intent.
