# Single Instance and IPC Specification

**Status:** application-lifecycle baseline.

## 1. Requirement

Exactly one DraftRescue observation/tracking/repository writer instance runs per interactive user session.

## 2. Secondary launch behavior

Secondary explicit launch requests an action from the existing instance and exits.

Allowed baseline commands:

- `OpenMainWindow`
- `OpenSettings`
- `QuitRequest` only when initiated through trusted local UI path

No draft body is sent over IPC in MVP.

## 3. Security

IPC endpoint must be scoped to the current user/session and must not expose generic commands such as "restore arbitrary plaintext" or "read draft".

Do not use predictable unauthenticated machine-wide TCP listeners for local IPC.

Exact primitive (named mutex + named pipe, platform single-instance helper, etc.) requires implementation ADR after packaging choice.

## 4. Split-brain handling

Repository cannot rely solely on SQLite serialization to make two observers acceptable. Prevent parallel observer pipelines before normal operation starts.

If ownership is uncertain, fail safe: do not start a second capture pipeline.

## 5. Update interaction

Updater/restart must avoid a race where old and new versions observe simultaneously. New process waits for old owner exit or fails cleanly.

## 6. Tests

- double-click executable repeatedly;
- startup launch + immediate explicit launch;
- existing instance hung during secondary launch;
- old-version instance during update;
- different Windows users/sessions;
- malformed IPC command;
- verify no draft text appears in IPC trace.
