# ADR 0034 — Editable does not mean allowed

## Status
Accepted.

## Decision
An unknown editable control with `IsPassword == false` is not automatically eligible. Capture requires a positive allow predicate from a compatible certified profile after all hard-deny checks.

## Rationale
Credential/payment fields can be ordinary editable controls and providers may omit protected-content flags. Fail-closed positive allowlisting better matches the privacy model.
