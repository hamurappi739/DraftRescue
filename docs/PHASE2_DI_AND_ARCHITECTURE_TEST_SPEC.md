# Phase 2 DI & Architecture Test Specification

## Goal

Encode privacy boundaries in automated tests so a future refactor cannot casually wire content acquisition into security classification.

## Required architecture assertions

### A. Security graph dependency denylist

Types under application/platform Phase 2 security namespaces must not depend on:

```text
IEligibleFieldTextReader
IDraftTracker
IDraftRepository
IDraftProtector
IClipboardService
IRestoreAdapter
IRestoreService
```

### B. No content reader implementation in Phase 2

Before Phase 3 begins, the solution contains no concrete `IEligibleFieldTextReader` registration/implementation.

### C. Capability-only reader signature

When Phase 3 later introduces the reader, architecture test requires its public read method to accept `AllowedFieldHandle`, not `CandidateFieldMetadata`, `AutomationElement`, `IntPtr hwnd`, or arbitrary object/string.

### D. No serializable capability

Reject:

- public parameterless constructor;
- public constructor exposing capability fields;
- JSON/data-contract attributes on `AllowedFieldHandle`;
- parse/from-string APIs;
- repository/settings schema references to capability.

### E. Platform object containment

`AutomationElement`, COM UIA interfaces and raw HWND-based field objects do not cross into Domain/Desktop. Application sees normalized metadata and opaque binding/capability abstractions only.

### F. Forbidden API source scan

Until Phase 3, CI/source guard fails on content-read patterns in production code, including reviewed equivalents of:

```text
ValuePattern.Value
DocumentRange.GetText
GetText(-1)
LegacyIAccessible.*Value
Clipboard.Get*
GetAsyncKeyState
SetWindowsHookEx(WH_KEYBOARD*)
```

The scan is defense-in-depth, not the only protection.

## Required tests

- `ARC-005` Security graph excludes content reader.
- `ARC-006` AllowedFieldHandle is non-serializable/non-publicly constructible.
- `ARC-007` Phase 2 has no concrete content reader.
- `SEC-014` Denied decision cannot issue capability.
- `SEC-015` Capability requires current generation/binding.
- `SEC-016` Capability can be claimed once only.
- `SEC-017` Expired capability cannot be claimed.
- `SEC-018` Concurrent duplicate claim produces one winner.
