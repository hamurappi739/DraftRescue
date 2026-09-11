# Phase 4 SQL Transaction Recipes

These are normative pseudocode recipes, not copy-paste provider-specific C#.

## Monotonic upsert

Within one write transaction:

```sql
SELECT snapshot_sequence
FROM drafts
WHERE draft_id = $draft_id;
```

Then:

- no row → INSERT complete record;
- incoming sequence `< existing` → `StaleIgnored`, no mutation;
- `== existing` → `IdempotentNoChange` if semantic protected-record identity matches; otherwise typed conflict/corruption result;
- `> existing` → UPDATE all current-state columns atomically.

Never implement a stale check in application code only.

## Metadata-only list

```sql
SELECT draft_id,
       application_id,
       app_profile_id,
       app_profile_version,
       presentation_kind,
       updated_at_utc_ms,
       expires_at_utc_ms,
       recoverable_state
FROM drafts
WHERE expires_at_utc_ms > $now
ORDER BY updated_at_utc_ms DESC;
```

Do **not** select `protected_payload` for the normal card list query.

## Get selected protected record

Only explicit Preview/Copy/Restore phases may request the protected payload by `DraftId`. Phase 4 tests can exercise repository API without UI.

## Delete expired

Metadata-only delete in one transaction. No decryption.

## Delete one / all

Idempotent delete; no pre-read of protected payload.
