# SQLite Schema v1 Specification

**Status:** canonical Phase-4 schema baseline.

## Principles

- exactly one row per current recoverable `DraftId`;
- no revision/event/keystroke table;
- draft body is ciphertext only;
- match metadata is privacy-minimized/fingerprinted;
- all timestamps are UTC Unix milliseconds;
- sequence is persisted and monotonic;
- unknown schema version fails closed.

## Schema

```sql
PRAGMA user_version = 1;

CREATE TABLE drafts (
    draft_id                BLOB    NOT NULL PRIMARY KEY,
    record_schema_version   INTEGER NOT NULL CHECK(record_schema_version = 1),
    application_id          TEXT    NOT NULL,
    app_profile_id          TEXT,
    app_profile_version     INTEGER,
    presentation_kind       INTEGER NOT NULL,
    fingerprint_version     INTEGER NOT NULL,
    match_metadata_version  INTEGER NOT NULL,
    match_metadata          BLOB    NOT NULL,
    protected_payload       BLOB    NOT NULL,
    protection_version      INTEGER NOT NULL,
    snapshot_sequence       INTEGER NOT NULL CHECK(snapshot_sequence >= 0),
    created_at_utc_ms       INTEGER NOT NULL,
    updated_at_utc_ms       INTEGER NOT NULL,
    expires_at_utc_ms       INTEGER NOT NULL,
    recoverable_state       INTEGER NOT NULL,
    CHECK(updated_at_utc_ms >= created_at_utc_ms),
    CHECK(expires_at_utc_ms >= updated_at_utc_ms)
) WITHOUT ROWID;

CREATE INDEX ix_drafts_expires_at
ON drafts(expires_at_utc_ms);
```

## Explicitly absent

There is no:

- `draft_revisions`;
- `keystrokes`;
- `events` containing text;
- plaintext `preview` or `snippet`;
- URL/title/field-label column;
- plaintext checksum of text;
- search/full-text index.

## `application_id`

This is a product-safe application identifier such as a stable profile/application token, not an arbitrary window title. It must not encode user document names or URLs.

## `match_metadata`

Opaque versioned binary/JSON payload containing only approved fingerprints and enum-like matching metadata. Source strings used to derive keyed fingerprints are never included.

## Row replacement

A change to text updates the same row. `draft_id` does not change merely because text changed.
