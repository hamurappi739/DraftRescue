# Capability Revocation & Consumption Specification

## States

```text
NotIssued
   |
   v
Fresh
 |  |  \
 |  |   \ deadline/generation/profile/binding change
 |  |    v
 |  |  Revoked
 |  |
 |  +-- validation starts --> Consumed
 |
 +-- explicit revocation --> Revoked
```

Terminal states are `Consumed` and `Revoked`.

## Revocation triggers

A fresh capability becomes invalid on any of:

- `ContextGeneration` changes;
- current candidate/binding changes;
- target process exits;
- target binding disappears/stales;
- profile resolution/revision/version changes;
- private/security state emits fresh deny evidence;
- monotonic deadline passes;
- orchestrator explicitly cancels the read attempt;
- primary process shuts down.

## Consumption timing

Consumption is **claim-before-read**, not read-before-claim.

Conceptual validator:

```text
TryClaim(handle, currentContext)
    validate freshness + deadline + generation + binding
    atomically Fresh -> Consumed
    return validated platform binding OR deny
```

Only after successful claim may the Phase 3 reader attempt content access.

If the actual UIA read then fails, the handle remains consumed.

## Concurrency

Two concurrent attempts using the same handle:

- exactly one may win `Fresh -> Consumed`;
- the other receives `AlreadyConsumed`;
- neither gets a second read authorization.

## Revocation precedence

`Revoked`/expired/stale beats a concurrent claim if revocation is observed before the atomic claim succeeds. Tests must cover deterministic interleavings using a fake monotonic clock and barrier-controlled registry.

## No durable revocation list

Capabilities are process-local and short-lived. Do not persist them or their revocation history.
