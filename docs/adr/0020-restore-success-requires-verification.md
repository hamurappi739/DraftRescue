# ADR 0020 — Direct Restore success requires verification when safely available

**Status:** Accepted

## Decision

A write API returning success is not automatically treated as verified recovery. Target adapters must verify the resulting value/identity when a safe capability exists. If safe verification is unavailable, return an explicit unverified state rather than claiming verified success.

## Consequences

- UI distinguishes verified from unverified outcome;
- failed/mismatched verification keeps recovery conservative;
- verification must not persist or log plaintext.
