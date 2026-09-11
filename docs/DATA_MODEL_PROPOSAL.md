# Data Model Proposal

**Status:** historical proposal refined by `DOMAIN_MODEL_CANONICAL.md`, `PERSISTENCE_STORAGE_SPEC.md`, and `REPOSITORY_TRANSACTION_SEMANTICS.md`. Those newer documents are normative.

## Canonical base fields

- DraftId
- ApplicationId
- WindowFingerprint
- FieldFingerprint
- EncryptedText
- CreatedAt
- UpdatedAt
- ExpiresAt
- RestoreState

## Proposed principles

### DraftId

Opaque random identifier. It must not encode user content.

### ApplicationId

Stable normalized application identity. Prefer executable identity/path-derived metadata as needed without using it as the sole recovery key.

### WindowFingerprint / FieldFingerprint

Opaque correlation identifiers derived from multiple context signals. Avoid storing raw user-visible strings if a derived fingerprint is enough.

### EncryptedText

Persistence must store protected ciphertext, never ordinary plaintext draft content. Protection format should be versioned so migrations/rotation are possible.

### Timestamps

Use UTC internally for persistence and expiry comparisons. UI may localize display.

### RestoreState

Represents whether a recoverable draft is still pending, restored, discarded, or expired. Exact enum/storage mapping will be finalized with recovery semantics.

## Explicitly not a history schema

Do not design tables that naturally accumulate every intermediate typing revision. Persistence should represent recoverable draft state, not a forensic timeline of user input.

## Proposed protected record shape

Conceptual persisted record (not a final schema):

```text
ProtectedDraftRecord
- SchemaVersion
- DraftId
- ApplicationId / AppProfileId / AppProfileVersion
- SnapshotSequence
- WindowFingerprint
- FieldFingerprint
- ProtectedPayload
- ProtectionFormatVersion
- CreatedAtUtc
- UpdatedAtUtc
- ExpiresAtUtc
- RecoverableState
- MinimalMatchMetadataVersion
```

The repository should not need plaintext to list recoverable items. Any user-visible preview/snippet decision should occur through an explicit decrypt/application presentation path, not by storing a second plaintext preview column.

## Record-count invariant

For one logical active draft, repeated text changes should update the current record rather than append a revision row. Tests should assert bounded record count under sustained typing.

## Metadata caution

Encryption of text does not make surrounding metadata automatically safe. Full URLs, page titles, accessibility names, and labels may contain user information; minimize or derive fingerprints where possible.
