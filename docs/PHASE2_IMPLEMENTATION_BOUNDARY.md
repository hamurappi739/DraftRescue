# Phase 2 Implementation Boundary

## Allowed production changes

Phase 2 may add only:

- security signal/domain result types;
- `ISecureInputGuard` refinement;
- deterministic security policy evaluator;
- platform adapters that acquire **approved metadata-only security signals**;
- positive allow predicates for synthetic/certified fixtures;
- capability issuer/validator/registry required to create `AllowedFieldHandle`;
- content-free diagnostics/error codes;
- tests/harnesses for the above.

## Forbidden production changes

Phase 2 must not add any implementation that retrieves target content.

Forbidden APIs/behavior include:

```text
ValuePattern.Value
TextPattern.DocumentRange.GetText(...)
TextPatternRange.GetText(...)
LegacyIAccessible.Value
Win32 GetWindowText used as field content
clipboard reads used as capture
keyboard-event aggregation
SendKeys/paste/write operations
```

`Name`, `HelpText`, `ItemStatus`, labels, window/page titles and other Tier C metadata also remain outside generic classification unless a later audited profile explicitly requires one and a new ADR approves it.

## Compile-time intent

The Phase 2 composition root must not register an `IEligibleFieldTextReader` implementation. Security handlers must have no constructor/member dependency on:

- `IEligibleFieldTextReader`;
- `IDraftTracker`;
- `IDraftRepository`;
- `IDraftProtector`;
- `IClipboardService`;
- restore adapters.

The capability issuer is the terminal output of Phase 2.

## Data flow

```text
TargetFieldCandidateMetadata
        |
        v
SecuritySignalCollector (metadata only)
        |
        v
SecurityEvidenceSet
        |
        v
Pure SecurityPolicyEvaluator
    |             |
  Denied        Allowed
    |             |
    v             v
 typed deny   AllowedFieldHandleIssuer
 result            |
                   v
             one-read capability
                   |
                  STOP
```

No target text exists anywhere in this phase.

## Security boundary

A target may be allowed only if every mandatory signal for its profile is known and safe. Missing signal means deny; there is no permissive default.

## Phase stop condition

When `AllowedFieldHandle` can be issued for the controlled synthetic ordinary-field fixture and cannot be issued for every negative fixture, Phase 2 stops. Do not prove the handle by reading real text; prove it through issuer/validator tests and fake reader spies only.
