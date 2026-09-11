# Windows Session End, Shutdown, Restart, and Crash Specification

**Status:** accepted behavioral baseline; Win32 implementation deferred.

## 1. Goal

DraftRescue should survive normal application loss scenarios without pretending shutdown callbacks are a reliable last-second save mechanism.

Primary durability comes from periodic bounded checkpoints during normal operation. Shutdown handling is a best-effort finalization path only.

## 2. Windows session-end behavior

A GUI process may receive `WM_QUERYENDSESSION` followed by `WM_ENDSESSION` during logoff/shutdown. DraftRescue must respond promptly and must not block shutdown merely to preserve the last few characters.

Policy:

1. `WM_QUERYENDSESSION`: return consent promptly; do not perform expensive UI Automation, database migration, network work, or user prompts.
2. `WM_ENDSESSION` with confirmed end: stop accepting new work, cancel nonessential observation, perform only bounded best-effort checkpoint/final DB flush of already eligible in-memory state, then exit.
3. forced termination/power loss may skip both; correctness must still hold from the last committed snapshot.

## 3. No shutdown blocker

DraftRescue does not use `ShutdownBlockReasonCreate` for ordinary draft protection. The product must respect the user's shutdown/restart intent.

## 4. Crash semantics

If DraftRescue crashes:

- plaintext that existed only in volatile memory may be lost;
- last atomically committed protected snapshot remains recoverable;
- no recovery from debug dumps/logs is expected or desired;
- restart must tolerate an incomplete previous transaction and corrupted/unknown individual records by failing closed.

## 5. Power loss

Power loss is treated as an abrupt process termination. Product copy must never promise recovery of every last keystroke.

## 6. Restart Manager / application restart

`RegisterApplicationRestart` may be evaluated later so Windows/Restart Manager can relaunch DraftRescue after certain managed shutdown/update scenarios. It is not a substitute for normal startup registration and is not required for earliest MVP.

If adopted:

- restart command line must contain no draft IDs, text, URLs, titles, or secrets;
- restart should enter background mode;
- duplicate-instance protection must prevent parallel observers.

## 7. Startup after reboot/logon

If Start with Windows is enabled, normal logon launch starts background protection without opening the recovery window. Existing unexpired protected drafts are indexed by metadata only; bodies are not bulk-decrypted.

## 8. Process exit ordering

Graceful explicit Quit:

```text
Stop accepting observation events
-> cancel/debounce pending reads
-> checkpoint already eligible latest state within time budget
-> detach WinEvent/UIA observers
-> dispose repository/resources
-> exit
```

System session-end uses a stricter time budget and may skip nonessential work.

## 9. Recovery integrity after restart

On startup:

- validate DB/schema version;
- reject unknown protection formats safely;
- exclude expired records before UI exposure;
- never auto-restore;
- never infer that a crash itself proves the target draft was lost.

## 10. Required tests

- clean Quit with dirty eligible snapshot;
- shutdown notification during debounce;
- shutdown while UIA provider is hung;
- kill process during DB update;
- machine restart after committed snapshot;
- corrupt one record without breaking all other valid records;
- restart with expired records;
- double-launch at login;
- update/restart path if Restart Manager support is later enabled.

## 11. External platform references

Implementation research should verify current Microsoft documentation for `WM_QUERYENDSESSION`, `WM_ENDSESSION`, Restart Manager, and `RegisterApplicationRestart` before coding.
