# Command Idempotency and Single-Flight Specification

**Status:** accepted orchestration contract.

## 1. Goal

Desktop users can double-click buttons; async events can repeat; IPC can be retried. Product commands must remain safe under duplication.

## 2. Per-command policy

| Command | Duplicate behavior |
|---|---|
| Preview | may coalesce; no mutation |
| Copy | duplicate explicit activations may copy twice but must not mutate draft state |
| Restore | **single-flight per DraftId**; second activation returns AlreadyInFlight/joins same operation; never second injection |
| DiscardDraft | idempotent; already absent is success-equivalent |
| DiscardAll | one storage mutation at a time |
| UpdateSettings | last validated explicit user value wins |
| OpenWindow IPC | idempotent/bring existing window forward |
| Quit IPC | single transition |

## 3. Restore operation key

Use `DraftId` as the logical single-flight key. The lock/gate is application-process state, not stored user data.

Do not hold the gate across arbitrary UI lifetime. Release when the typed restore operation reaches a terminal result.

## 4. Cancellation

Canceling the UI wait does not mean target mutation was undone. Once adapter write begins, completion/verification path must settle to an honest typed result before automatic record deletion decisions.

## 5. Retry after failure

- pre-write failure: user may retry after context changes;
- `AppliedButUnverified`: no automatic retry because that could duplicate content;
- verification mismatch: no automatic retry;
- delete-after-verified-restore failure: retry **deletion only**, never target write.

## 6. Tests

Inject delays and fire 2–10 concurrent Restore commands for the same DraftId. Exactly one adapter write call is permitted.
