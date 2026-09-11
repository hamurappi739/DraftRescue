# Plaintext Memory Lifetime Specification

## Goal

Minimize where and how long target text exists after the security gate.

## Allowed plaintext holders in Phase 3

1. provider-returned temporary string inside `Platform.Windows` reader;
2. `FieldTextSnapshot` while crossing into Application;
3. current in-memory draft snapshot owned by `DraftTracker`.

No other Phase-3 component should retain a copy.

## Prohibited copies

Do not place plaintext in:

- logs/traces/Activity tags;
- exception messages;
- telemetry/metrics labels;
- event-bus history;
- debug `ToString()` output;
- immutable audit records;
- crash/support bundles;
- clipboard;
- UI ViewModels;
- persistent caches.

## .NET zeroing reality

A .NET `string` is immutable and cannot be reliably zeroed in place. Therefore the design must not claim cryptographic memory erasure. The mitigation is **copy minimization + bounded lifetime + reference release**, not false guarantees.

## Design rules

- avoid substring/prefix/suffix diagnostics;
- avoid LINQ/string transformations that duplicate content;
- do not Unicode-normalize or trim user text;
- release old snapshot references immediately when superseded/cleared;
- do not pool buffers containing plaintext unless the specific implementation can clear them safely;
- GC behavior is not treated as a security boundary.
