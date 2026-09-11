# Draft Lifecycle State Machine

**Status:** product/architecture contract for later implementation.

## 1. Purpose

A DraftRescue draft is not a typing log. It is the **latest recoverable state of one eligible editable field** for a limited period.

The state machine exists to prevent three common design failures:

1. accumulating every revision as history;
2. keeping a draft after the user has clearly submitted/saved it;
3. restoring stale text into a field that no longer represents the same context.

## 2. Canonical states

```text
ObservedCandidate
      |
      | safe + supported
      v
    Active --------------------------+
      |                               |
      | context lost unexpectedly     | explicit save/submit/clear
      v                               v
 Recoverable                      Completed
      |        |        |             |
      |        |        |             +--> purge protected payload
      |        |        |
      |        |        +--> Expired ------> purge
      |        +----------> Discarded -----> purge
      +-------------------> Restored ------> purge or mark terminal, then purge
```

`Completed` is a conceptual terminal event and does not need to exist as a persisted historical record.

The current code enum may remain minimal; implementation must preserve these semantics even if names differ.

## 3. Active

`Active` means:

- the context has passed privacy/security gating;
- DraftRescue is tracking only the current draft state;
- the field/session is still plausibly live;
- the latest snapshot may replace the previous snapshot for that same active session.

### Required invariant

At most one current recoverable payload should exist per logical active draft session unless a later ADR explicitly proves a need for otherwise.

Do not append revisions merely because text changed.

## 4. Transition to Recoverable

A draft becomes `Recoverable` when the tracked context disappears or becomes unavailable **without positive evidence that the draft was intentionally completed**.

Examples:

- application/window closes unexpectedly;
- browser tab/window disappears;
- process exits or crashes;
- machine/app restart leaves a still-valid protected draft on startup;
- observer loses the target in a way that cannot be classified as successful completion.

Loss of focus alone must **not** automatically make a draft recoverable. Users frequently switch windows while still composing.

## 5. Completion

A draft should stop being recoverable when there is sufficiently strong evidence that the user intentionally submitted/saved/cleared it.

Examples of possible evidence are defined in `SUBMISSION_AND_CLEAR_DETECTION.md`.

### Conservative MVP rule

False deletion is worse than briefly retaining a legitimate draft, but retaining text after an obvious submission creates privacy risk and confusing recovery. Therefore:

- use explicit high-confidence completion signals when available;
- otherwise use conservative field-state heuristics;
- never infer completion from a single weak signal such as focus loss.

## 6. Recoverable

A recoverable draft:

- is visible in the recovery UI;
- is still inside retention;
- remains encrypted at rest;
- can be Previewed or Copied explicitly;
- can be Restore-enabled only after a fresh safe-match decision.

Being recoverable does **not** mean the original field is automatically trusted for restore.

## 7. Restore

Successful restore is a terminal user action for that recoverable instance.

Default MVP behavior after confirmed successful restore:

1. mark the operation successful;
2. delete the persisted recoverable payload promptly;
3. if observation continues in the target field, any later unsaved edits form a new active tracking lifecycle.

Do not keep a hidden historical copy “just in case.”

## 8. Copy

Copy is **not** completion by itself.

The user may copy a draft because direct Restore is unsupported. The draft remains recoverable until discarded, expired, successfully restored, or otherwise completed.

## 9. Discard

Discard means permanent deletion of the recoverable payload.

MVP should not implement an undo recycle bin because that would deliberately retain data the user just asked to delete.

## 10. Expiry

Expiry is authoritative even if the UI still has stale presentation data.

When `ExpiresAt <= now`:

- application layer rejects restore/copy/preview requests that require the expired payload;
- persistence deletes the payload;
- UI refreshes/removes the item.

Startup cleanup must handle drafts that expired while DraftRescue was not running.

## 11. Crash consistency

The lifecycle must tolerate abrupt process termination between state changes. Detailed persistence behavior is in `CRASH_AND_PERSISTENCE_SEMANTICS.md`.

## 12. Acceptance invariants

- No revision history table/log is required for recovery.
- Focus loss is not equivalent to completion.
- Copy does not silently delete a draft.
- Successful Restore does not create history.
- Discard and expiry remove recoverable content.
- Restore eligibility is recalculated against the current target, not inherited from when the draft was captured.
