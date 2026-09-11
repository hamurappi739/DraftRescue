# Phase 3 Type Blueprint

Suggested future types; names may change only through an explicit architecture update.

## Application

```text
IEligibleFieldTextReader
EligibleTextReadResult
ReadBudget
FieldTextSnapshot
SnapshotSequence
IDraftTracker
ApplySnapshotResult
EmptyCandidateState
CaptureSnapshotHandler
```

## Platform.Windows

```text
WindowsEligibleFieldTextReader
IWindowsReadStrategy
TextPatternDocumentReadStrategy
CertifiedValuePatternReadStrategy
PlatformBindingResolver
```

## Tests

```text
FakeEligibleFieldTextReader
SpyForbiddenSinks
SyntheticTextTargetFixture
FakeMonotonicClock
```

## Ownership rule

UIA types never escape `Platform.Windows`. `FieldTextSnapshot` is Application-level transient data, not a persistence entity or Desktop UI DTO.
