# Concurrency and Race Specification

## 1. Core model: context generation

Every observed foreground/focused-field identity belongs to a monotonically increasing in-process `ContextGeneration`.

Any operation started for generation N must discard its result if the current generation is no longer N before the result is consumed.

This prevents slow cross-process calls from attaching text/security results to the wrong field.

## 2. Metadata -> eligibility -> read race

Sequence:

1. locate metadata for generation N;
2. collect/evaluate fresh security evidence for generation N;
3. issuer rechecks current binding/profile/generation and issues one-read `AllowedFieldHandle(N)`;
4. future reader atomically claims `Fresh -> Consumed` before any content access and checks monotonic deadline/current generation;
5. perform the single provider read attempt;
6. after read, verify N is still current;
7. otherwise discard transient plaintext snapshot without tracking/persisting it.

Two concurrent claims of the same capability may never produce two reads.

## 3. Dirty snapshot ordering

DraftTracker assigns a per-draft monotonic `SnapshotSequence` in memory.

Persistence completion for sequence K may only replace current durable state if no newer sequence has already committed/been accepted. Slow old writes cannot overwrite newer content.

## 4. Restore race

Between user click and actual write:

- target can disappear;
- focus can move;
- target can become password/secure;
- browser can switch to private context;
- field can already contain unrelated text.

RestoreService therefore uses a new operation generation and revalidates immediately before write. Any mismatch aborts.

## 5. Expiry race

Every command fetches/checks expiry at execution time. A card rendered one second before expiry cannot authorize a later preview/copy/restore.

## 6. Clear/completion race

Completion detector and new text snapshot may arrive close together. Strong completion associated with an older generation/sequence must not delete a newer draft context.

## 7. Shutdown

Shutdown checkpoint has a fixed small time budget and does not wait indefinitely on a hung UIA/provider/DB operation. Previously committed state remains valid.

## 8. Cancellation

Cancellation is normal control flow. Do not log cancellation with plaintext context. Cancellation never converts a deny/unknown into allow.
