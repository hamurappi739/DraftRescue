# Protected Draft Record Model v1

## Purpose

This is the only record shape accepted by `IProtectedDraftRepository`.

```text
ProtectedDraftRecordV1
  DraftId
  RecordSchemaVersion = 1
  ApplicationId
  AppProfileId?
  AppProfileVersion?
  PresentationKind
  FingerprintVersion
  MatchMetadataVersion
  MatchMetadataBytes
  ProtectedPayloadBytes
  ProtectionVersion = 1
  SnapshotSequence
  CreatedAtUtc
  UpdatedAtUtc
  ExpiresAtUtc
  RecoverableState
```

## Forbidden members

This type must not contain:

```text
Text
Plaintext
Preview
Snippet
RawUrl
WindowTitle
FieldName
ClipboardText
DraftPayloadV1 (unprotected)
```

## Construction rule

Only the persistence coordinator can construct a `ProtectedDraftRecordV1` from:

- privacy-safe metadata;
- the result of `IDraftProtector.Protect`;
- current snapshot sequence/timestamps.

The repository cannot protect data itself and cannot accept a `FieldTextSnapshot` overload.

## Immutability

Treat the record as immutable. Updates produce a complete new protected record and are atomically compared by sequence inside the repository.
