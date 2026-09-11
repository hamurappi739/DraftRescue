# Phase 4 Windows / Library API Verification Notes

**Checked:** 2026-09-02 against official Microsoft and SQLite documentation.

## DPAPI / ProtectedData

- `System.Security.Cryptography.ProtectedData` is Windows-backed DPAPI.
- `DataProtectionScope.CurrentUser` associates protected data with the current user; `LocalMachine` would broaden decryption to machine context and is forbidden for DraftRescue MVP.
- DPAPI does not protect against arbitrary code already executing under the same user credentials; the threat model must say this plainly.

## SQLite rollback journal

SQLite documents atomic commit in rollback mode and recovery via hot rollback journals after interrupted commits.

## SQLite `secure_delete`

SQLite documents that `secure_delete=ON` overwrites deleted content within SQLite-managed storage. This remains defense-in-depth, not forensic wipe assurance.

## SQLite `synchronous`

SQLite documents `EXTRA` as stronger than `FULL` for rollback-journal DELETE mode around directory synchronization when deleting the journal. Phase 4 should test `EXTRA` as the durability-first baseline.

## Implementation validation still required

Before lock:

- verify exact Microsoft.Data.Sqlite API/package behavior on .NET 8 Windows;
- verify returned `journal_mode` and `synchronous` values;
- inject process kill/power-loss-equivalent boundaries as far as practical;
- verify chosen provider does not create unexpected plaintext temp files.
