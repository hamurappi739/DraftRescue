# ADR 0010 — DPAPI CurrentUser Payload Protection v1

**Status:** Accepted for Windows MVP

## Decision

`ProtectionFormatVersion=1` uses Windows DPAPI via `ProtectedData` with `DataProtectionScope.CurrentUser` to protect each serialized draft payload before repository persistence.

## Why

It is local, Windows-native, user-bound, and avoids introducing an application master-key system for small temporary MVP payloads.

## Consequences

- no plaintext fallback on protection failure;
- Windows-specific implementation stays behind `IDraftProtector`;
- format is versioned for future migration;
- this does not defend against malware/OS compromise in the same user session.
