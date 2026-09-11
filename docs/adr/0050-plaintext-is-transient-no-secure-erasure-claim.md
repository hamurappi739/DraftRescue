# ADR 0050 — Plaintext Is Transient No Secure Erasure Claim

**Status:** Accepted

## Decision

Plaintext is transient and copy-minimized; DraftRescue does not claim reliable zeroization of managed strings.

## Rationale

Managed strings are immutable and GC lifetime is not a cryptographic erasure primitive.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
