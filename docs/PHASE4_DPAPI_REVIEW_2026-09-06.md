# Phase 4 WP4.2 DPAPI Review — 2026-09-06

## Decision

WP4.2 implementation and boundary gate are **Pass**. The implementation is fail-closed when Windows DPAPI is unavailable; it never falls back to plaintext or another protection scope. Full Phase 4 remains pending because SQLite runtime, coordinator, retention and corruption handling are not enabled.

## Implemented

- Strict `DRP1` envelope codec: ASCII magic, little-endian version/length, UTF-8 exact bytes, reserved-byte validation, strict invalid-UTF-8 rejection.
- `WindowsDpapiDraftProtector` uses `DataProtectionScope.CurrentUser` and empty optional entropy only.
- Unknown protection versions fail before unprotect; malformed envelopes map to stable `InvalidProtectedPayload`.
- Mutable plaintext/decrypted buffers are zeroed in `finally`; exception text contains only stable failure codes.
- `WindowsDpapiInstallationSecretStore` generates exactly 256 random bits, protects them with DPAPI CurrentUser, stores a versioned opaque file, reuses the existing identity, and fails closed on corruption/unavailability.
- No LocalMachine scope, machine/user/path-derived entropy, plaintext fallback, settings/registry/database key storage, or automatic identity replacement.

## Evidence

- Full solution build: **0 warnings / 0 errors**.
- `dotnet test tests\DraftRescue.Tests\DraftRescue.Tests.csproj --no-build -c Debug`: **127/127**.
- `scripts/phase4_dpapi_guard.ps1`: **Pass**, zero forbidden scope/fallback violations.
- `scripts/run_phase4_dpapi_gate.ps1 -SkipBuild`: **Pass**; artifact `artifacts/phase4-dpapi-gate/PHASE4-DPAPI-GATE.json`.
- The current test host reports Windows DPAPI unavailable because its user profile is not loaded. Tests explicitly verify the stable `DpapiFailure` fail-closed path; no test treats that as permission to persist plaintext.

## Privacy and correctness invariants

Relevant controls: P-002, P-003, P-004, P-005, P-006, P-017, P-025, P-028, P-030, P-031, P-032, P-033, P-035, P-036, P-037; C-019, C-020, C-026, C-027, C-028, C-029, C-030, C-031, C-032, C-033.

## Next bounded work package

WP4.3: SQLite store bootstrap/schema migration and serialized writer, preserving the protected-record-only repository contract and refusing plaintext fallback.
