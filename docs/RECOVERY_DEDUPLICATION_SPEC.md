# Recovery Deduplication and Presentation Specification

**Status:** accepted UI/data behavior.

## 1. Goal

The recovery window should show meaningful current recoverable drafts, not multiple cards for implementation artifacts of the same logical draft.

## 2. Logical identity

A recoverable item is keyed by `DraftId` plus its accepted logical lifecycle. Repeated checkpoints update the same item; they do not create new cards.

A new lifecycle is created only after the previous lifecycle is conclusively completed/discarded/expired and subsequent eligible editing begins.

## 3. Duplicate records after crash/migration

If persistence anomalies produce multiple physical records that claim the same logical identity:

- never concatenate plaintext;
- choose only records that pass schema/protection validation;
- prefer newest valid monotonic snapshot sequence within the same lifecycle;
- quarantine/delete invalid superseded records without exposing their payload;
- record structural diagnostic counters only.

## 4. Similar drafts are not duplicates

Two fields containing identical text are separate drafts if their logical field/context identities differ. Text equality must never be used as a cross-context deduplication key.

## 5. UI ordering

Default recovery list ordering: most recently updated recoverable draft first.

Cards show minimized metadata only. No grouping by raw URL/title histories.

## 6. Notification deduplication

A single recoverable item should not repeatedly notify the user every time focus enters/leaves the target. Notification state is metadata-only and may be tracked per draft lifecycle.

## 7. Tests

- 100 checkpoints -> one recovery card;
- same text in two fields -> two cards;
- crash duplicate rows -> newest valid sequence wins;
- expired duplicate cannot resurrect item;
- notification shown at most according to deduplication policy;
- no plaintext-based global dedupe.
