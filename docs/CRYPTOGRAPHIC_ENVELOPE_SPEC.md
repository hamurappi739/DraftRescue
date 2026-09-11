# Cryptographic Envelope Specification

**Status:** accepted MVP baseline for Windows; implementation begins in Phase 4.

## 1. Goal

Protect draft bodies at rest using a Windows-user-bound primitive while keeping cryptography outside repository and UI concerns.

## 2. MVP primitive

Use Windows DPAPI through `System.Security.Cryptography.ProtectedData` with `DataProtectionScope.CurrentUser` for `ProtectionFormatVersion = 1`.

The protected payload can be decrypted only in the intended Windows user context. DPAPI use is isolated in `DraftRescue.Platform.Windows` or a Windows-specific infrastructure adapter behind `IDraftProtector`.

## 3. Why direct DPAPI for v1

For small, temporary draft bodies, direct protection keeps the key architecture simple:

- no application master data-encryption key;
- no key database;
- no cloud/key service;
- Windows account binding is explicit;
- fewer secret-management failure modes for MVP.

A future envelope using AES-GCM + DPAPI-wrapped key is allowed only through a new ADR demonstrating a concrete need.

## 4. Plaintext payload format before protection

The normative byte format is `DPAPI_PAYLOAD_FORMAT_V1.md`: a small versioned `DRP1` envelope containing exact UTF-8 bytes and an explicit byte length. It contains no URL/title/label/match metadata.

## 5. Protection context

Conceptual:

```text
DraftProtectionContext
- DraftId
- ProtectionFormatVersion
```

MVP does not rely on optional entropy as a secret. If optional entropy is used, it must be deterministic/recoverable and separately specified; do not invent user-machine identifiers as entropy.

## 6. API shape

```text
IDraftProtector
  Protect(DraftPlaintext, DraftProtectionContext) -> ProtectedDraftPayload
  Unprotect(ProtectedDraftPayload, DraftProtectionContext) -> DraftPlaintext
```

Repository never invokes `Protect`/`Unprotect`; orchestration does.

## 7. Failure semantics

Protect failure:

- do not persist plaintext;
- keep only bounded transient in-memory state if still appropriate;
- surface structural error state without content;
- retry policy must be bounded;
- never fall back to unencrypted storage.

Unprotect failure:

- mark the record unavailable/invalidated for UI purposes;
- do not log payload bytes;
- do not repeatedly hammer DPAPI;
- allow user to discard the unreadable record.

## 8. Memory handling

.NET cannot guarantee perfect secret zeroization for immutable strings. Therefore:

- minimize number/lifetime of plaintext `string` instances;
- avoid copies/interpolation/LINQ transformations where unnecessary;
- UTF-8 buffers used for protection should be short-lived and cleared when practical;
- do not make false claims of guaranteed zeroization;
- prioritize preventing durable/log/telemetry copies.

## 9. Protection format versioning

Persist alongside every payload:

```text
protection_version = 1
```

Unprotect dispatches by version. Unknown version fails closed. Version migration is explicit and tested.

## 10. Fingerprint key protection

The separate installation-local HMAC fingerprint key is also protected with DPAPI `CurrentUser`, but it is not stored inside each draft record.

## 11. Threat boundary

This protects at-rest content from casual disk/database inspection and binds decryption to the Windows user context. It is not a defense against malware already running as the same user, an unlocked hostile session, or a fully compromised OS. Product copy must not imply otherwise.

## 12. Tests

Windows integration tests must prove:

- protect output differs from plaintext bytes;
- roundtrip succeeds for current user;
- wrong/random ciphertext fails without plaintext fallback;
- logs/exceptions do not include draft content;
- unknown format version fails closed;
- repository raw bytes do not contain known plaintext fixture.
