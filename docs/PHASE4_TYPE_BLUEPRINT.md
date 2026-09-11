# Phase 4 Type Blueprint

Conceptual types and ownership; exact C# syntax is deferred to implementation WP.

## Application contracts

```text
IDraftProtector
  Protect(DraftPlaintextPayload, DraftProtectionContext)
    -> ProtectResult<ProtectedDraftPayload>

  Unprotect(ProtectedDraftPayload, DraftProtectionContext)
    -> UnprotectResult<DraftPlaintextPayload>

IProtectedDraftRepository
  UpsertAsync(ProtectedDraftRecordV1)
  GetProtectedAsync(DraftId)
  ListRecoverableMetadataAsync(now)
  DeleteAsync(DraftId)
  DeleteExpiredAsync(now)
  DeleteAllAsync()

ICheckpointCoordinator
  CheckpointAsync(CurrentDraftSnapshot)

IInstallationSecretProvider
  GetOrCreateAsync()
```

## Infrastructure types

```text
SqliteProtectedDraftRepository
SqliteStoreBootstrapper
SqliteSchemaMigrator
SerializedStoreWriter
```

## Platform.Windows types

```text
WindowsDpapiDraftProtector
WindowsDpapiInstallationSecretStore
WindowsLocalDataPathProvider
```

## Result enums

```text
ProtectedDraftUpsertResult:
  Inserted
  Updated
  IdempotentNoChange
  StaleIgnored

StoreOpenResult:
  Ready
  Incompatible
  Corrupt
  Unavailable
```

No result carries plaintext in error text.
