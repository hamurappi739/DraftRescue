namespace DraftRescue.Infrastructure.Persistence;

/// <summary>Canonical Phase 4 SQLite contract. Runtime bootstrap is intentionally deferred.</summary>
public static class SqliteSchemaV1
{
    public const int UserVersion = 1;
    public const string JournalMode = "DELETE";
    public const string SecureDelete = "ON";
    public const string ForeignKeys = "ON";
    public const int BusyTimeoutMilliseconds = 1500;
    public const string Synchronous = "EXTRA";

    public const string CreateDraftsTableSql = """
CREATE TABLE drafts (
    draft_id BLOB NOT NULL PRIMARY KEY,
    record_schema_version INTEGER NOT NULL CHECK(record_schema_version = 1),
    application_id TEXT NOT NULL,
    app_profile_id TEXT,
    app_profile_version INTEGER,
    presentation_kind INTEGER NOT NULL,
    fingerprint_version INTEGER NOT NULL,
    match_metadata_version INTEGER NOT NULL,
    match_metadata BLOB NOT NULL,
    protected_payload BLOB NOT NULL,
    protection_version INTEGER NOT NULL,
    snapshot_sequence INTEGER NOT NULL CHECK(snapshot_sequence >= 0),
    created_at_utc_ms INTEGER NOT NULL,
    updated_at_utc_ms INTEGER NOT NULL,
    expires_at_utc_ms INTEGER NOT NULL,
    recoverable_state INTEGER NOT NULL,
    CHECK(updated_at_utc_ms >= created_at_utc_ms),
    CHECK(expires_at_utc_ms >= updated_at_utc_ms)
) WITHOUT ROWID;
""";

    public const string CreateExpiryIndexSql =
        "CREATE INDEX ix_drafts_expires_at ON drafts(expires_at_utc_ms);";

    public static IReadOnlyList<string> RequiredPragmas =>
    [
        "PRAGMA journal_mode=DELETE;",
        "PRAGMA secure_delete=ON;",
        "PRAGMA foreign_keys=ON;",
        "PRAGMA busy_timeout=1500;",
        "PRAGMA synchronous=EXTRA;"
    ];
}
