# End-to-End Recovery Scenarios

**Status:** normative behavioral walkthroughs. These scenarios connect module contracts into user-visible outcomes.

## E2E-001 — ordinary draft survives application crash

1. User focuses a supported ordinary text field.
2. Foreground/focus event creates `ContextGeneration = N`.
3. App profile resolves to supported compatible profile.
4. Candidate metadata is inspected without field text.
5. Secure/private guard returns `Allowed`.
6. Short-lived `AllowedFieldHandle` is minted.
7. A bounded change/snapshot trigger causes eligible text read.
8. Snapshot `Sequence=1` enters tracker as transient plaintext.
9. Tracker establishes logical `DraftId` and fingerprint set.
10. Checkpoint scheduler coalesces subsequent changes.
11. Protector encrypts the newest current plaintext before repository boundary.
12. Repository atomically upserts protected `Sequence=k`.
13. Target application crashes.
14. DraftRescue does not need a crash-time text read; last committed checkpoint is durable.
15. User reopens DraftRescue: metadata list shows the draft without decrypting body.
16. User chooses Preview or returns to the target field and chooses Restore.
17. Restore performs fresh target/profile/security/match checks.
18. Selected payload is decrypted only after restore mutation is authorized.
19. Adapter writes text.
20. If safe verification succeeds, repository deletes the recoverable record.
21. UI shows `Draft restored.` and no history entry remains.

## E2E-002 — PC reboot during long typing session

1. User types for several minutes.
2. Scheduler periodically commits current-state checkpoints; no per-keystroke history is created.
3. Windows begins shutdown/restart.
4. DraftRescue performs only bounded session-end handling and does not block shutdown waiting for a final full capture.
5. Process exits.
6. After sign-in/autostart, DraftRescue opens existing store, excludes expired records, and lists metadata only.
7. Latest **committed** checkpoint is recoverable. DraftRescue does not claim text typed after the last successful checkpoint was saved.

## E2E-003 — password field never reaches reader

1. Candidate metadata reports trusted protected/password evidence.
2. Secure guard returns deny.
3. No `AllowedFieldHandle` is created.
4. Text reader invocation count remains zero.
5. No tracker/persistence path executes.
6. Diagnostics contain only structural deny code.

## E2E-004 — private browser window while normal window exists

1. Normal Chrome window has a supported ordinary field.
2. Separate Incognito window gains focus.
3. Context generation changes.
4. Browser privacy detector classifies the **current window/context**, not process globally.
5. Private or Unknown -> deny before text read.
6. Previously recoverable normal-window draft remains in store until normal completion/expiry; it is not attached to the private window.

## E2E-005 — stale async result after focus switch

1. Field A begins metadata/read operation for generation N.
2. User quickly focuses field B -> generation N+1.
3. A's provider call returns late.
4. Coordinator sees stale generation and discards result before tracking/persistence.
5. No text from A is associated with B.

## E2E-006 — out-of-order checkpoints

1. Snapshot sequence 41 and 42 are prepared asynchronously.
2. Protected sequence 42 commits first.
3. Sequence 41 arrives later.
4. Repository transaction sees stored sequence 42 and returns `StaleIgnored`.
5. Durable record remains sequence 42.

## E2E-007 — ambiguous recovery target

1. Draft metadata matches two current compose fields that both satisfy weak/partial evidence.
2. Evidence lattice cannot identify exactly one strong target.
3. Result is `Ambiguous`.
4. Restore is unavailable; Preview/Copy remain available.
5. No force-restore control exists.

## E2E-008 — target becomes secure after user clicks Restore

1. Recovery card shows Restore available based on a prior non-authoritative availability check.
2. User clicks Restore.
3. Target changed to credential/password surface.
4. Fresh guard denies.
5. No payload decryption for injection if authorization has not yet passed; no write occurs.
6. Draft remains recoverable.

## E2E-009 — write applied but cannot be safely verified

1. All pre-write checks pass.
2. Adapter writes using a certified mechanism whose target cannot expose a safe verification readback.
3. Result is `AppliedButUnverified`.
4. Draft remains recoverable.
5. UI clearly states the recovery copy was kept; it does not automatically retry the write.

## E2E-010 — explicit Copy

1. User clicks Copy on one draft.
2. Application decrypts only that draft.
3. Clipboard service writes once.
4. Draft remains recoverable.
5. DraftRescue drops plaintext reference after operation.
6. No timer later mutates the user's clipboard.

## E2E-011 — manual stable clear

1. User has an active recoverable draft.
2. Same allowed field is deliberately cleared and remains stably empty according to the profile's clear semantics.
3. Tracker classifies `ClearedByUser`.
4. Current draft record is removed.
5. Later focus loss does not resurrect the old content.

## E2E-012 — storage/protection failure

1. An eligible snapshot exists in memory.
2. DPAPI/protector fails or store transaction fails.
3. DraftRescue does **not** fall back to plaintext storage/logging.
4. Product may report reduced protection capability in a content-free way.
5. Safety wins over recovery completeness.
