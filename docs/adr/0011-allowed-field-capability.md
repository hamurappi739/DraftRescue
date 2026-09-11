# ADR 0011 — Allowed-Field Capability Before Text Read

**Status:** Accepted; refined by ADR 0039, 0040, and 0045

## Decision

General platform UI elements cannot be passed directly to text-reading code. `IEligibleFieldTextReader` requires a short-lived `AllowedFieldHandle` produced only after metadata-only security classification returns Allowed.

## Why

This encodes "classify before reading" into API shape rather than relying only on developer discipline.

## Consequences

- capability expires on context-generation change/TTL;
- it is not serializable or durable;
- stale/forged capability attempts fail closed.

## Refinement

ADR 0039 makes the capability single-consumption; ADR 0040 fixes generation/binding/deadline semantics; ADR 0045 prevents using it for Restore.
