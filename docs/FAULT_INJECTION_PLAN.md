# Fault Injection Plan

## Purpose

Make failure behavior executable before broad target support is attempted.

## Fault layers

### Observation

- WinEvent registration failure;
- event queue saturation;
- duplicate/out-of-order envelope;
- target process exits between event and reconciliation.

### UI Automation/provider

- `ElementNotAvailable`/stale element;
- COM error;
- timeout/hang;
- property unsupported;
- malformed/oversized structural string;
- UI tree churn;
- access denied/integrity mismatch.

### Security/profile

- profile missing;
- profile schema invalid;
- two profiles overlap;
- target version outside certified range;
- private-mode state unknown;
- IsPassword unavailable;
- contradictory signals.

### Protection/storage

- DPAPI protect/unprotect failure;
- SQLite locked/busy;
- disk full;
- transaction interruption;
- corrupted row;
- schema version newer than binary;
- stale sequence arrives late.

### Restore

- target changes after click;
- target becomes secure;
- write returns failure;
- write succeeds but verification fails;
- application closes during restore.

### UI/clipboard

- clipboard unavailable;
- duplicate button activation;
- preview canceled during decrypt;
- window closes during operation.

## Injection mechanism design

Prefer typed interfaces/test doubles and deterministic fault scripts. Do not rely solely on random chaos. Each fault case records:

```text
FaultId
Layer
Trigger
Expected typed result
Expected invariant IDs
Expected log code
Forbidden side effects
```

## Mandatory assertions

Every fault test should assert relevant negative effects, for example:

- text-reader call count == 0;
- repository call count == 0;
- no plaintext log canary;
- old valid record still readable;
- no second restore write;
- no allowed capability remains after context change.

## Production fault hooks

Do not ship hidden fault-injection endpoints or IPC commands in release builds. Test hooks live behind test-only composition or dedicated harness binaries.
