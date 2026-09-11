# Security Signal Model v1

## Purpose

Security classification must distinguish **false**, **unknown**, **unavailable**, and **failed**. Collapsing all of these to booleans creates unsafe defaults.

## Generic signal state

Conceptual model:

```text
SignalState<T>
- Known(T)
- Unknown
- Unavailable
- Failed(reasonCode)
```

Rules:

- `Known(false)` is not equivalent to `Unknown`;
- a required signal in `Unknown`, `Unavailable`, or `Failed` state blocks allow;
- raw provider exception text is never stored in the signal; use stable `DR-*` codes;
- signals contain structural enums/booleans/opaque identifiers only.

## SecurityEvidenceSet v1

Canonical fields:

```text
CandidateId                 opaque, ephemeral
ContextGeneration           required
ObservationSequence         required
ProfileResolution           Resolved | Missing | Invalid | Ambiguous | IncompatibleVersion
ProfileId                   only if Resolved
ProfileRevision             only if Resolved
TargetVersionStatus         Certified | Unknown | Incompatible | NotApplicable
IntegrityBoundary           Accessible | Inaccessible | Unknown
PrivateMode                 NotApplicable | ConfirmedNormal | ConfirmedPrivate | Unknown
ProtectedContent            KnownTrue | KnownFalse | Unknown | Unavailable | Failed
Editable                     KnownTrue | KnownFalse | Unknown | Unavailable | Failed
ReadOnly                     KnownTrue | KnownFalse | Unknown | Unavailable | Failed
ProviderHealth              Healthy | Stale | Timeout | AccessDenied | Failed
SensitivePurpose            None | Credential | Password | PinOrOtp | Payment | Banking | Unknown
PositiveAllowPredicate      Satisfied | NotSatisfied | Indeterminate
BindingStatus               Current | Stale | Missing
```

## Mandatory deny precedence

Any of these is terminal deny regardless of positive allow evidence:

- unsupported/inaccessible integrity boundary;
- missing/invalid/ambiguous/incompatible profile;
- private mode confirmed or required privacy state unknown;
- protected content true;
- required protected-content state unknown/unavailable/failed;
- sensitive purpose other than `None`;
- provider unhealthy;
- stale/missing binding;
- positive allow predicate not exactly `Satisfied`.

## Why not `bool?`

`bool?` cannot encode the difference between provider failure, unsupported property, stale element, and genuinely unknown policy state. These states need different diagnostics and tests even though all must deny.

## Logging

Allowed:

```text
SecurityDecision=Denied
DenialCode=DR-SEC-....
ProfileId=<static product-owned id>
ContextGeneration=<number>
DurationBucket=<bucket>
```

Forbidden:

- raw element Name/HelpText;
- field content;
- exception messages from providers;
- URL/window/page title;
- raw runtime IDs if they reveal provider-specific user state.
