# Phase 3 DI and Architecture Test Specification

## Allowed graph

```text
Application capture orchestration
 -> IAllowedFieldCapabilityClaimer
 -> IEligibleFieldTextReader
 -> IDraftTracker

Platform.Windows
 -> capability binding resolver
 -> certified UIA read strategy

Infrastructure persistence/protection: NOT referenced by Phase-3 capture path
Desktop UI: does not receive FieldTextSnapshot
```

## Required architecture tests

- reader cannot be called with candidate metadata alone;
- `AllowedFieldHandle` cannot be reconstructed from DTO/string/JSON;
- Phase-3 orchestration has no `IDraftRepository` dependency;
- `FieldTextSnapshot` is not exposed by public Desktop ViewModel contracts;
- no logger call accepts snapshot text;
- no production `GetText(-1)`;
- no `LegacyIAccessible` value fallback;
- tracker representation has one current text field, not a collection of revisions.
