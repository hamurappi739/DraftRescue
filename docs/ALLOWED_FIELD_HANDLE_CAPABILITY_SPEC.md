# AllowedFieldHandle Capability Specification

## Purpose

`AllowedFieldHandle` is a security capability, not a data DTO and not an identifier for persistence.

Possessing a valid handle is the only normal application path that may authorize **one future target-content read**.

## Core properties

The handle is:

- opaque to UI/domain callers;
- created only by the security capability issuer after an `Allowed` decision;
- bound to one ephemeral target binding;
- bound to one `ContextGeneration`;
- bound to one resolved profile ID + profile revision/version evidence;
- short-lived;
- single-consumption;
- non-serializable;
- non-persistable;
- non-loggable;
- invalid across process restart;
- invalid for Restore writes.

Restore uses a separate future `ValidatedRestoreTarget`; a capture handle can never authorize a write.

## Canonical conceptual fields

Internal implementation may contain:

```text
CapabilityId             cryptographically random opaque nonce
CandidateBindingId       process-local opaque binding registry key
ContextGeneration
ProfileId
ProfileRevision
IssuedAtMonotonic
DeadlineMonotonic
AllowedOperation         ReadSnapshot only
ConsumptionState         Fresh | Consumed | Revoked
```

These fields are not a wire/storage schema.

## Lifetime baseline

Initial implementation baseline:

- capability is expected to be consumed immediately by the same orchestration flow;
- default maximum age: **1000 ms**;
- absolute hard cap: **2000 ms**;
- no app profile or user setting may increase the hard cap;
- use a monotonic clock for age checks;
- a valid generation/binding check is still required even before deadline.

The 1000 ms baseline is a security default to validate empirically; changing it requires focused performance/security evidence and an ADR update.

## Single-consumption rule

Whether the downstream read succeeds, fails, times out, or is canceled after validation begins, the capability is consumed. Retrying requires fresh classification and a new capability.

This prevents a caller from repeatedly reading a field under one old security decision.

## Issuance preconditions

Issuer must verify atomically enough for the orchestration boundary:

1. decision is `Allowed`;
2. decision candidate ID equals current candidate;
3. `ContextGeneration` is current;
4. binding registry entry exists and is current;
5. profile revision/version evidence still matches;
6. deadline can be created from the current monotonic clock;
7. no newer hard-deny/revocation epoch exists.

Failure returns typed deny/non-success; issuer never "best efforts" a handle.

## API-shape intent

Do not expose:

```text
new AllowedFieldHandle(...)
AllowedFieldHandle.Parse(...)
AllowedFieldHandle.FromJson(...)
AllowedFieldHandle.ToTokenString()
```

to arbitrary callers.

`ToString()` should return a fixed redacted type label, never internal identifiers.

## Testability

Tests use a controlled issuer and fake binding registry, not serialized production capabilities.
