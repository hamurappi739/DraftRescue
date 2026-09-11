# Phase 3 Plaintext Non-Leakage Argument

## Claim

Within Phase 3, plaintext can flow only from an already-authorized provider read into one transient snapshot and one in-memory current-state tracker. It cannot flow to persistence, UI, logging, clipboard, network, diagnostics, or revision history.

## Structural argument

```text
AllowedFieldHandle
  -> IEligibleFieldTextReader
  -> FieldTextSnapshot
  -> IDraftTracker.ApplySnapshot

NO EDGES TO:
  IDraftRepository
  IDraftProtector
  Recovery UI/ViewModel
  Clipboard
  RestoreService
  HttpClient/network
  Logger<string-content>
```

Phase-3 architecture tests should inspect project references/constructor dependencies and source patterns for forbidden sinks.

## Dynamic argument

Spy tests inject a synthetic canary and assert:

- repository calls = 0;
- protector calls = 0;
- clipboard writes = 0;
- network sends = 0;
- UI body presentations = 0;
- captured log payload contains no canary;
- tracker holds exactly one current snapshot, not a revision list.

## Limitation

The CLR may retain string memory until GC; this package does not claim secure erasure. See `PLAINTEXT_MEMORY_LIFETIME_SPEC.md`.
